using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.ViewModels;
using ClosedXML.Excel;
using System.IO;
using WMS.Models;
using System.Data;

namespace WMS.Controllers;

public class ReportsController : Controller
{
    private readonly AppDbContext _db;
    public ReportsController(AppDbContext db) => _db = db;

    private int? GetScopedWarehouseId()
    {
        if (User.IsInRole("Admin")) return null;
        var claim = User.FindFirst("WarehouseId")?.Value;
        return int.TryParse(claim, out var id) ? id : -1;
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> StockCount(int? warehouseId, DateTime? countDate)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;
        countDate ??= DateTime.UtcNow.Date;

        var vm = new StockCountPageViewModel
        {
            WarehouseId = warehouseId,
            CountDate = countDate.Value,
            Warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync()
        };
        if (scopedWh.HasValue)
            vm.Warehouses = vm.Warehouses.Where(w => w.WarehouseId == scopedWh.Value).ToList();

        if (!warehouseId.HasValue)
            return View(vm);

        vm.ExistingSheets = await _db.StockCountSheets.AsNoTracking()
            .Include(s => s.GeneratedAdjustmentVoucher)
            .Where(s => s.WarehouseId == warehouseId.Value && s.CountDate == countDate.Value.Date)
            .OrderByDescending(s => s.StockCountSheetId)
            .Take(50)
            .Select(s => new StockCountSheetSummary
            {
                StockCountSheetId = s.StockCountSheetId,
                CountDate = s.CountDate,
                CreatedBy = s.CreatedBy,
                CreatedAt = s.CreatedAt,
                Status = s.Status,
                ApprovedBy = s.ApprovedBy,
                ApprovedAt = s.ApprovedAt,
                ApprovalReason = s.ApprovalReason,
                UnlockedBy = s.UnlockedBy,
                UnlockedAt = s.UnlockedAt,
                UnlockReason = s.UnlockReason,
                VoucherCode = s.GeneratedAdjustmentVoucher != null ? s.GeneratedAdjustmentVoucher.VoucherCode : null,
                TotalLines = _db.StockCountLines.Count(l => l.StockCountSheetId == s.StockCountSheetId),
                DiffLines = _db.StockCountLines.Count(l => l.StockCountSheetId == s.StockCountSheetId && l.DiffQty != 0)
            })
            .ToListAsync();

        vm.Lines = await _db.ItemLocations.AsNoTracking()
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Include(il => il.Item)
            .Where(il => il.Quantity != 0
                && il.Location != null
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId.Value)
            .OrderBy(il => il.ItemId).ThenBy(il => il.LocationId)
            .Select(il => new StockCountLineInput
            {
                ItemId = il.ItemId,
                ItemCode = il.Item != null ? il.Item.ItemCode : "",
                ItemName = il.Item != null ? il.Item.ItemName : "",
                LocationId = il.LocationId,
                LocationCode = il.Location != null ? il.Location.LocationCode : "",
                LotNumber = il.LotNumber,
                ExpiryDate = il.ExpiryDate,
                SystemQty = il.Quantity,
                CountedQty = il.Quantity
            })
            .ToListAsync();

        return View(vm);
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockCountSaveDraft(StockCountPageViewModel vm)
    {
        if (!vm.WarehouseId.HasValue || vm.WarehouseId.Value <= 0)
        {
            TempData["Error"] = "Vui lòng chọn kho kiểm kê.";
            return RedirectToAction(nameof(StockCount));
        }

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && vm.WarehouseId.Value != scopedWh.Value)
            return Forbid();

        var lockDate = await _db.WarehousePeriodLocks.AsNoTracking()
            .Where(l => l.WarehouseId == vm.WarehouseId.Value && l.IsActive)
            .OrderByDescending(l => l.LockDate)
            .Select(l => (DateTime?)l.LockDate)
            .FirstOrDefaultAsync();
        if (lockDate.HasValue && vm.CountDate.Date <= lockDate.Value.Date)
        {
            TempData["Error"] = $"Kho đã khóa kỳ đến {lockDate.Value:dd/MM/yyyy}. Không thể tạo điều chỉnh kiểm kê cho ngày {vm.CountDate:dd/MM/yyyy}.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = vm.WarehouseId, countDate = vm.CountDate });
        }
        var approvedExists = await _db.StockCountSheets.AsNoTracking()
            .AnyAsync(s => s.WarehouseId == vm.WarehouseId.Value && s.CountDate == vm.CountDate.Date && s.Status == 2);
        if (approvedExists)
        {
            TempData["Error"] = "Ngày kiểm kê này đã được duyệt. Hệ thống khóa không cho tạo/sửa phiếu kiểm kê mới.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = vm.WarehouseId, countDate = vm.CountDate });
        }

        var normalizedLines = (vm.Lines ?? new List<StockCountLineInput>())
            .Where(l => l.ItemId > 0 && l.LocationId > 0)
            .Select(l => new StockCountLineInput
            {
                ItemId = l.ItemId,
                LocationId = l.LocationId,
                LotNumber = string.IsNullOrWhiteSpace(l.LotNumber) ? null : l.LotNumber.Trim(),
                ExpiryDate = l.ExpiryDate?.Date,
                CountedQty = l.CountedQty
            })
            .GroupBy(l => new { l.ItemId, l.LocationId, l.LotNumber, l.ExpiryDate })
            .Select(g => g.Last())
            .ToList();

        if (normalizedLines.Count == 0)
        {
            TempData["Error"] = "Không có dòng kiểm kê hợp lệ.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = vm.WarehouseId, countDate = vm.CountDate });
        }
        if (normalizedLines.Any(l => l.CountedQty < 0))
        {
            TempData["Error"] = "Số lượng thực tế không được âm.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = vm.WarehouseId, countDate = vm.CountDate });
        }
        var inputLocationIds = normalizedLines.Select(l => l.LocationId).Distinct().ToList();
        var validLocationIds = await _db.Locations.AsNoTracking()
            .Include(l => l.Zone)
            .Where(l => inputLocationIds.Contains(l.LocationId)
                && l.Zone != null
                && l.Zone.WarehouseId == vm.WarehouseId.Value)
            .Select(l => l.LocationId)
            .ToListAsync();
        if (validLocationIds.Count != inputLocationIds.Count)
        {
            TempData["Error"] = "Có vị trí không thuộc kho đang kiểm kê.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = vm.WarehouseId, countDate = vm.CountDate });
        }

        // Security/integrity: never trust client SystemQty; recompute from DB by batch key.
        var itemIds = normalizedLines.Select(x => x.ItemId).Distinct().ToList();
        var locationIds = normalizedLines.Select(x => x.LocationId).Distinct().ToList();
        var currentRows = await _db.ItemLocations.AsNoTracking()
            .Where(il => itemIds.Contains(il.ItemId) && locationIds.Contains(il.LocationId))
            .Select(il => new { il.ItemId, il.LocationId, il.LotNumber, il.ExpiryDate, il.Quantity })
            .ToListAsync();
        foreach (var l in normalizedLines)
        {
            l.SystemQty = currentRows
                .Where(r => r.ItemId == l.ItemId
                    && r.LocationId == l.LocationId
                    && r.LotNumber == l.LotNumber
                    && r.ExpiryDate == l.ExpiryDate)
                .Select(r => r.Quantity)
                .FirstOrDefault();
        }

        using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var sheet = new StockCountSheet
            {
                WarehouseId = vm.WarehouseId.Value,
                CountDate = vm.CountDate.Date,
                Notes = vm.Notes,
                Status = 1,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow
            };
            _db.StockCountSheets.Add(sheet);
            await _db.SaveChangesAsync();

            foreach (var l in normalizedLines)
            {
                _db.StockCountLines.Add(new StockCountLine
                {
                    StockCountSheetId = sheet.StockCountSheetId,
                    ItemId = l.ItemId,
                    LocationId = l.LocationId,
                    LotNumber = l.LotNumber,
                    ExpiryDate = l.ExpiryDate,
                    SystemQty = l.SystemQty,
                    CountedQty = l.CountedQty,
                    DiffQty = l.CountedQty - l.SystemQty
                });
            }
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            TempData["Success"] = $"Đã lưu phiếu kiểm kê nháp #{sheet.StockCountSheetId}. Vui lòng duyệt để sinh phiếu điều chỉnh.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = vm.WarehouseId, countDate = vm.CountDate });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            TempData["Error"] = $"Lỗi lưu phiếu kiểm kê: {ex.Message}";
            return RedirectToAction(nameof(StockCount), new { warehouseId = vm.WarehouseId, countDate = vm.CountDate });
        }
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockCountApproveDraft(long id, string? approvalReason)
    {
        if (string.IsNullOrWhiteSpace(approvalReason))
        {
            TempData["Error"] = "Vui lòng nhập lý do duyệt kiểm kê.";
            return RedirectToAction(nameof(StockCount));
        }
        approvalReason = approvalReason.Trim();
        if (approvalReason.Length > 500)
            approvalReason = approvalReason[..500];
        var approver = User.Identity?.Name ?? "system";
        var scopedWh = GetScopedWarehouseId();
        int? redirectWarehouseId = null;
        DateTime? redirectCountDate = null;
        using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var sheet = await _db.StockCountSheets
                .FirstOrDefaultAsync(s => s.StockCountSheetId == id);
            if (sheet == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu kiểm kê.";
                return RedirectToAction(nameof(StockCount));
            }
            if (scopedWh.HasValue && sheet.WarehouseId != scopedWh.Value)
                return Forbid();
            redirectWarehouseId = sheet.WarehouseId;
            redirectCountDate = sheet.CountDate;
            if (sheet.Status != 1)
            {
                TempData["Error"] = "Phiếu kiểm kê này đã được duyệt.";
                return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
            }
            if (sheet.GeneratedAdjustmentVoucherId.HasValue)
            {
                TempData["Error"] = "Phiếu kiểm kê này đã có phiếu điều chỉnh.";
                return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
            }
            if (string.Equals(sheet.CreatedBy, approver, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Người tạo phiếu kiểm kê không được tự duyệt. Vui lòng nhờ Manager/Admin khác duyệt.";
                return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
            }

            var duplicatedApproved = await _db.StockCountSheets
                .AnyAsync(s => s.StockCountSheetId != sheet.StockCountSheetId
                    && s.WarehouseId == sheet.WarehouseId
                    && s.CountDate == sheet.CountDate
                    && s.Status == 2);
            if (duplicatedApproved)
            {
                TempData["Error"] = "Đã tồn tại phiếu kiểm kê cùng kho/cùng ngày đã được duyệt. Không thể duyệt trùng.";
                return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
            }

            var lockDate = await _db.WarehousePeriodLocks.AsNoTracking()
                .Where(l => l.WarehouseId == sheet.WarehouseId && l.IsActive)
                .OrderByDescending(l => l.LockDate)
                .Select(l => (DateTime?)l.LockDate)
                .FirstOrDefaultAsync();
            if (lockDate.HasValue && sheet.CountDate.Date <= lockDate.Value.Date)
            {
                TempData["Error"] = $"Kho đã khóa kỳ đến {lockDate.Value:dd/MM/yyyy}. Không thể duyệt kiểm kê ngày {sheet.CountDate:dd/MM/yyyy}.";
                return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
            }

            var diffLines = await _db.StockCountLines
                .Where(l => l.StockCountSheetId == sheet.StockCountSheetId && l.DiffQty != 0)
                .ToListAsync();
            if (diffLines.Count > 0)
            {
                var diffLocationIds = diffLines.Select(x => x.LocationId).Distinct().ToList();
                var validDiffLocationIds = await _db.Locations.AsNoTracking()
                    .Include(l => l.Zone)
                    .Where(l => diffLocationIds.Contains(l.LocationId)
                        && l.Zone != null
                        && l.Zone.WarehouseId == sheet.WarehouseId)
                    .Select(l => l.LocationId)
                    .ToListAsync();
                if (validDiffLocationIds.Count != diffLocationIds.Count)
                    throw new Exception("Phiếu kiểm kê có vị trí không thuộc kho của phiếu. Dừng duyệt để đảm bảo an toàn dữ liệu.");
            }

            Voucher? createdVoucher = null;
            if (diffLines.Count > 0)
            {
                var items = await _db.Items
                    .Where(i => diffLines.Select(d => d.ItemId).Contains(i.ItemId))
                    .ToDictionaryAsync(i => i.ItemId, i => i);

                var prefix = "PDC";
                var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    var seq = await _db.Vouchers.CountAsync(v => v.VoucherCode.StartsWith(prefix + "-" + dateStr)) + 1;
                    var random = Random.Shared.Next(0, 100).ToString("D2");
                    var voucherCode = $"{prefix}-{dateStr}-{seq:D5}{random}";
                    var voucher = new Voucher
                    {
                        VoucherCode = voucherCode,
                        VoucherType = 5,
                        VoucherDate = sheet.CountDate.Date,
                        WarehouseId = sheet.WarehouseId,
                        Description = $"Điều chỉnh từ kiểm kê #{sheet.StockCountSheetId}" + (string.IsNullOrWhiteSpace(sheet.Notes) ? "" : $" - {sheet.Notes}"),
                        SourceType = 1,
                        CreatedBy = approver,
                        CreatedAt = DateTime.UtcNow,
                        IsPosted = true
                    };
                    _db.Vouchers.Add(voucher);
                    try
                    {
                        await _db.SaveChangesAsync();
                        createdVoucher = voucher;
                        break;
                    }
                    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
                        || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true
                        || ex.InnerException?.Message.Contains("2627", StringComparison.OrdinalIgnoreCase) == true
                        || ex.InnerException?.Message.Contains("2601", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _db.Entry(voucher).State = EntityState.Detached;
                    }
                }
                if (createdVoucher == null)
                    throw new Exception("Không thể tạo mã phiếu điều chỉnh, vui lòng thử lại.");

                int lineNo = 0;
                decimal totalAmount = 0;
                foreach (var l in diffLines)
                {
                    if (!items.TryGetValue(l.ItemId, out var item)) continue;
                    lineNo++;
                    var baseQty = l.DiffQty;
                    var abs = Math.Abs(baseQty);
                    var lineAmount = item.UnitCost * abs;
                    var unitPrice = abs > 0 ? lineAmount / abs : 0m;

                    _db.VoucherDetails.Add(new VoucherDetail
                    {
                        VoucherId = createdVoucher.VoucherId,
                        ItemId = l.ItemId,
                        LocationId = l.LocationId,
                        TransactionQty = abs,
                        TransactionUomId = item.BaseUomId,
                        ConversionRate = 1m,
                        BaseQty = baseQty,
                        UnitPrice = unitPrice,
                        LineAmount = lineAmount,
                        QualityStatus = 1,
                        ExpiryDate = l.ExpiryDate,
                        LotNumber = l.LotNumber,
                        Notes = $"Kiểm kê #{sheet.StockCountSheetId}: hệ thống {l.SystemQty:N2}, thực tế {l.CountedQty:N2}",
                        LineNumber = lineNo,
                        DefectQty = 0,
                        DefectBaseQty = 0
                    });
                    totalAmount += lineAmount;

                    var itemLoc = await _db.ItemLocations.FirstOrDefaultAsync(il =>
                        il.ItemId == l.ItemId
                        && il.LocationId == l.LocationId
                        && il.LotNumber == l.LotNumber
                        && il.ExpiryDate == l.ExpiryDate);
                    if (itemLoc == null)
                    {
                        itemLoc = new ItemLocation
                        {
                            ItemId = l.ItemId,
                            LocationId = l.LocationId,
                            LotNumber = l.LotNumber,
                            ExpiryDate = l.ExpiryDate,
                            Quantity = 0,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.ItemLocations.Add(itemLoc);
                    }
                    itemLoc.Quantity += baseQty;
                    if (itemLoc.Quantity < 0)
                        throw new Exception($"Điều chỉnh kiểm kê làm âm tồn vị trí cho mã {item.ItemCode}.");

                    item.CurrentStock += baseQty;
                    if (item.CurrentStock < 0)
                        throw new Exception($"Điều chỉnh kiểm kê làm âm tổng tồn cho mã {item.ItemCode}.");
                    item.TotalStockValue = item.CurrentStock * item.UnitCost;
                    item.UpdatedAt = DateTime.UtcNow;
                }

                createdVoucher.TotalLines = lineNo;
                createdVoucher.TotalAmount = totalAmount;
                sheet.GeneratedAdjustmentVoucherId = createdVoucher.VoucherId;
            }

            sheet.Status = 2;
            sheet.ApprovedBy = approver;
            sheet.ApprovedAt = DateTime.UtcNow;
            sheet.ApprovalReason = approvalReason;
            sheet.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            if (sheet.GeneratedAdjustmentVoucherId.HasValue)
            {
                TempData["Success"] = $"Đã duyệt phiếu kiểm kê #{sheet.StockCountSheetId} và sinh phiếu điều chỉnh.";
                return RedirectToAction("Details", "Vouchers", new { id = sheet.GeneratedAdjustmentVoucherId.Value });
            }

            TempData["Success"] = $"Đã duyệt phiếu kiểm kê #{sheet.StockCountSheetId}. Không có chênh lệch.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            TempData["Error"] = $"Lỗi duyệt phiếu kiểm kê: {ex.Message}";
            return RedirectToAction(nameof(StockCount), new { warehouseId = redirectWarehouseId, countDate = redirectCountDate });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockCountUnlockApproved(long id, string? unlockReason)
    {
        if (string.IsNullOrWhiteSpace(unlockReason))
        {
            TempData["Error"] = "Vui lòng nhập lý do mở khóa.";
            return RedirectToAction(nameof(StockCount));
        }
        unlockReason = unlockReason.Trim();
        if (unlockReason.Length > 500)
            unlockReason = unlockReason[..500];

        var sheet = await _db.StockCountSheets
            .FirstOrDefaultAsync(s => s.StockCountSheetId == id);
        if (sheet == null)
        {
            TempData["Error"] = "Không tìm thấy phiếu kiểm kê.";
            return RedirectToAction(nameof(StockCount));
        }
        if (sheet.Status != 2)
        {
            TempData["Error"] = "Chỉ phiếu đã duyệt mới được mở khóa.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
        }

        if (sheet.GeneratedAdjustmentVoucherId.HasValue)
        {
            var voucher = await _db.Vouchers
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VoucherId == sheet.GeneratedAdjustmentVoucherId.Value);
            if (voucher != null && !voucher.IsCancelled)
            {
                TempData["Error"] = "Phiếu điều chỉnh phát sinh từ kiểm kê chưa hủy. Vui lòng hủy phiếu điều chỉnh trước khi mở khóa kiểm kê.";
                return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
            }

            var hasChildren = await _db.Vouchers.AsNoTracking()
                .AnyAsync(v => v.ParentVoucherId == sheet.GeneratedAdjustmentVoucherId.Value && !v.IsCancelled);
            if (hasChildren)
            {
                TempData["Error"] = "Phiếu điều chỉnh đã được tham chiếu bởi nghiệp vụ khác. Không thể mở khóa.";
                return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
            }
        }

        sheet.Status = 1;
        sheet.ApprovedBy = null;
        sheet.ApprovedAt = null;
        sheet.ApprovalReason = null;
        sheet.CompletedAt = null;
        sheet.GeneratedAdjustmentVoucherId = null;
        sheet.UnlockedBy = User.Identity?.Name ?? "system";
        sheet.UnlockedAt = DateTime.UtcNow;
        sheet.UnlockReason = unlockReason;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã mở khóa phiếu kiểm kê #{sheet.StockCountSheetId}.";
        return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> PeriodLocks()
    {
        var scopedWh = GetScopedWarehouseId();
        var whQuery = _db.Warehouses.Where(w => w.IsActive);
        if (scopedWh.HasValue) whQuery = whQuery.Where(w => w.WarehouseId == scopedWh.Value);

        ViewBag.Warehouses = await whQuery.OrderBy(w => w.WarehouseCode).ToListAsync();
        var locksQuery = _db.WarehousePeriodLocks
            .Include(l => l.Warehouse)
            .AsQueryable();
        if (scopedWh.HasValue) locksQuery = locksQuery.Where(l => l.WarehouseId == scopedWh.Value);

        var locks = await locksQuery
            .OrderByDescending(l => l.IsActive)
            .ThenByDescending(l => l.LockDate)
            .Take(200)
            .ToListAsync();
        return View(locks);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPeriodLock(int warehouseId, DateTime lockDate, string? reason)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && warehouseId != scopedWh.Value) return Forbid();

        var wh = await _db.Warehouses.FirstOrDefaultAsync(w => w.WarehouseId == warehouseId && w.IsActive);
        if (wh == null)
        {
            TempData["Error"] = "Kho không hợp lệ.";
            return RedirectToAction(nameof(PeriodLocks));
        }

        var active = await _db.WarehousePeriodLocks
            .FirstOrDefaultAsync(l => l.WarehouseId == warehouseId && l.IsActive);
        if (active == null)
        {
            active = new WarehousePeriodLock
            {
                WarehouseId = warehouseId,
                LockDate = lockDate.Date,
                Reason = reason,
                LockedBy = User.Identity?.Name ?? "system",
                LockedAt = DateTime.UtcNow,
                IsActive = true
            };
            _db.WarehousePeriodLocks.Add(active);
        }
        else
        {
            active.LockDate = lockDate.Date;
            active.Reason = reason;
            active.LockedBy = User.Identity?.Name ?? "system";
            active.LockedAt = DateTime.UtcNow;
            active.IsActive = true;
            active.UnlockedAt = null;
            active.UnlockedBy = null;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã khóa kỳ kho {wh.WarehouseCode} đến ngày {lockDate:dd/MM/yyyy}.";
        return RedirectToAction(nameof(PeriodLocks));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearPeriodLock(long id)
    {
        var lockRow = await _db.WarehousePeriodLocks
            .Include(l => l.Warehouse)
            .FirstOrDefaultAsync(l => l.WarehousePeriodLockId == id);
        if (lockRow == null) return NotFound();

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && lockRow.WarehouseId != scopedWh.Value) return Forbid();

        lockRow.IsActive = false;
        lockRow.UnlockedAt = DateTime.UtcNow;
        lockRow.UnlockedBy = User.Identity?.Name ?? "system";
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã mở khóa kỳ cho kho {lockRow.Warehouse?.WarehouseCode}.";
        return RedirectToAction(nameof(PeriodLocks));
    }

    public async Task<IActionResult> StockMovement(int? itemId, int? warehouseId, DateTime? dateFrom, DateTime? dateTo)
    {
        dateFrom ??= DateTime.UtcNow.Date.AddDays(-30);
        dateTo ??= DateTime.UtcNow.Date;

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var query = _db.VoucherDetails
            .Include(vd => vd.Voucher).ThenInclude(v => v!.Warehouse)
            .Include(vd => vd.Item).ThenInclude(i => i!.BaseUom)
            .Include(vd => vd.Location)
            .Include(vd => vd.TransactionUom)
            .Where(vd => vd.Voucher != null
                && !vd.Voucher.IsCancelled
                && vd.Voucher.IsPosted
                && vd.Voucher.VoucherDate >= dateFrom.Value
                && vd.Voucher.VoucherDate <= dateTo.Value);

        if (itemId.HasValue)
            query = query.Where(vd => vd.ItemId == itemId.Value);
        if (warehouseId.HasValue)
            query = query.Where(vd => vd.Voucher!.WarehouseId == warehouseId.Value);

        var data = await query.OrderByDescending(vd => vd.Voucher!.VoucherDate)
            .ThenBy(vd => vd.LineNumber).Take(500).ToListAsync();

        ViewBag.Items = await _db.Items.Where(i => i.IsActive).OrderBy(i => i.ItemCode).ToListAsync();
        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
        ViewBag.ItemId = itemId;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;
        ViewBag.Data = data;

        return View();
    }

    public async Task<IActionResult> ExportStockMovement(int? itemId, int? warehouseId, DateTime? dateFrom, DateTime? dateTo)
    {
        dateFrom ??= DateTime.UtcNow.Date.AddDays(-30);
        dateTo ??= DateTime.UtcNow.Date;

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var query = _db.VoucherDetails.AsNoTracking()
            .Include(vd => vd.Voucher).ThenInclude(v => v!.Warehouse)
            .Include(vd => vd.Item).ThenInclude(i => i!.BaseUom)
            .Include(vd => vd.TransactionUom)
            .Where(vd => vd.Voucher != null
                && !vd.Voucher.IsCancelled
                && vd.Voucher.IsPosted
                && vd.Voucher.VoucherDate >= dateFrom.Value
                && vd.Voucher.VoucherDate <= dateTo.Value);

        if (itemId.HasValue)
            query = query.Where(vd => vd.ItemId == itemId.Value);
        if (warehouseId.HasValue)
            query = query.Where(vd => vd.Voucher!.WarehouseId == warehouseId.Value);

        var data = await query
            .OrderByDescending(vd => vd.Voucher!.VoucherDate)
            .ThenBy(vd => vd.LineNumber)
            .Take(2000)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("XuatNhapTon");

        var row = 1;
        ws.Cell(row, 1).Value = "Ngày";
        ws.Cell(row, 2).Value = "Mã phiếu";
        ws.Cell(row, 3).Value = "Loại phiếu";
        ws.Cell(row, 4).Value = "Kho";
        ws.Cell(row, 5).Value = "Mã VT";
        ws.Cell(row, 6).Value = "Tên VT";
        ws.Cell(row, 7).Value = "SL (+/-)";
        ws.Cell(row, 8).Value = "ĐVT";

        ws.Range("A1:H1").Style.Font.Bold = true;
        ws.Range("A1:H1").Style.Fill.BackgroundColor = XLColor.AirForceBlue;
        ws.Range("A1:H1").Style.Font.FontColor = XLColor.White;

        foreach (var d in data)
        {
            row++;
            var v = d.Voucher!;

            ws.Cell(row, 1).Value = v.VoucherDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 2).Value = v.VoucherCode;
            ws.Cell(row, 3).Value = v.VoucherTypeName;
            ws.Cell(row, 4).Value = v.Warehouse?.WarehouseName ?? "";
            ws.Cell(row, 5).Value = d.Item?.ItemCode ?? "";
            ws.Cell(row, 6).Value = d.Item?.ItemName ?? "";

            var signedQty = v.VoucherType switch
            {
                1 or 4 or 7 => d.BaseQty,
                2 or 3 or 8 => -d.BaseQty,
                5 => d.BaseQty, // already carries sign (+/-)
                6 => 0m,        // transfer does not change total stock
                _ => 0m
            };
            ws.Cell(row, 7).Value = signedQty;
            ws.Cell(row, 8).Value = d.Item?.BaseUom?.UomCode ?? d.TransactionUom?.UomCode ?? "N/A";
        }

        ws.Column(7).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        var fileName = $"XuatNhapTon_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    public async Task<IActionResult> Inventory(int? warehouseId, int? categoryId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var query = _db.Items.AsNoTracking().Include(i => i.Category).Include(i => i.BaseUom)
            .Where(i => i.IsActive).AsQueryable();

        if (categoryId.HasValue)
        {
            var targetCategoryIds = await _db.ItemCategories
                .Where(c => c.CategoryId == categoryId.Value || c.ParentCategoryId == categoryId.Value)
                .Select(c => c.CategoryId)
                .ToListAsync();

            query = query.Where(i => i.CategoryId.HasValue && targetCategoryIds.Contains(i.CategoryId.Value));
        }

        var items = await query.OrderBy(i => i.ItemCode).ToListAsync();

        if (warehouseId.HasValue)
        {
            // Calculate stock dynamically per warehouse and filter out zero-stock items
            var itemLocs = await _db.ItemLocations.AsNoTracking()
                .Include(il => il.Location).ThenInclude(l => l!.Zone)
                .Where(il => il.Location != null && il.Location.Zone != null && il.Location.Zone.WarehouseId == warehouseId.Value && il.Quantity > 0)
                .ToListAsync();

            foreach(var item in items)
            {
                item.CurrentStock = itemLocs.Where(il => il.ItemId == item.ItemId).Sum(il => il.Quantity);
                item.TotalStockValue = item.CurrentStock * item.UnitCost;
            }
            items = items.Where(i => i.CurrentStock > 0).ToList();
        }

        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
        ViewBag.Categories = await _db.ItemCategories.Where(c => c.IsActive).ToListAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.CategoryId = categoryId;

        return View(items);
    }

    public async Task<IActionResult> ExportInventory(int? warehouseId, int? categoryId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var query = _db.Items.AsNoTracking().Include(i => i.Category).Include(i => i.BaseUom)
            .Where(i => i.IsActive).AsQueryable();

        if (categoryId.HasValue)
        {
            var targetCategoryIds = await _db.ItemCategories
                .Where(c => c.CategoryId == categoryId.Value || c.ParentCategoryId == categoryId.Value)
                .Select(c => c.CategoryId)
                .ToListAsync();

            query = query.Where(i => i.CategoryId.HasValue && targetCategoryIds.Contains(i.CategoryId.Value));
        }

        var items = await query.OrderBy(i => i.ItemCode).ToListAsync();

        if (warehouseId.HasValue)
        {
            var itemLocs = await _db.ItemLocations.AsNoTracking()
                .Include(il => il.Location).ThenInclude(l => l!.Zone)
                .Where(il => il.Location != null && il.Location.Zone != null && il.Location.Zone.WarehouseId == warehouseId.Value && il.Quantity > 0)
                .ToListAsync();

            foreach(var item in items)
            {
                item.CurrentStock = itemLocs.Where(il => il.ItemId == item.ItemId).Sum(il => il.Quantity);
                item.TotalStockValue = item.CurrentStock * item.UnitCost;
            }
            items = items.Where(i => i.CurrentStock > 0).ToList();
        }

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("BaoCaoTonKho");
            var currentRow = 1;

            // Header Row
            worksheet.Cell(currentRow, 1).Value = "Mã VT";
            worksheet.Cell(currentRow, 2).Value = "Tên Vật Tư";
            worksheet.Cell(currentRow, 3).Value = "Loại";
            worksheet.Cell(currentRow, 4).Value = "Danh Mục";
            worksheet.Cell(currentRow, 5).Value = "ĐVT";
            worksheet.Cell(currentRow, 6).Value = "Tồn Kho";
            worksheet.Cell(currentRow, 7).Value = "Giá Vốn BQ";
            worksheet.Cell(currentRow, 8).Value = "Tổng Tiền (VNĐ)";
            
            // Header Styling
            worksheet.Range("A1:H1").Style.Font.Bold = true;
            worksheet.Range("A1:H1").Style.Fill.BackgroundColor = XLColor.AirForceBlue;
            worksheet.Range("A1:H1").Style.Font.FontColor = XLColor.White;

            foreach (var item in items)
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = item.ItemCode;
                worksheet.Cell(currentRow, 2).Value = item.ItemName;
                worksheet.Cell(currentRow, 3).Value = item.ItemTypeName;
                worksheet.Cell(currentRow, 4).Value = item.Category?.CategoryName ?? "---";
                worksheet.Cell(currentRow, 5).Value = item.BaseUom?.UomCode;
                worksheet.Cell(currentRow, 6).Value = item.CurrentStock;
                worksheet.Cell(currentRow, 7).Value = item.UnitCost;
                worksheet.Cell(currentRow, 8).Value = item.TotalStockValue;
            }

            // Formatting columns
            worksheet.Column(6).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Column(7).Style.NumberFormat.Format = "#,##0";
            worksheet.Column(8).Style.NumberFormat.Format = "#,##0";
            worksheet.Columns().AdjustToContents(); // Auto-fit

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCaoTonKho_{DateTime.Now.ToString("yyyyMMdd_HHmm")}.xlsx");
            }
        }
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> StockSnapshot(int? warehouseId, DateTime? snapshotDate)
    {
        snapshotDate ??= DateTime.UtcNow.Date;

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.SnapshotDate = snapshotDate;

        if (!warehouseId.HasValue)
        {
            return View(new List<StockSnapshotCompareRow>());
        }

        var snapshotRows = await _db.StockSnapshots.AsNoTracking()
            .Include(s => s.Item).ThenInclude(i => i!.BaseUom)
            .Where(s => s.SnapshotDate == snapshotDate.Value && s.WarehouseId == warehouseId.Value)
            .OrderBy(s => s.Item!.ItemCode)
            .ToListAsync();

        // Current stock per item in warehouse
        var currentStocks = await _db.ItemLocations.AsNoTracking()
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.Quantity != 0
                && il.Location != null
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId.Value)
            .GroupBy(il => il.ItemId)
            .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Qty);

        List<StockSnapshotCompareRow> data;

        if (snapshotRows.Count == 0)
        {
            // No snapshot yet -> show PREVIEW of what will be snapshotted (current stock)
            var itemStocks = await _db.ItemLocations.AsNoTracking()
                .Include(il => il.Location).ThenInclude(l => l!.Zone)
                .Where(il => il.Quantity != 0
                    && il.Location != null
                    && il.Location.Zone != null
                    && il.Location.Zone.WarehouseId == warehouseId.Value)
                .GroupBy(il => il.ItemId)
                .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToListAsync();

            var itemIds = itemStocks.Select(x => x.ItemId).ToList();
            var items = await _db.Items.AsNoTracking()
                .Include(i => i.BaseUom)
                .Where(i => i.IsActive && itemIds.Contains(i.ItemId))
                .OrderBy(i => i.ItemCode)
                .ToListAsync();

            var qtyMap = itemStocks.ToDictionary(x => x.ItemId, x => x.Qty);
            data = items.Select(it =>
            {
                var currentQty = qtyMap.TryGetValue(it.ItemId, out var q) ? q : 0m;
                var snapshotValue = currentQty * it.UnitCost;
                return new StockSnapshotCompareRow
                {
                    ItemId = it.ItemId,
                    ItemCode = it.ItemCode,
                    ItemName = it.ItemName,
                    UomCode = it.BaseUom?.UomCode ?? "",
                    SnapshotQty = currentQty, // preview: will be saved as snapshot qty
                    CurrentQty = currentQty,
                    DiffQty = 0,
                    UnitCost = it.UnitCost,
                    SnapshotValue = snapshotValue,
                    CurrentValue = snapshotValue,
                    DiffValue = 0
                };
            }).ToList();

            ViewBag.IsPreview = true;
        }
        else
        {
            data = snapshotRows.Select(s =>
            {
                var currentQty = currentStocks.TryGetValue(s.ItemId, out var q) ? q : 0m;
                var diffQty = s.ClosingStock - currentQty; // needed adjustment to match snapshot
                var snapshotValue = s.ClosingStock * s.UnitCost;
                var currentValue = currentQty * s.UnitCost;
                return new StockSnapshotCompareRow
                {
                    ItemId = s.ItemId,
                    ItemCode = s.Item?.ItemCode ?? "",
                    ItemName = s.Item?.ItemName ?? "",
                    UomCode = s.Item?.BaseUom?.UomCode ?? "",
                    SnapshotQty = s.ClosingStock,
                    CurrentQty = currentQty,
                    DiffQty = diffQty,
                    UnitCost = s.UnitCost,
                    SnapshotValue = snapshotValue,
                    CurrentValue = currentValue,
                    DiffValue = snapshotValue - currentValue
                };
            }).ToList();

            ViewBag.IsPreview = false;
        }

        ViewBag.HasSnapshot = snapshotRows.Count > 0;
        ViewBag.DiffCount = data.Count(x => x.DiffQty != 0);
        return View(data);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateStockSnapshot(int warehouseId, DateTime snapshotDate)
    {
        snapshotDate = snapshotDate.Date;

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && warehouseId != scopedWh.Value)
            return Forbid();

        var wh = await _db.Warehouses.FirstOrDefaultAsync(w => w.WarehouseId == warehouseId && w.IsActive);
        if (wh == null)
        {
            TempData["Error"] = "Kho không hợp lệ.";
            return RedirectToAction(nameof(StockSnapshot), new { warehouseId, snapshotDate });
        }

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Remove existing snapshot for the day+warehouse to allow re-generate
            var existing = await _db.StockSnapshots
                .Where(s => s.WarehouseId == warehouseId && s.SnapshotDate == snapshotDate)
                .ToListAsync();
            if (existing.Count > 0)
            {
                _db.StockSnapshots.RemoveRange(existing);
                await _db.SaveChangesAsync();
            }

            // Aggregate stock per item in warehouse
            var itemStocks = await _db.ItemLocations.AsNoTracking()
                .Include(il => il.Location).ThenInclude(l => l!.Zone)
                .Where(il => il.Quantity != 0
                    && il.Location != null
                    && il.Location.Zone != null
                    && il.Location.Zone.WarehouseId == warehouseId)
                .GroupBy(il => il.ItemId)
                .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToListAsync();

            var itemIds = itemStocks.Select(x => x.ItemId).ToList();
            var items = await _db.Items.AsNoTracking()
                .Where(i => i.IsActive && itemIds.Contains(i.ItemId))
                .ToDictionaryAsync(i => i.ItemId, i => i);

            var snapshots = new List<StockSnapshot>(itemStocks.Count);
            foreach (var s in itemStocks)
            {
                if (!items.TryGetValue(s.ItemId, out var item)) continue;
                var qty = s.Qty;
                var unitCost = item.UnitCost;
                snapshots.Add(new StockSnapshot
                {
                    SnapshotDate = snapshotDate,
                    ItemId = item.ItemId,
                    WarehouseId = warehouseId,
                    ClosingStock = qty,
                    UnitCost = unitCost,
                    TotalValue = qty * unitCost,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (snapshots.Count > 0)
            {
                await _db.StockSnapshots.AddRangeAsync(snapshots);
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            TempData["Success"] = $"Đã chốt tồn kho '{wh.WarehouseName}' ngày {snapshotDate:dd/MM/yyyy} ({snapshots.Count} mã).";
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            TempData["Error"] = $"Lỗi chốt tồn: {ex.Message}";
        }

        return RedirectToAction(nameof(StockSnapshot), new { warehouseId, snapshotDate });
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ExportStockSnapshot(int warehouseId, DateTime snapshotDate)
    {
        snapshotDate = snapshotDate.Date;
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && warehouseId != scopedWh.Value)
            return Forbid();

        var wh = await _db.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.WarehouseId == warehouseId);
        if (wh == null) return NotFound();

        var data = await _db.StockSnapshots.AsNoTracking()
            .Include(s => s.Item).ThenInclude(i => i!.BaseUom)
            .Where(s => s.WarehouseId == warehouseId && s.SnapshotDate == snapshotDate)
            .OrderBy(s => s.Item!.ItemCode)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("ChotTon");

        ws.Cell(1, 1).Value = "Kho";
        ws.Cell(1, 2).Value = wh.WarehouseName;
        ws.Cell(2, 1).Value = "Ngày chốt";
        ws.Cell(2, 2).Value = snapshotDate.ToString("dd/MM/yyyy");

        var row = 4;
        ws.Cell(row, 1).Value = "Mã VT";
        ws.Cell(row, 2).Value = "Tên VT";
        ws.Cell(row, 3).Value = "ĐVT";
        ws.Cell(row, 4).Value = "Tồn chốt";
        ws.Cell(row, 5).Value = "Giá vốn";
        ws.Cell(row, 6).Value = "Thành tiền";

        ws.Range(row, 1, row, 6).Style.Font.Bold = true;
        ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.AirForceBlue;
        ws.Range(row, 1, row, 6).Style.Font.FontColor = XLColor.White;

        foreach (var s in data)
        {
            row++;
            ws.Cell(row, 1).Value = s.Item?.ItemCode ?? "";
            ws.Cell(row, 2).Value = s.Item?.ItemName ?? "";
            ws.Cell(row, 3).Value = s.Item?.BaseUom?.UomCode ?? "";
            ws.Cell(row, 4).Value = s.ClosingStock;
            ws.Cell(row, 5).Value = s.UnitCost;
            ws.Cell(row, 6).Value = s.TotalValue;
        }

        ws.Column(4).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(5).Style.NumberFormat.Format = "#,##0";
        ws.Column(6).Style.NumberFormat.Format = "#,##0";
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        var fileName = $"ChotTon_{wh.WarehouseCode}_{snapshotDate:yyyyMMdd}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AuditTrail(string? tableName, string? changedBy, DateTime? dateFrom, DateTime? dateTo)
    {
        dateFrom ??= DateTime.UtcNow.Date.AddDays(-7);
        dateTo ??= DateTime.UtcNow.Date.AddDays(1);

        var query = _db.AuditLogs
            .Where(a => a.ChangedAt >= dateFrom.Value && a.ChangedAt <= dateTo.Value);

        if (!string.IsNullOrWhiteSpace(tableName))
            query = query.Where(a => a.TableName == tableName);
        if (!string.IsNullOrWhiteSpace(changedBy))
            query = query.Where(a => a.ChangedBy != null && a.ChangedBy.Contains(changedBy));

        ViewBag.TableName = tableName;
        ViewBag.ChangedBy = changedBy;
        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;

        var logs = await query.OrderByDescending(a => a.ChangedAt).Take(200).ToListAsync();
        return View(logs);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Alerts(byte? type, bool? unresolvedOnly, int days = 30)
    {
        unresolvedOnly ??= true;
        if (days < 1) days = 1;
        if (days > 365) days = 365;

        ViewBag.Type = type;
        ViewBag.UnresolvedOnly = unresolvedOnly;
        ViewBag.Days = days;

        var query = _db.StockAlerts.AsNoTracking()
            .Include(a => a.Item)
            .Where(a => a.Item != null && a.Item.IsActive);

        if (type.HasValue) query = query.Where(a => a.AlertType == type.Value);
        if (unresolvedOnly == true) query = query.Where(a => !a.IsResolved);

        var alerts = await query.OrderBy(a => a.IsResolved).ThenByDescending(a => a.CreatedAt).Take(500).ToListAsync();
        return View(alerts);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshExpiryAlerts(int days = 30)
    {
        if (days < 1) days = 1;
        if (days > 365) days = 365;

        var today = DateTime.UtcNow.Date;
        var cutoff = today.AddDays(days);

        // Aggregate expiring quantity per item (within window) and the nearest expiry date
        var expiring = await _db.ItemLocations.AsNoTracking()
            .Include(il => il.Item)
            .Where(il => il.Quantity > 0
                && il.Item != null
                && il.Item.IsActive
                && il.ExpiryDate.HasValue
                && il.ExpiryDate.Value.Date <= cutoff)
            .GroupBy(il => il.ItemId)
            .Select(g => new
            {
                ItemId = g.Key,
                Qty = g.Sum(x => x.Quantity),
                NearestExpiry = g.Min(x => x.ExpiryDate)
            })
            .ToListAsync();

        var expiringMap = expiring.ToDictionary(x => x.ItemId, x => x);

        // Upsert unresolved expiry alerts
        var existing = await _db.StockAlerts
            .Where(a => a.AlertType == 3 && !a.IsResolved)
            .ToListAsync();

        foreach (var alert in existing)
        {
            if (!expiringMap.TryGetValue(alert.ItemId, out var e) || e.NearestExpiry == null)
            {
                alert.IsResolved = true;
                alert.ResolvedAt = DateTime.UtcNow;
                continue;
            }

            var nearest = e.NearestExpiry.Value.Date;
            var daysLeft = (nearest - today).Days;
            alert.CurrentStock = e.Qty;
            alert.Threshold = daysLeft;
            alert.IsRead = false;
        }

        foreach (var e in expiring)
        {
            if (e.NearestExpiry == null) continue;
            if (existing.Any(a => a.ItemId == e.ItemId)) continue;

            var nearest = e.NearestExpiry.Value.Date;
            var daysLeft = (nearest - today).Days;

            _db.StockAlerts.Add(new StockAlert
            {
                ItemId = e.ItemId,
                AlertType = 3, // Expiry
                CurrentStock = e.Qty, // expiring qty
                Threshold = daysLeft, // days left
                IsRead = false,
                IsResolved = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã làm mới cảnh báo hết hạn (<= {days} ngày).";
        return RedirectToAction(nameof(Alerts), new { type = (byte)3, unresolvedOnly = true, days });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveAlert(long id, byte? type, bool? unresolvedOnly, int days = 30)
    {
        var alert = await _db.StockAlerts.FindAsync(id);
        if (alert == null) return NotFound();
        alert.IsResolved = true;
        alert.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Alerts), new { type, unresolvedOnly, days });
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> OpsKpi(int? warehouseId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var wavesQuery = _db.Waves.AsNoTracking().AsQueryable();
        if (warehouseId.HasValue) wavesQuery = wavesQuery.Where(w => w.WarehouseId == warehouseId.Value);
        var waveIds = await wavesQuery.Select(w => w.WaveId).ToListAsync();

        var tasksQuery = _db.PickTasks.AsNoTracking().Where(t => waveIds.Contains(t.WaveId));
        var totalTasks = await tasksQuery.CountAsync();
        var doneTasks = await tasksQuery.CountAsync(t => t.Status == 4);
        var shortTasks = await tasksQuery.CountAsync(t => t.Status == 5);
        var openTasks = await tasksQuery.CountAsync(t => t.Status == 1 || t.Status == 2 || t.Status == 3);
        var completedDurations = await tasksQuery
            .Where(t => t.CompletedAt.HasValue && t.AssignedAt.HasValue)
            .Select(t => EF.Functions.DateDiffMinute(t.AssignedAt!.Value, t.CompletedAt!.Value))
            .ToListAsync();
        var avgMinutes = completedDurations.Count > 0 ? completedDurations.Average() : 0;

        var reservations = await _db.StockReservations.AsNoTracking()
            .Where(r => !warehouseId.HasValue || (r.Voucher != null && r.Voucher.WarehouseId == warehouseId.Value))
            .ToListAsync();
        var reserved = reservations.Sum(r => r.ReservedQty);
        var consumed = reservations.Sum(r => r.ConsumedQty);
        var fillRate = reserved > 0 ? (consumed / reserved) * 100m : 0m;

        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.TotalTasks = totalTasks;
        ViewBag.DoneTasks = doneTasks;
        ViewBag.ShortTasks = shortTasks;
        ViewBag.OpenTasks = openTasks;
        ViewBag.AvgMinutes = avgMinutes;
        ViewBag.FillRate = fillRate;
        ViewBag.WaveCount = waveIds.Count;

        var recentWaves = await wavesQuery
            .OrderByDescending(w => w.CreatedAt)
            .Take(10)
            .Select(w => new WaveBoardRow
            {
                WaveId = w.WaveId,
                WaveCode = w.WaveCode,
                WarehouseId = w.WarehouseId,
                WarehouseName = w.Warehouse != null ? w.Warehouse.WarehouseCode + " - " + w.Warehouse.WarehouseName : "",
                Status = w.Status,
                OpenTasks = _db.PickTasks.Count(t => t.WaveId == w.WaveId && (t.Status == 1 || t.Status == 2 || t.Status == 3)),
                DoneTasks = _db.PickTasks.Count(t => t.WaveId == w.WaveId && t.Status == 4),
                CreatedAt = w.CreatedAt,
                CompletedAt = w.CompletedAt
            })
            .ToListAsync();

        var recentTasksQuery = _db.PickTasks.AsNoTracking()
            .Include(t => t.Wave)
            .Include(t => t.Voucher)
            .Include(t => t.Item)
            .Include(t => t.SourceLocation)
            .AsQueryable();
        if (warehouseId.HasValue)
            recentTasksQuery = recentTasksQuery.Where(t => t.Wave != null && t.Wave.WarehouseId == warehouseId.Value);

        var recentTasks = await recentTasksQuery
            .OrderByDescending(t => t.AssignedAt ?? t.CompletedAt ?? DateTime.MinValue)
            .ThenByDescending(t => t.PickTaskId)
            .Take(20)
            .Select(t => new PickTaskBoardRow
            {
                PickTaskId = t.PickTaskId,
                TaskCode = t.TaskCode,
                WaveId = t.WaveId,
                WaveCode = t.Wave != null ? t.Wave.WaveCode : "",
                VoucherCode = t.Voucher != null ? t.Voucher.VoucherCode : "",
                ItemCode = t.Item != null ? t.Item.ItemCode : "",
                LocationCode = t.SourceLocation != null ? t.SourceLocation.LocationCode : "",
                TargetQty = t.TargetQty,
                PickedQty = t.PickedQty,
                Status = t.Status,
                AssignedTo = t.AssignedTo,
                CompletedAt = t.CompletedAt
            })
            .ToListAsync();

        ViewBag.RecentWaves = (object)recentWaves;
        ViewBag.RecentTasks = (object)recentTasks;
        return View();
    }
}
