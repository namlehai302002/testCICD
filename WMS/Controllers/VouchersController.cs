using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;
using WMS.ViewModels;
using System.Text.Json;
using ClosedXML.Excel;
using System.Globalization;
using System.Data;

namespace WMS.Controllers;

public class VouchersController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public VouchersController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    private int? GetScopedWarehouseId()
    {
        if (User.IsInRole("Admin")) return null;
        var claim = User.FindFirst("WarehouseId")?.Value;
        return int.TryParse(claim, out var id) ? id : -1;
    }

    private async Task<DateTime?> GetActiveLockDateAsync(int warehouseId)
    {
        return await _db.WarehousePeriodLocks.AsNoTracking()
            .Where(l => l.WarehouseId == warehouseId && l.IsActive)
            .OrderByDescending(l => l.LockDate)
            .Select(l => (DateTime?)l.LockDate)
            .FirstOrDefaultAsync();
    }

    private static bool IsLocked(DateTime voucherDate, DateTime? lockDate)
    {
        return lockDate.HasValue && voucherDate.Date <= lockDate.Value.Date;
    }

    private async Task<int?> GetLocationWarehouseIdAsync(int locationId)
    {
        return await _db.Locations.AsNoTracking()
            .Include(l => l.Zone)
            .Where(l => l.LocationId == locationId && l.Zone != null)
            .Select(l => (int?)l.Zone!.WarehouseId)
            .FirstOrDefaultAsync();
    }

    private sealed record FefoAllocation(int LocationId, string? LotNumber, DateTime? ExpiryDate, decimal Qty);

    private async Task<List<FefoAllocation>> AllocateFefoAsync(int itemId, int warehouseId, decimal requiredBaseQty)
    {
        var remaining = requiredBaseQty;
        var picks = new List<FefoAllocation>();
        if (requiredBaseQty <= 0) return picks;

        var candidates = await _db.ItemLocations
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.ItemId == itemId
                && il.Quantity > il.ReservedQty
                && il.Location != null
                && il.Location.IsActive
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId)
            .OrderBy(il => il.ExpiryDate == null)
            .ThenBy(il => il.ExpiryDate)
            .ThenByDescending(il => il.Quantity - il.ReservedQty)
            .ToListAsync();

        foreach (var c in candidates)
        {
            var available = c.Quantity - c.ReservedQty;
            if (available <= 0) continue;
            var take = Math.Min(remaining, available);
            if (take <= 0) continue;

            picks.Add(new FefoAllocation(c.LocationId, c.LotNumber, c.ExpiryDate, take));
            remaining -= take;
            if (remaining <= 0) break;
        }

        if (remaining > 0)
            throw new Exception($"Không đủ tồn khả dụng (available) cho vật tư #{itemId}. Thiếu {remaining:N2}.");

        return picks;
    }

    private async Task RecalculateReservedQtyAsync(IEnumerable<int> itemLocationIds)
    {
        var ids = itemLocationIds.Distinct().ToList();
        if (ids.Count == 0) return;

        var locRows = await _db.ItemLocations.Where(x => ids.Contains(x.ItemLocationId)).ToListAsync();
        var keyRows = locRows.Select(l => new { l.ItemId, l.LocationId, l.LotNumber, l.ExpiryDate }).Distinct().ToList();
        var itemIds = keyRows.Select(k => k.ItemId).Distinct().ToList();
        var locationIds = keyRows.Select(k => k.LocationId).Distinct().ToList();
        var activeReservations = await _db.StockReservations
            .Where(r => r.Status == 1 && itemIds.Contains(r.ItemId) && locationIds.Contains(r.LocationId))
            .Select(r => new { r.ItemId, r.LocationId, r.LotNumber, r.ExpiryDate, OpenQty = (r.ReservedQty - r.ConsumedQty - r.ReleasedQty) })
            .ToListAsync();

        foreach (var loc in locRows)
        {
            var reserved = activeReservations
                .Where(r => r.ItemId == loc.ItemId
                    && r.LocationId == loc.LocationId
                    && r.LotNumber == loc.LotNumber
                    && r.ExpiryDate == loc.ExpiryDate)
                .Sum(r => r.OpenQty);
            loc.ReservedQty = Math.Max(0, reserved);
            loc.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<int?> GetFefoLocationIdAsync(int itemId, int warehouseId, decimal requiredBaseQty)
    {
        // FEFO: pick location with earliest expiry (nulls last), then most stock
        // If none has enough stock, still return best FEFO location (caller may fail on negative stock check later).
        var candidates = await _db.ItemLocations.AsNoTracking()
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.ItemId == itemId
                && il.Quantity > 0
                && il.Location != null
                && il.Location.IsActive
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId)
            .OrderBy(il => il.ExpiryDate == null) // non-null first
            .ThenBy(il => il.ExpiryDate)          // earliest expiry
            .ThenByDescending(il => il.Quantity)  // more stock first
            .Take(20)
            .ToListAsync();

        if (candidates.Count == 0) return null;

        var enough = candidates.FirstOrDefault(c => c.Quantity >= requiredBaseQty);
        return (enough ?? candidates[0]).LocationId;
    }

    public async Task<IActionResult> Index(byte? type, DateTime? dateFrom, DateTime? dateTo, string? search)
    {
        var query = _db.Vouchers
            .Include(v => v.Warehouse).Include(v => v.Partner)
            .Include(v => v.Details)
            .AsQueryable();

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            query = query.Where(v => v.WarehouseId == scopedWh.Value);

        if (type.HasValue)
            query = query.Where(v => v.VoucherType == type.Value);
        if (dateFrom.HasValue)
            query = query.Where(v => v.VoucherDate >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(v => v.VoucherDate <= dateTo.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.VoucherCode.Contains(search) || (v.Description != null && v.Description.Contains(search)));

        ViewBag.Type = type;
        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;
        ViewBag.Search = search;

        var vouchers = await query.OrderByDescending(v => v.CreatedAt).Take(200).ToListAsync();
        return View(vouchers);
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> Create(byte type = 1)
    {
        var scopedWh = GetScopedWarehouseId();
        var vm = new VoucherCreateViewModel
        {
            VoucherType = type,
            Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync(),
            Partners = await _db.Partners.Where(p => p.IsActive).ToListAsync(),
            Items = await _db.Items
                .Include(i => i.BaseUom)
                .Where(i => i.IsActive)
                .OrderBy(i => i.ItemCode)
                .ToListAsync(),
            Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).ToListAsync(),
            Locations = await _db.Locations.Where(l => l.IsActive).ToListAsync(),
            PackagingUnits = await _db.PackagingUnits
                .Include(p => p.BaseUom)
                .Where(p => p.IsActive)
                .OrderBy(p => p.TenDongGoi)
                .ToListAsync()
        };

        if (scopedWh.HasValue)
            vm.WarehouseId = scopedWh.Value;
        return View(vm);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> CreateAdjustmentFromSnapshot(int warehouseId, DateTime snapshotDate)
    {
        snapshotDate = snapshotDate.Date;
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && warehouseId != scopedWh.Value)
            return Forbid();

        var snapshotRows = await _db.StockSnapshots.AsNoTracking()
            .Where(s => s.WarehouseId == warehouseId && s.SnapshotDate == snapshotDate)
            .ToListAsync();

        if (snapshotRows.Count == 0)
        {
            TempData["Error"] = "Chưa có snapshot cho kho/ngày đã chọn. Vui lòng chốt tồn trước.";
            return RedirectToAction("StockSnapshot", "Reports", new { warehouseId, snapshotDate });
        }

        var currentStocks = await _db.ItemLocations.AsNoTracking()
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.Quantity != 0
                && il.Location != null
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId)
            .GroupBy(il => il.ItemId)
            .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Qty);

        var itemIds = snapshotRows.Select(s => s.ItemId).Distinct().ToList();
        var items = await _db.Items.AsNoTracking().Where(i => i.IsActive && itemIds.Contains(i.ItemId)).ToListAsync();
        var itemById = items.ToDictionary(i => i.ItemId, i => i);

        // Location suggestion for decrease: pick a location in the warehouse that has stock for the item
        var stockLocs = await _db.ItemLocations.AsNoTracking()
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.Quantity > 0
                && il.Location != null
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId
                && itemIds.Contains(il.ItemId))
            .OrderByDescending(il => il.Quantity)
            .ToListAsync();

        var bestLocByItem = stockLocs
            .GroupBy(il => il.ItemId)
            .ToDictionary(g => g.Key, g => g.First().LocationId);

        var lines = new List<VoucherDetailLine>();
        foreach (var s in snapshotRows)
        {
            if (!itemById.TryGetValue(s.ItemId, out var item)) continue;
            var currentQty = currentStocks.TryGetValue(s.ItemId, out var q) ? q : 0m;
            var diff = s.ClosingStock - currentQty; // needed adjustment to match snapshot
            if (diff == 0) continue;

            var sign = diff > 0 ? (sbyte)1 : (sbyte)-1;
            var abs = Math.Abs(diff);

            lines.Add(new VoucherDetailLine
            {
                ItemId = s.ItemId,
                TransactionQty = abs,
                TransactionUomId = item.BaseUomId,
                AdjustSign = sign,
                LocationId = sign < 0
                    ? (bestLocByItem.TryGetValue(s.ItemId, out var locId) ? locId : item.DefaultLocationId)
                    : (item.DefaultLocationId ?? (bestLocByItem.TryGetValue(s.ItemId, out var locId2) ? locId2 : null)),
                UnitPrice = item.UnitCost,
                LineAmount = item.UnitCost * abs,
                Notes = $"Điều chỉnh theo snapshot {snapshotDate:dd/MM/yyyy}"
            });
        }

        var vm = new VoucherCreateViewModel
        {
            VoucherType = 5,
            WarehouseId = warehouseId,
            ReferenceNo = $"SNAP-{snapshotDate:yyyyMMdd}",
            Description = $"Điều chỉnh tồn theo snapshot ngày {snapshotDate:dd/MM/yyyy}",
            Lines = lines
        };

        // Populate dropdown data as in Create()
        vm.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
        vm.Partners = await _db.Partners.Where(p => p.IsActive).ToListAsync();
        vm.Items = await _db.Items.Include(i => i.BaseUom).Where(i => i.IsActive).OrderBy(i => i.ItemCode).ToListAsync();
        vm.Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).ToListAsync();
        vm.Locations = await _db.Locations.Where(l => l.IsActive).ToListAsync();
        vm.PackagingUnits = await _db.PackagingUnits.Include(p => p.BaseUom).Where(p => p.IsActive).OrderBy(p => p.TenDongGoi).ToListAsync();

        TempData["Info"] = $"Đã tạo nháp phiếu điều chỉnh theo snapshot {snapshotDate:dd/MM/yyyy}. Vui lòng kiểm tra và lưu.";
        return View("Create", vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> Create(VoucherCreateViewModel vm)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var scopedWh = GetScopedWarehouseId();
            if (scopedWh.HasValue && vm.WarehouseId != scopedWh.Value)
                return Forbid();

            var createVoucherDate = DateTime.UtcNow.Date;
            var lockDate = await GetActiveLockDateAsync(vm.WarehouseId);
            if (IsLocked(createVoucherDate, lockDate))
                throw new Exception($"Kho đã khóa kỳ đến {lockDate:dd/MM/yyyy}. Không thể tạo phiếu ngày {createVoucherDate:dd/MM/yyyy}.");

            // Basic cross-field validations for correctness
            if (vm.VoucherType == 6) // Transfer
            {
                if (!vm.DestWarehouseId.HasValue || vm.DestWarehouseId.Value <= 0)
                    throw new Exception("Phiếu chuyển kho phải chọn kho đích.");
                if (vm.DestWarehouseId.Value == vm.WarehouseId)
                    throw new Exception("Kho đích không được trùng kho nguồn.");
            }

            // Generate voucher code
            var prefix = vm.VoucherType switch
            {
                1 => "PN", 2 => "PX", 3 => "PTN", 4 => "PTK",
                5 => "PDC", 6 => "PCK", 7 => "NTP", 8 => "XSX", _ => "PH"
            };
            var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
            Voucher? voucher = null;
            var voucherCode = "";
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var seq = await _db.Vouchers
                    .Where(v => v.VoucherCode.StartsWith(prefix + "-" + dateStr))
                    .CountAsync() + 1 + attempt;
                voucherCode = $"{prefix}-{dateStr}-{seq:D5}";

                voucher = new Voucher
                {
                    VoucherCode = voucherCode,
                    VoucherType = vm.VoucherType,
                    VoucherDate = createVoucherDate,
                    WarehouseId = vm.WarehouseId,
                    DestWarehouseId = vm.DestWarehouseId,
                    PartnerId = vm.PartnerId,
                    ReferenceNo = vm.ReferenceNo,
                    Description = vm.Description,
                    SourceType = 1,
                    CreatedBy = User.Identity?.Name ?? "system",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ParentVoucherId = vm.ParentVoucherId,
                    IsPosted = vm.VoucherType is 4 ? true : false,
                    FulfillmentStatus = 1
                };

                _db.Vouchers.Add(voucher);
                try
                {
                    await _db.SaveChangesAsync();
                    break;
                }
                catch (DbUpdateException ex) when ((ex.InnerException?.Message?.Contains("2601") ?? false)
                    || (ex.InnerException?.Message?.Contains("2627") ?? false)
                    || (ex.InnerException?.Message?.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ?? false)
                    || (ex.InnerException?.Message?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    _db.Entry(voucher).State = EntityState.Detached;
                    voucher = null;
                    if (attempt == 9)
                        throw new Exception("Không thể tạo mã phiếu do trùng mã đồng thời. Vui lòng thử lại.");
                }
            }
            if (voucher == null || string.IsNullOrWhiteSpace(voucherCode))
                throw new Exception("Không thể tạo mã phiếu. Vui lòng thử lại.");

            // If this is a replenishment, find the original voucher and resolve its defects
            if (vm.ParentVoucherId.HasValue)
            {
                var original = await _db.Vouchers.Include(v => v.Details)
                    .FirstOrDefaultAsync(v => v.VoucherId == vm.ParentVoucherId.Value);
                if (original != null)
                {
                    foreach (var d in original.Details.Where(x => x.DefectQty > 0))
                    {
                        d.Notes = (d.Notes ?? "") + $" [Replenished by {voucherCode}]";
                        d.DefectQty = 0; // Resolving the defect qty marks the original as "Completed" (No longer partial)
                    }
                    original.Description = (original.Description ?? "") + $" [Replenished by {voucherCode}]";
                    _db.Vouchers.Update(original);
                }
            }

            decimal totalAmount = 0;
            int lineNum = 0;
            
            // Theo dõi các mặt hàng được đưa vào vị trí trong cùng 1 phiếu để chặn lỗi "trùng ô ngay trong 1 phiếu"
            var locationsUsedInThisVoucher = new Dictionary<int, int>();
            var locationsAddedWeightThisVoucher = new Dictionary<int, decimal>();

            foreach (var line in vm.Lines.Where(l => l.ItemId > 0 && l.TransactionQty > 0))
            {
                lineNum++;
                var item = await _db.Items.FindAsync(line.ItemId);
                if (item == null) continue;

                // Calculate BaseQty based on UnitConversion (ĐV Nguồn → ĐV Đích)
                // SL nhập kho = SL Nguồn × SL Đích (VD: 10 Pair × 2 Pcs/Pair = 20 Pcs)
                decimal baseQty = line.TransactionQty;
                int destUomId = item.BaseUomId;
                if (line.DestQty > 0)
                {
                    // SL Đích là hệ số quy đổi trên mỗi đơn vị nguồn → baseQty = SL Nguồn × SL Đích
                    baseQty = line.TransactionQty * line.DestQty;
                }
                else if (line.TransactionUomId > 0 && line.TransactionUomId != item.BaseUomId)
                {
                    // Fallback: Lookup conversion rate from source UoM to item's base UoM
                    var conversionCandidates = await _db.UnitConversions
                        .Where(uc => uc.FromUomId == line.TransactionUomId
                            && uc.ToUomId == item.BaseUomId
                            && uc.IsActive
                            && (uc.ItemId == null || uc.ItemId == item.ItemId))
                        .OrderByDescending(uc => uc.ItemId == item.ItemId)
                        .ToListAsync();
                    if (conversionCandidates.Count > 1 && conversionCandidates.Count(uc => uc.ItemId == null) > 1)
                        throw new Exception($"Đang có nhiều quy đổi toàn cục cho cặp ĐVT của [{item.ItemCode}]. Vui lòng kiểm tra bảng quy đổi.");
                    var conversion = conversionCandidates.FirstOrDefault();
                    if (conversion != null)
                    {
                        baseQty = line.TransactionQty * conversion.ConversionRate;
                    }
                    else
                    {
                        // Try reverse lookup
                        var reverseCandidates = await _db.UnitConversions
                            .Where(uc => uc.FromUomId == item.BaseUomId
                                && uc.ToUomId == line.TransactionUomId
                                && uc.IsActive
                                && (uc.ItemId == null || uc.ItemId == item.ItemId))
                            .OrderByDescending(uc => uc.ItemId == item.ItemId)
                            .ToListAsync();
                        if (reverseCandidates.Count > 1 && reverseCandidates.Count(uc => uc.ItemId == null) > 1)
                            throw new Exception($"Đang có nhiều quy đổi ngược toàn cục cho cặp ĐVT của [{item.ItemCode}]. Vui lòng kiểm tra bảng quy đổi.");
                        var reverseConversion = reverseCandidates.FirstOrDefault();
                        if (reverseConversion != null && reverseConversion.ConversionRate != 0)
                        {
                            baseQty = line.TransactionQty / reverseConversion.ConversionRate;
                        }
                    }
                }

                // VoucherType 5 (Điều chỉnh): apply sign (Tăng/Giảm) without changing input rules
                if (vm.VoucherType == 5 && line.AdjustSign < 0)
                {
                    baseQty = -baseQty;
                }

                // Transfer must have both source and destination locations
                if (vm.VoucherType == 6)
                {
                    if (!line.LocationId.HasValue || line.LocationId.Value <= 0)
                        throw new Exception($"Phiếu chuyển kho thiếu vị trí nguồn cho [{item.ItemCode}].");
                    if (!line.DestLocationId.HasValue || line.DestLocationId.Value <= 0)
                        throw new Exception($"Phiếu chuyển kho thiếu vị trí đích cho [{item.ItemCode}].");
                    if (line.DestLocationId.Value == line.LocationId.Value)
                        throw new Exception($"Vị trí đích không được trùng vị trí nguồn cho [{item.ItemCode}].");

                    // Validate source/destination locations belong to selected warehouses
                    var srcWhId = await _db.Locations.AsNoTracking()
                        .Include(l => l.Zone)
                        .Where(l => l.LocationId == line.LocationId.Value && l.Zone != null)
                        .Select(l => (int?)l.Zone!.WarehouseId)
                        .FirstOrDefaultAsync();
                    if (!srcWhId.HasValue || srcWhId.Value != vm.WarehouseId)
                        throw new Exception($"Vị trí nguồn không thuộc kho nguồn cho [{item.ItemCode}].");

                    var destWhId = await _db.Locations.AsNoTracking()
                        .Include(l => l.Zone)
                        .Where(l => l.LocationId == line.DestLocationId.Value && l.Zone != null)
                        .Select(l => (int?)l.Zone!.WarehouseId)
                        .FirstOrDefaultAsync();
                    if (!destWhId.HasValue || !vm.DestWarehouseId.HasValue || destWhId.Value != vm.DestWarehouseId.Value)
                        throw new Exception($"Vị trí đích không thuộc kho đích cho [{item.ItemCode}].");
                }

                // Auto-discover the single fixed location for this item
                if (line.LocationId == null || line.LocationId == 0)
                {
                    // FEFO auto-pick for exports / returns-to-supplier / production-issue / transfer-out / adjustment-decrease
                    var needsSourceStock = vm.VoucherType is 2 or 3 or 8 or 6 || (vm.VoucherType == 5 && baseQty < 0);
                    if (needsSourceStock)
                    {
                        var required = Math.Abs(baseQty);
                        var fefoLoc = await GetFefoLocationIdAsync(item.ItemId, vm.WarehouseId, required);
                        if (fefoLoc.HasValue)
                        {
                            line.LocationId = fefoLoc.Value;
                        }
                    }

                    if ((line.LocationId == null || line.LocationId == 0) && item.DefaultLocationId.HasValue && item.DefaultLocationId > 0)
                    {
                        line.LocationId = item.DefaultLocationId;
                    }
                    else
                    {
                        var defaultLoc = await _db.ItemLocations.Where(il => il.ItemId == line.ItemId).Select(il => il.LocationId).FirstOrDefaultAsync();
                        // Keep fallback within selected warehouse
                        defaultLoc = await _db.ItemLocations
                            .Include(il => il.Location).ThenInclude(l => l!.Zone)
                            .Where(il => il.ItemId == line.ItemId
                                && il.Location != null
                                && il.Location.Zone != null
                                && il.Location.Zone.WarehouseId == vm.WarehouseId)
                            .Select(il => il.LocationId)
                            .FirstOrDefaultAsync();
                        if (defaultLoc == 0) 
                        {
                            defaultLoc = await _db.Locations
                                .Include(l => l.Zone)
                                .Where(l => l.Zone != null && l.Zone.WarehouseId == vm.WarehouseId)
                                .Select(l => l.LocationId)
                                .FirstOrDefaultAsync();
                        }
                        line.LocationId = defaultLoc > 0 ? defaultLoc : null;
                    }
                }

                if (line.LocationId.HasValue && vm.VoucherType != 6)
                {
                    var lineWhId = await GetLocationWarehouseIdAsync(line.LocationId.Value);
                    if (!lineWhId.HasValue || lineWhId.Value != vm.WarehouseId)
                        throw new Exception($"Vị trí nguồn không thuộc kho đã chọn cho [{item.ItemCode}].");
                }

                var isChemical = item.ItemType == 6;
                var unitWeight = item.Weight ?? 1m;
                var weightAdded = isChemical ? baseQty : baseQty * unitWeight;
                var maxCapacity = isChemical ? 50000m : 2000m;
                var unitName = isChemical ? "Lít" : "kg";

                if (vm.VoucherType is 1 or 4 or 7 && line.LocationId.HasValue)
                {
                    var currentStock = await _db.ItemLocations.Where(il => il.LocationId == line.LocationId.Value).SumAsync(il => il.Quantity);
                    var localAdded = locationsAddedWeightThisVoucher.ContainsKey(line.LocationId.Value) ? locationsAddedWeightThisVoucher[line.LocationId.Value] : 0;
                    
                    var totalExpectedWeight = (isChemical ? currentStock : currentStock * unitWeight) + localAdded + weightAdded;
                    if (totalExpectedWeight > maxCapacity) throw new Exception($"Ô chứa tối đa {maxCapacity:N0} {unitName}! Mã {item.ItemCode} làm ô quá tải lên thành {totalExpectedWeight:N2} {unitName}.");
                    
                    locationsAddedWeightThisVoucher[line.LocationId.Value] = localAdded + weightAdded;
                }
                else if (vm.VoucherType == 6 && line.DestLocationId.HasValue)
                {
                    var currentStock = await _db.ItemLocations.Where(il => il.LocationId == line.DestLocationId.Value).SumAsync(il => il.Quantity);
                    var localAdded = locationsAddedWeightThisVoucher.ContainsKey(line.DestLocationId.Value) ? locationsAddedWeightThisVoucher[line.DestLocationId.Value] : 0;
                    
                    var totalExpectedWeight = (isChemical ? currentStock : currentStock * unitWeight) + localAdded + weightAdded;
                    if (totalExpectedWeight > maxCapacity) throw new Exception($"Ô nhận tối đa {maxCapacity:N0} {unitName}! Mã {item.ItemCode} làm ô đích quá tải lên thành {totalExpectedWeight:N2} {unitName}.");
                    
                    locationsAddedWeightThisVoucher[line.DestLocationId.Value] = localAdded + weightAdded;
                }

                var absBaseQty = Math.Abs(baseQty);
                var lineAmount = (vm.VoucherType == 2 && vm.ExportMode == 1) ? 0 :
                    (line.LineAmount > 0 ? line.LineAmount : (line.UnitPrice * absBaseQty));
                var lineUnitPrice = absBaseQty > 0 ? lineAmount / absBaseQty : 0;

                var defectQty = (vm.VoucherType is 1 or 4 or 7) ? Math.Max(0, line.DefectQty) : 0;
                if (defectQty > absBaseQty)
                    throw new Exception($"SL lỗi/thiếu không hợp lệ cho [{item.ItemCode}]. SL lỗi/thiếu ({defectQty:N2}) không được lớn hơn SL đích ({absBaseQty:N2}).");

                // IMPORTANT: DefectBaseQty must be in base-stock units (same unit as BaseQty)
                var conversionRate = line.TransactionQty > 0 ? baseQty / line.TransactionQty : 1m;
                var defectBaseQty = defectQty * Math.Abs(conversionRate);
                if (defectBaseQty > absBaseQty)
                    throw new Exception($"SL lỗi/thiếu quy đổi không hợp lệ cho [{item.ItemCode}]. SL lỗi/thiếu quy đổi ({defectBaseQty:N2}) không được lớn hơn SL đích ({absBaseQty:N2}).");

                var detail = new VoucherDetail
                {
                    VoucherId = voucher.VoucherId,
                    ItemId = line.ItemId,
                    LocationId = line.LocationId,
                    DestLocationId = line.DestLocationId,
                    TransactionQty = line.TransactionQty,
                    TransactionUomId = line.TransactionUomId > 0 ? line.TransactionUomId : item.BaseUomId,
                    PackagingUnitId = line.PackagingUnitId,
                    ConversionRate = conversionRate,
                    BaseQty = baseQty,
                    UnitPrice = lineUnitPrice,
                    LineAmount = lineAmount,
                    QualityStatus = line.QualityStatus,
                    ExpiryDate = line.ExpiryDate,
                    LotNumber = string.IsNullOrWhiteSpace(line.LotNumber) ? null : line.LotNumber.Trim(),
                    Notes = line.Notes,
                    LineNumber = lineNum,
                    DefectQty = defectQty,
                    DefectBaseQty = defectBaseQty
                };

                _db.VoucherDetails.Add(detail);
                totalAmount += lineAmount;

            // Cập nhật tồn kho NẾU PHIẾU ĐÃ GHI SỔ
            if (voucher.IsPosted)
            {
                // Điều chỉnh (5) cũng phải áp dụng check sức chứa giống nhập kho nếu là tăng tồn
                if (vm.VoucherType == 5 && detail.BaseQty > 0 && detail.LocationId.HasValue)
                {
                    var currentStock = await _db.ItemLocations.Where(il => il.LocationId == detail.LocationId.Value).SumAsync(il => il.Quantity);
                    var localAdded = locationsAddedWeightThisVoucher.ContainsKey(detail.LocationId.Value) ? locationsAddedWeightThisVoucher[detail.LocationId.Value] : 0;

                    var isChemicalAdj = item.ItemType == 6;
                    var unitWeightAdj = item.Weight ?? 1m;
                    var weightAddedAdj = isChemicalAdj ? detail.BaseQty : detail.BaseQty * unitWeightAdj;
                    var maxCapacityAdj = isChemicalAdj ? 50000m : 2000m;
                    var unitNameAdj = isChemicalAdj ? "Lít" : "kg";

                    var totalExpectedWeight = (isChemicalAdj ? currentStock : currentStock * unitWeightAdj) + localAdded + weightAddedAdj;
                    if (totalExpectedWeight > maxCapacityAdj)
                        throw new Exception($"Điều chỉnh tăng làm quá tải: Ô chỉ chứa tối đa {maxCapacityAdj:N0} {unitNameAdj}! Mã {item.ItemCode} làm ô lên {totalExpectedWeight:N2} {unitNameAdj}.");

                    locationsAddedWeightThisVoucher[detail.LocationId.Value] = localAdded + weightAddedAdj;
                }

                if (detail.LocationId.HasValue)
                {
                    // Ràng buộc quy tắc: 1 ô chỉ chứa 1 vật tư
                    if ((vm.VoucherType is 1 or 4 or 7) || (vm.VoucherType == 5 && detail.BaseQty > 0)) // Điều chỉnh TĂNG mới cần check đè ô
                    {
                        // 1. Kiểm tra ngay trong bộ nhớ của cái phiếu hiện tại (trường hợp ngta ghi 3 dòng khác nhau nhưng nhét vào cùng 1 ô)
                        if (locationsUsedInThisVoucher.TryGetValue(detail.LocationId.Value, out int usedItemId) && usedItemId != detail.ItemId)
                        {
                            var conflictNameLocal = (await _db.Items.FindAsync(usedItemId))?.ItemCode ?? usedItemId.ToString();
                            throw new Exception($"Vi phạm quy tắc '1 ô 1 vật tư': Ngay trong cùng 1 phiếu, bạn đang cố xếp [{item.ItemCode}] và [{conflictNameLocal}] đè lên nhau tại cùng 1 vị trí!");
                        }
                        locationsUsedInThisVoucher[detail.LocationId.Value] = detail.ItemId;

                        // 2. Kiểm tra dữ liệu đã có trong kho cũ (Database)
                        var otherItemsInLocation = await _db.ItemLocations
                            .Where(il => il.LocationId == detail.LocationId.Value 
                                      && il.ItemId != detail.ItemId 
                                      && il.Quantity > 0)
                            .Include(il => il.Item)
                            .FirstOrDefaultAsync();

                        if (otherItemsInLocation != null)
                        {
                            var conflictName = otherItemsInLocation.Item != null ? otherItemsInLocation.Item.ItemCode : otherItemsInLocation.ItemId.ToString();
                            throw new Exception($"Vi phạm quy tắc '1 ô 1 vật tư': Vị trí này đang chứa mặt hàng [{conflictName}]. Bạn không được xếp [{item.ItemCode}] đè chung vào đây!");
                        }
                    }

                    var itemLocation = await _db.ItemLocations
                        .FirstOrDefaultAsync(il => il.ItemId == detail.ItemId
                            && il.LocationId == detail.LocationId.Value
                            && il.LotNumber == detail.LotNumber
                            && il.ExpiryDate == detail.ExpiryDate);
                    
                    if (itemLocation == null)
                    {
                        itemLocation = new ItemLocation
                        {
                            ItemId = detail.ItemId,
                            LocationId = detail.LocationId.Value,
                            Quantity = 0,
                            ExpiryDate = detail.ExpiryDate,
                            LotNumber = detail.LotNumber,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.ItemLocations.Add(itemLocation);
                    }

                    // Nhập kho (1), Khách trả (4), Nhập TP (7)
                    if (vm.VoucherType is 1 or 4 or 7)
                    {
                        // Posted receipts/returns add full BaseQty here (defects are handled on Approve stage for types 1/7),
                        // but for VoucherType 4 (Khách Trả) this is already posted immediately => treat as all good.
                        itemLocation.Quantity += detail.BaseQty;
                    }
                    // Xuất kho (2), Trả NCC (3), Xuất SX (8)
                    else if (vm.VoucherType is 2 or 3 or 8)
                    {
                        itemLocation.Quantity -= detail.BaseQty;
                        if (itemLocation.Quantity < 0) 
                            throw new Exception($"Lỗi: Vật tư {item.ItemCode} không đủ tồn kho tại vị trí này để xuất (chỉ còn {itemLocation.Quantity + detail.BaseQty}).");
                    }
                    // Điều chỉnh (5): BaseQty đã có dấu (+ tăng, - giảm)
                    else if (vm.VoucherType == 5)
                    {
                        itemLocation.Quantity += detail.BaseQty;
                        if (itemLocation.Quantity < 0)
                            throw new Exception($"Lỗi: Điều chỉnh giảm làm âm tồn tại vị trí. Vật tư {item.ItemCode} chỉ còn {itemLocation.Quantity - detail.BaseQty} trước điều chỉnh.");
                    }
                    // Chuyển kho (6)
                    else if (vm.VoucherType == 6)
                    {
                        itemLocation.Quantity -= detail.BaseQty;
                        if (itemLocation.Quantity < 0) 
                            throw new Exception($"Lỗi: Vật tư {item.ItemCode} không đủ tồn kho để chuyển (chỉ còn {itemLocation.Quantity + detail.BaseQty}).");

                        if (!detail.DestLocationId.HasValue)
                            throw new Exception($"Phiếu chuyển kho thiếu vị trí đích cho [{item.ItemCode}].");

                        var destItemLocation = await _db.ItemLocations
                            .FirstOrDefaultAsync(il => il.ItemId == detail.ItemId
                                && il.LocationId == detail.DestLocationId.Value
                                && il.LotNumber == detail.LotNumber
                                && il.ExpiryDate == detail.ExpiryDate);

                        if (destItemLocation == null)
                        {
                            destItemLocation = new ItemLocation
                            {
                                ItemId = detail.ItemId,
                                LocationId = detail.DestLocationId.Value,
                                Quantity = 0,
                                ExpiryDate = detail.ExpiryDate,
                                LotNumber = detail.LotNumber,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _db.ItemLocations.Add(destItemLocation);
                        }

                        destItemLocation.Quantity += detail.BaseQty;
                        destItemLocation.UpdatedAt = DateTime.UtcNow;
                    }
                    
                    
                    itemLocation.UpdatedAt = DateTime.UtcNow;
                }

                // Cập nhật tổng tồn kho của toàn bộ Item (hiển thị trên Dashboard và Báo Cáo Tồn Kho)
                if (vm.VoucherType is 1 or 4 or 7)
                {
                    item.CurrentStock += detail.BaseQty;
                }
                else if (vm.VoucherType is 2 or 3 or 8)
                {
                    item.CurrentStock -= detail.BaseQty;
                }
                else if (vm.VoucherType == 5)
                {
                    item.CurrentStock += detail.BaseQty;
                    if (item.CurrentStock < 0)
                        throw new Exception($"Lỗi: Điều chỉnh giảm làm âm tổng tồn của {item.ItemCode}.");
                }
                // Loại 6 (Chuyển kho) không làm thay đổi tổng tồn của Item
                
                item.TotalStockValue = item.CurrentStock * item.UnitCost;
                item.UpdatedAt = DateTime.UtcNow;

                // ───── Auto-create StockAlert khi tồn kho thấp ─────
                if (item.MinThreshold > 0 && item.CurrentStock <= item.MinThreshold)
                {
                    // Chỉ tạo alert mới nếu chưa có alert chưa giải quyết cho item này
                    var existingAlert = await _db.StockAlerts
                        .AnyAsync(a => a.ItemId == item.ItemId && !a.IsResolved && a.AlertType == 1);
                    if (!existingAlert)
                    {
                        _db.StockAlerts.Add(new StockAlert
                        {
                            ItemId = item.ItemId,
                            AlertType = item.CurrentStock <= 0 ? (byte)1 : (byte)1, // 1=LowStock
                            CurrentStock = item.CurrentStock,
                            Threshold = item.MinThreshold,
                            IsRead = false,
                            IsResolved = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                // Auto-resolve alert nếu tồn kho đã phục hồi trên ngưỡng
                else if (item.MinThreshold > 0 && item.CurrentStock > item.MinThreshold)
                {
                    var activeAlerts = await _db.StockAlerts
                        .Where(a => a.ItemId == item.ItemId && !a.IsResolved && a.AlertType == 1)
                        .ToListAsync();
                    foreach (var alert in activeAlerts)
                    {
                        alert.IsResolved = true;
                        alert.ResolvedAt = DateTime.UtcNow;
                    }
                }
            }
            }

            voucher.TotalAmount = totalAmount;
            voucher.TotalLines = lineNum;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = $"Tạo phiếu {voucherCode} thành công! ({lineNum} dòng, tổng {totalAmount:N0} VNĐ)";
            return RedirectToAction("Details", new { id = voucher.VoucherId });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = $"Lỗi hệ thống: {ex.Message}";
            
            // Re-populate view model for return
            vm.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
            vm.Partners = await _db.Partners.Where(p => p.IsActive).ToListAsync();
            vm.Items = await _db.Items.Include(i => i.BaseUom).Where(i => i.IsActive).OrderBy(i => i.ItemCode).ToListAsync();
            vm.Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).ToListAsync();
            vm.Locations = await _db.Locations.Where(l => l.IsActive).ToListAsync();
            
            return View(vm);
        }
    }

    public async Task<IActionResult> Details(long id)
    {
        var voucher = await _db.Vouchers
            .Include(v => v.Warehouse).Include(v => v.DestWarehouse).Include(v => v.Partner)
            .Include(v => v.Details).ThenInclude(d => d.Item)
            .Include(v => v.Details).ThenInclude(d => d.Location)
            .Include(v => v.Details).ThenInclude(d => d.TransactionUom)
            .Include(v => v.Details).ThenInclude(d => d.PackagingUnit)
            .FirstOrDefaultAsync(v => v.VoucherId == id);

        if (voucher == null) return NotFound();
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value)
            return Forbid();
        return View(voucher);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmForPicking(long id)
    {
        using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var voucher = await _db.Vouchers
                .Include(v => v.Details)
                .FirstOrDefaultAsync(v => v.VoucherId == id);
            if (voucher == null) return NotFound();
            if (voucher.IsCancelled) return BadRequest("Phiếu đã hủy.");
            if (voucher.IsPosted) return BadRequest("Phiếu đã ghi sổ.");
            if (voucher.VoucherType is not (2 or 3 or 6 or 8))
                return BadRequest("Chỉ áp dụng cho phiếu xuất/chuyển kho.");

            var scopedWh = GetScopedWarehouseId();
            if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value)
                return Forbid();

            var lockDate = await GetActiveLockDateAsync(voucher.WarehouseId);
            if (IsLocked(voucher.VoucherDate, lockDate))
                throw new Exception($"Kho đã khóa kỳ đến {lockDate:dd/MM/yyyy}. Không thể chốt xuất phiếu ngày {voucher.VoucherDate:dd/MM/yyyy}.");

            // Strict idempotency/state-machine guard: outbound voucher must be released only once.
            if (voucher.WaveId.HasValue || voucher.FulfillmentStatus >= 2)
            {
                var existingActive = await _db.StockReservations.AnyAsync(r => r.VoucherId == voucher.VoucherId && r.Status == 1);
                if (existingActive)
                {
                    voucher.FulfillmentStatus = 2;
                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                    TempData["Success"] = "Phiếu đã được giữ chỗ trước đó.";
                    return RedirectToAction("Details", new { id });
                }

                TempData["Error"] = "Phiếu đã qua bước release picking, không thể release lại. Vui lòng tạo phiếu mới hoặc hủy phiếu hiện tại.";
                await tx.CommitAsync();
                return RedirectToAction("Details", new { id });
            }

            Wave? wave = null;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var waveCode = $"WV-{DateTime.UtcNow:yyyyMMdd}-{(await _db.Waves.CountAsync()) + 1 + attempt:D5}";
                wave = new Wave
                {
                    WaveCode = waveCode,
                    WarehouseId = voucher.WarehouseId,
                    Status = 2,
                    CreatedBy = User.Identity?.Name ?? "system",
                    CreatedAt = DateTime.UtcNow,
                    ReleasedAt = DateTime.UtcNow,
                    Notes = $"Wave from voucher {voucher.VoucherCode}"
                };
                _db.Waves.Add(wave);
                try
                {
                    await _db.SaveChangesAsync();
                    break;
                }
                catch (DbUpdateException ex) when ((ex.InnerException?.Message?.Contains("2601") ?? false)
                    || (ex.InnerException?.Message?.Contains("2627") ?? false)
                    || (ex.InnerException?.Message?.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ?? false)
                    || (ex.InnerException?.Message?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    _db.Entry(wave).State = EntityState.Detached;
                    wave = null;
                    if (attempt == 9)
                        throw new Exception("Không thể tạo mã wave do trùng mã đồng thời. Vui lòng thử lại.");
                }
            }
            if (wave == null)
                throw new Exception("Không thể tạo wave. Vui lòng thử lại.");

            voucher.WaveId = wave.WaveId;
            voucher.FulfillmentStatus = 2;
            var affectedItemLocationIds = new List<int>();
            var taskSeq = 0;
            foreach (var d in voucher.Details)
            {
                var req = Math.Abs(d.BaseQty);
                if (req <= 0) continue;

                List<FefoAllocation> allocations;
                if (d.LocationId.HasValue)
                {
                    var sourceLoc = await _db.ItemLocations
                        .FirstOrDefaultAsync(il => il.ItemId == d.ItemId
                            && il.LocationId == d.LocationId.Value
                            && (d.LotNumber == null || il.LotNumber == d.LotNumber)
                            && (!d.ExpiryDate.HasValue || il.ExpiryDate == d.ExpiryDate));
                    if (sourceLoc == null)
                        throw new Exception($"Không tìm thấy tồn nguồn cho [{d.ItemId}] tại vị trí đã chọn.");
                    var available = sourceLoc.Quantity - sourceLoc.ReservedQty;
                    if (available < req)
                        throw new Exception($"Không đủ tồn khả dụng tại vị trí chỉ định. Cần {req:N2}, khả dụng {available:N2}.");
                    allocations = new List<FefoAllocation> { new(sourceLoc.LocationId, sourceLoc.LotNumber, sourceLoc.ExpiryDate, req) };
                }
                else
                {
                    allocations = await AllocateFefoAsync(d.ItemId, voucher.WarehouseId, req);
                }

                _db.WaveLines.Add(new WaveLine
                {
                    WaveId = wave.WaveId,
                    VoucherId = voucher.VoucherId,
                    ItemId = d.ItemId,
                    RequiredQty = req,
                    PickedQty = 0,
                    Status = 1
                });

                foreach (var a in allocations)
                {
                    var reservation = new StockReservation
                    {
                        VoucherId = voucher.VoucherId,
                        VoucherDetailId = d.VoucherDetailId,
                        ItemId = d.ItemId,
                        LocationId = a.LocationId,
                        LotNumber = a.LotNumber,
                        ExpiryDate = a.ExpiryDate,
                        ReservedQty = a.Qty,
                        Status = 1,
                        CreatedBy = User.Identity?.Name ?? "system",
                        CreatedAt = DateTime.UtcNow,
                        Notes = $"Reserved for {voucher.VoucherCode}"
                    };
                    _db.StockReservations.Add(reservation);

                    taskSeq++;
                    _db.PickTasks.Add(new PickTask
                    {
                        TaskCode = $"PT-{wave.WaveCode}-{taskSeq:D3}",
                        WaveId = wave.WaveId,
                        VoucherId = voucher.VoucherId,
                        VoucherDetailId = d.VoucherDetailId,
                        ItemId = d.ItemId,
                        SourceLocationId = a.LocationId,
                        LotNumber = a.LotNumber,
                        ExpiryDate = a.ExpiryDate,
                        TargetQty = a.Qty,
                        Status = 1
                    });

                    var itemLoc = await _db.ItemLocations
                        .FirstOrDefaultAsync(il => il.ItemId == d.ItemId && il.LocationId == a.LocationId && il.LotNumber == a.LotNumber && il.ExpiryDate == a.ExpiryDate);
                    if (itemLoc != null)
                        affectedItemLocationIds.Add(itemLoc.ItemLocationId);
                }
            }

            await _db.SaveChangesAsync();
            await RecalculateReservedQtyAsync(affectedItemLocationIds);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["Success"] = $"Đã release wave {wave.WaveCode} và giữ chỗ tồn khả dụng.";
            return RedirectToAction("Details", new { id });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            TempData["Error"] = $"Không thể release picking: {ex.Message}";
            return RedirectToAction("Details", new { id });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPickTask(long id, decimal qty, string? scanValue)
    {
        if (qty <= 0) return BadRequest("SL pick phải > 0.");
        if (string.IsNullOrWhiteSpace(scanValue))
            return BadRequest("Vui lòng quét/nhập mã vạch hoặc số lô trước khi xác nhận lấy.");

        long? redirectVoucherId = null;
        using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var task = await _db.PickTasks.FirstOrDefaultAsync(t => t.PickTaskId == id);
            if (task == null) return NotFound();
            redirectVoucherId = task.VoucherId;
            var scanned = scanValue.Trim();

            var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.VoucherId == task.VoucherId);
            if (voucher == null) return NotFound();
            if (voucher.IsCancelled) return BadRequest("Phiếu đã hủy, không thể xác nhận pick.");
            if (voucher.IsPosted) return BadRequest("Phiếu đã ghi sổ, không thể xác nhận pick.");
            var scopedWh = GetScopedWarehouseId();
            if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value)
                return Forbid();

            var actor = User.Identity?.Name ?? "system";
            var canOverrideTaskAssignee = User.IsInRole("Admin") || User.IsInRole("Manager");
            if (!string.IsNullOrWhiteSpace(task.AssignedTo)
                && !string.Equals(task.AssignedTo, actor, StringComparison.OrdinalIgnoreCase)
                && !canOverrideTaskAssignee)
            {
                return BadRequest($"Task này đang được giao cho '{task.AssignedTo}'. Bạn không thể xác nhận thay.");
            }

            var taskItem = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ItemId == task.ItemId);
            if (taskItem == null)
                return BadRequest("Không tìm thấy vật tư của nhiệm vụ lấy hàng.");

            var allowedScanKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                taskItem.ItemCode.Trim()
            };
            if (!string.IsNullOrWhiteSpace(taskItem.Barcode))
                allowedScanKeys.Add(taskItem.Barcode.Trim());
            if (!string.IsNullOrWhiteSpace(taskItem.SkuCode))
                allowedScanKeys.Add(taskItem.SkuCode.Trim());
            if (!string.IsNullOrWhiteSpace(task.LotNumber))
                allowedScanKeys.Add(task.LotNumber.Trim());

            if (!allowedScanKeys.Contains(scanned))
            {
                return BadRequest(
                    $"Mã quét không khớp vật tư nhiệm vụ [{taskItem.ItemCode}]"
                    + (string.IsNullOrWhiteSpace(task.LotNumber) ? "." : $" hoặc số lô [{task.LotNumber}]."));
            }

            var remain = task.TargetQty - task.PickedQty;
            var actual = Math.Min(remain, qty);
            if (actual <= 0)
            {
                TempData["Info"] = "Task đã đủ số lượng pick.";
                await tx.CommitAsync();
                return RedirectToAction("Details", new { id = task.VoucherId });
            }

            task.PickedQty += actual;
            task.Status = task.PickedQty >= task.TargetQty ? (byte)4 : (byte)3;
            if (task.Status == 4) task.CompletedAt = DateTime.UtcNow;
            if (task.Status is 3 or 4 && string.IsNullOrWhiteSpace(task.AssignedTo))
            {
                task.AssignedTo = actor;
                task.AssignedAt = DateTime.UtcNow;
            }

            _db.PickTaskScanLogs.Add(new PickTaskScanLog
            {
                PickTaskId = task.PickTaskId,
                ScannedBy = actor,
                ScanValue = scanned,
                Qty = actual,
                Notes = "Pick confirm"
            });

            await _db.SaveChangesAsync();

            var openTasks = await _db.PickTasks.CountAsync(t => t.WaveId == task.WaveId && (t.Status == 1 || t.Status == 2 || t.Status == 3));
            if (openTasks == 0)
            {
                var wave = await _db.Waves.FirstOrDefaultAsync(w => w.WaveId == task.WaveId);
                if (wave != null)
                {
                    wave.Status = 4;
                    wave.CompletedAt = DateTime.UtcNow;
                }
                var waveVouchers = await _db.Vouchers.Where(v => v.WaveId == task.WaveId && !v.IsCancelled).ToListAsync();
                foreach (var v in waveVouchers) v.FulfillmentStatus = 4;
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            TempData["Success"] = "Đã ghi nhận scan pick task.";
            return RedirectToAction("Details", new { id = task.VoucherId });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            TempData["Error"] = $"Scan pick task thất bại: {ex.Message}";
            if (redirectVoucherId.HasValue)
                return RedirectToAction("Details", new { id = redirectVoucherId.Value });
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostReservedOutbound(long id, bool cancelRemaining = false)
    {
        using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var voucher = await _db.Vouchers
                .Include(v => v.Details).ThenInclude(d => d.Item)
                .FirstOrDefaultAsync(v => v.VoucherId == id);
            if (voucher == null) return NotFound();
            if (voucher.IsCancelled) return BadRequest("Phiếu đã hủy.");
            if (voucher.IsPosted) return BadRequest("Phiếu đã ghi sổ.");
            if (voucher.VoucherType is not (2 or 3 or 6 or 8))
                return BadRequest("Chỉ áp dụng cho phiếu outbound.");

            var scopedWh = GetScopedWarehouseId();
            if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value)
                return Forbid();

            var lockDate = await GetActiveLockDateAsync(voucher.WarehouseId);
            if (IsLocked(voucher.VoucherDate, lockDate))
                throw new Exception($"Kho đã khóa kỳ đến {lockDate:dd/MM/yyyy}. Không thể chốt xuất phiếu ngày {voucher.VoucherDate:dd/MM/yyyy}.");

            var allReservations = await _db.StockReservations
                .Where(r => r.VoucherId == voucher.VoucherId)
                .ToListAsync();
            var reservations = allReservations
                .Where(r => r.Status == 1)
                .ToList();
            if (!allReservations.Any())
                throw new Exception("Phiếu chưa có reservation. Vui lòng Confirm for Picking trước.");

            // Idempotency guard: reservation đã xử lý hết nhưng trạng thái phiếu chưa cập nhật.
            var hasOpenBeforePost = allReservations.Any(r => (r.ReservedQty - r.ConsumedQty - r.ReleasedQty) > 0);
            if (!reservations.Any() || !hasOpenBeforePost)
            {
                voucher.IsPosted = true;
                voucher.FulfillmentStatus = 5;
                voucher.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                TempData["Success"] = $"Phiếu outbound {voucher.VoucherCode} đã hoàn tất trước đó.";
                return RedirectToAction("Details", new { id });
            }
            var pickedByTaskKey = await _db.PickTasks
                .Where(t => t.VoucherId == voucher.VoucherId)
                .GroupBy(t => new { t.VoucherDetailId, t.ItemId, t.SourceLocationId, t.LotNumber, t.ExpiryDate })
                .Select(g => new
                {
                    g.Key.VoucherDetailId,
                    g.Key.ItemId,
                    LocationId = g.Key.SourceLocationId,
                    g.Key.LotNumber,
                    g.Key.ExpiryDate,
                    PickedQty = g.Sum(x => x.PickedQty)
                })
                .ToListAsync();

            var affectedItemLocationIds = new List<int>();
            var itemIds = reservations.Select(r => r.ItemId).Distinct().ToList();
            var items = await _db.Items.Where(i => itemIds.Contains(i.ItemId)).ToDictionaryAsync(i => i.ItemId, i => i);

            var hasPostedAnyQty = false;

            foreach (var r in reservations)
            {
                var pending = r.ReservedQty - r.ConsumedQty - r.ReleasedQty;
                if (pending <= 0) continue;
                var pickedQty = pickedByTaskKey
                    .Where(x => x.VoucherDetailId == r.VoucherDetailId
                        && x.ItemId == r.ItemId
                        && x.LocationId == r.LocationId
                        && x.LotNumber == r.LotNumber
                        && x.ExpiryDate == r.ExpiryDate)
                    .Select(x => x.PickedQty)
                    .FirstOrDefault();
                var consumedBefore = allReservations
                    .Where(x => x.StockReservationId != r.StockReservationId
                        && x.VoucherDetailId == r.VoucherDetailId
                        && x.ItemId == r.ItemId
                        && x.LocationId == r.LocationId
                        && x.LotNumber == r.LotNumber
                        && x.ExpiryDate == r.ExpiryDate)
                    .Sum(x => x.ConsumedQty);
                var postableFromPicked = Math.Max(0, pickedQty - consumedBefore - r.ConsumedQty);
                var postQty = Math.Min(pending, postableFromPicked);

                if (postQty <= 0)
                {
                    if (cancelRemaining)
                    {
                        r.ReleasedQty += pending;
                        r.Status = 3;
                        r.UpdatedAt = DateTime.UtcNow;
                        var releasedLoc = await _db.ItemLocations
                            .FirstOrDefaultAsync(il => il.ItemId == r.ItemId
                                && il.LocationId == r.LocationId
                                && il.LotNumber == r.LotNumber
                                && il.ExpiryDate == r.ExpiryDate);
                        if (releasedLoc != null) affectedItemLocationIds.Add(releasedLoc.ItemLocationId);
                    }
                    else
                    {
                        // Keep reservation active for next picking round.
                    }
                    continue;
                }

                var itemLoc = await _db.ItemLocations
                    .FirstOrDefaultAsync(il => il.ItemId == r.ItemId
                        && il.LocationId == r.LocationId
                        && il.LotNumber == r.LotNumber
                        && il.ExpiryDate == r.ExpiryDate);
                if (itemLoc == null) throw new Exception("Không tìm thấy tồn nguồn để post.");
                if (itemLoc.Quantity < postQty) throw new Exception("Tồn thực tế đã thay đổi, không đủ để post.");

                itemLoc.Quantity -= postQty;
                itemLoc.UpdatedAt = DateTime.UtcNow;
                affectedItemLocationIds.Add(itemLoc.ItemLocationId);
                hasPostedAnyQty = true;

                if (voucher.VoucherType == 6)
                {
                    var targetDetail = voucher.Details.FirstOrDefault(d => d.VoucherDetailId == r.VoucherDetailId);
                    if (targetDetail?.DestLocationId == null)
                        throw new Exception("Phiếu chuyển kho thiếu vị trí đích.");

                    var dest = await _db.ItemLocations
                        .FirstOrDefaultAsync(il => il.ItemId == r.ItemId
                            && il.LocationId == targetDetail.DestLocationId.Value
                            && il.LotNumber == r.LotNumber
                            && il.ExpiryDate == r.ExpiryDate);
                    if (dest == null)
                    {
                        dest = new ItemLocation
                        {
                            ItemId = r.ItemId,
                            LocationId = targetDetail.DestLocationId.Value,
                            Quantity = 0,
                            LotNumber = r.LotNumber,
                            ExpiryDate = r.ExpiryDate,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.ItemLocations.Add(dest);
                    }
                    dest.Quantity += postQty;
                    dest.UpdatedAt = DateTime.UtcNow;
                }
                else if (items.TryGetValue(r.ItemId, out var item))
                {
                    item.CurrentStock -= postQty;
                    if (item.CurrentStock < 0) throw new Exception($"Post làm âm tổng tồn của {item.ItemCode}.");
                    item.TotalStockValue = item.CurrentStock * item.UnitCost;
                    item.UpdatedAt = DateTime.UtcNow;
                }

                r.ConsumedQty += postQty;
                var pendingAfter = r.ReservedQty - r.ConsumedQty - r.ReleasedQty;
                if (pendingAfter <= 0)
                {
                    r.Status = 2;
                }
                else if (cancelRemaining)
                {
                    r.ReleasedQty += pendingAfter;
                    r.Status = 2;
                    var releasedLoc = await _db.ItemLocations
                        .FirstOrDefaultAsync(il => il.ItemId == r.ItemId
                            && il.LocationId == r.LocationId
                            && il.LotNumber == r.LotNumber
                            && il.ExpiryDate == r.ExpiryDate);
                    if (releasedLoc != null) affectedItemLocationIds.Add(releasedLoc.ItemLocationId);
                }
                else
                {
                    r.Status = 1;
                }
                r.UpdatedAt = DateTime.UtcNow;
            }

            if (!hasPostedAnyQty && !cancelRemaining)
                throw new Exception("Chưa có số lượng nào được pick để ghi sổ. Có thể dùng tùy chọn hủy phần còn lại.");

            await RecalculateReservedQtyAsync(affectedItemLocationIds);
            var activeRemaining = reservations.Any(r => r.Status == 1 && (r.ReservedQty - r.ConsumedQty - r.ReleasedQty) > 0);

            voucher.IsPosted = !activeRemaining;
            voucher.FulfillmentStatus = activeRemaining ? (byte)6 : (byte)5;
            voucher.UpdatedAt = DateTime.UtcNow;

            if (voucher.WaveId.HasValue)
            {
                var relatedTasks = await _db.PickTasks
                    .Where(t => t.VoucherId == voucher.VoucherId && (t.Status == 1 || t.Status == 2 || t.Status == 3))
                    .ToListAsync();

                foreach (var task in relatedTasks)
                {
                    if (cancelRemaining && task.PickedQty < task.TargetQty)
                    {
                        task.Status = 6;
                        task.CompletedAt ??= DateTime.UtcNow;
                    }
                    else if (task.PickedQty >= task.TargetQty)
                    {
                        task.Status = 4;
                        task.CompletedAt ??= DateTime.UtcNow;
                    }
                    else if (!activeRemaining)
                    {
                        task.Status = 5;
                        task.CompletedAt ??= DateTime.UtcNow;
                    }
                }

                var openTasks = await _db.PickTasks
                    .CountAsync(t => t.WaveId == voucher.WaveId.Value && (t.Status == 1 || t.Status == 2 || t.Status == 3));
                if (openTasks == 0)
                {
                    var wave = await _db.Waves.FirstOrDefaultAsync(w => w.WaveId == voucher.WaveId.Value);
                    if (wave != null)
                    {
                        wave.Status = 4;
                        wave.CompletedAt ??= DateTime.UtcNow;
                    }
                }
            }
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            TempData["Success"] = activeRemaining
                ? $"Đã ghi sổ xuất một phần cho phiếu {voucher.VoucherCode}. Phần còn lại vẫn chờ xử lý."
                : $"Đã ghi sổ hoàn tất phiếu outbound {voucher.VoucherCode}.";
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            TempData["Error"] = $"Post outbound thất bại: {ex.Message}";
        }

        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(long id, byte reviewResult = 2, decimal responsibilityScore = 0, string? reviewNote = null)
    {
        var scopedWh = GetScopedWarehouseId();
        using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var reviewer = User.Identity?.Name ?? "system";
            var voucher = await _db.Vouchers
                .Include(v => v.Details).ThenInclude(d => d.Item)
                .FirstOrDefaultAsync(v => v.VoucherId == id);
            if (voucher == null) return NotFound();
            if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value)
                return Forbid();
            if (voucher.IsCancelled)
            {
                TempData["Error"] = "Phiếu đã bị hủy, không thể duyệt.";
                return RedirectToAction("Details", new { id });
            }
            if (voucher.IsPosted)
            {
                TempData["Error"] = "Phiếu này đã được duyệt và ghi nhận tồn kho!";
                return RedirectToAction("Details", new { id });
            }
            if (voucher.VoucherType is 2 or 3 or 6 or 8)
            {
                TempData["Error"] = "Phiếu outbound dùng luồng Confirm Picking + Post Outbound, không duyệt theo luồng nhập kho.";
                return RedirectToAction("Details", new { id });
            }
            if (string.Equals(voucher.CreatedBy, reviewer, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Người kiểm không được trùng người nhập phiếu. Vui lòng chuyển người kiểm khác.";
                return RedirectToAction("Details", new { id });
            }

            if (reviewResult is not (2 or 3))
                reviewResult = 2;
            if (responsibilityScore < 0 || responsibilityScore > 100)
                throw new Exception("Điểm trách nhiệm phải nằm trong khoảng 0-100.");
            if (reviewResult == 3)
            {
                if (string.IsNullOrWhiteSpace(reviewNote))
                    throw new Exception("Khi có sai lệch, phải nhập ghi chú kiểm và lý do chỉnh.");
                if (responsibilityScore <= 0)
                    throw new Exception("Khi có sai lệch, điểm trách nhiệm phải lớn hơn 0.");
            }
            else
            {
                responsibilityScore = 0;
            }

            var lockDate = await GetActiveLockDateAsync(voucher.WarehouseId);
            if (IsLocked(voucher.VoucherDate, lockDate))
            {
                TempData["Error"] = $"Kho đã khóa kỳ đến {lockDate:dd/MM/yyyy}. Không thể duyệt phiếu ngày {voucher.VoucherDate:dd/MM/yyyy}.";
                return RedirectToAction("Details", new { id });
            }

            var locationsUsedInThisVoucher = new Dictionary<int, int>();
            var locationsAddedWeightThisVoucher = new Dictionary<int, decimal>();

            foreach (var detail in voucher.Details)
            {
                var item = detail.Item;
                if (item == null) continue;

                var defectBase = detail.DefectBaseQty > 0 ? detail.DefectBaseQty : (detail.DefectQty * (detail.ConversionRate == 0 ? 1 : Math.Abs(detail.ConversionRate)));
                defectBase = Math.Max(0, defectBase);
                if (defectBase > detail.BaseQty)
                    throw new Exception($"Dòng vật tư [{item.ItemCode}] có SL lỗi/thiếu ({defectBase:N2}) lớn hơn SL đích ({detail.BaseQty:N2}). Vui lòng chỉnh lại trước khi duyệt.");

                var goodBaseQty = detail.BaseQty - defectBase;
                if (goodBaseQty < 0) goodBaseQty = 0;

                var isChemical = item.ItemType == 6;
                var unitWeight = item.Weight ?? 1m;
                var weightAdded = isChemical ? goodBaseQty : goodBaseQty * unitWeight;
                var maxCapacity = isChemical ? 50000m : 2000m;
                var unitName = isChemical ? "Lít" : "kg";

                if (detail.LocationId.HasValue && (voucher.VoucherType is 1 or 4 or 7 || (voucher.VoucherType == 5 && detail.BaseQty > 0)))
                {
                    var currentStock = await _db.ItemLocations.Where(il => il.LocationId == detail.LocationId.Value).SumAsync(il => il.Quantity);
                    var localAdded = locationsAddedWeightThisVoucher.ContainsKey(detail.LocationId.Value) ? locationsAddedWeightThisVoucher[detail.LocationId.Value] : 0;

                    var addedForCapacity = voucher.VoucherType == 5 ? detail.BaseQty : goodBaseQty;
                    var weightForCapacity = isChemical ? addedForCapacity : addedForCapacity * unitWeight;
                    var totalExpectedWeight = (isChemical ? currentStock : currentStock * unitWeight) + localAdded + weightForCapacity;
                    if (totalExpectedWeight > maxCapacity) throw new Exception($"Quá tải khi duyệt: Ô chỉ chứa tối đa {maxCapacity:N0} {unitName}! Nạp thêm mã {item.ItemCode} làm ô lên {totalExpectedWeight:N2} {unitName}.");

                    locationsAddedWeightThisVoucher[detail.LocationId.Value] = localAdded + weightForCapacity;
                }

                    if (detail.LocationId.HasValue)
                {
                    // Ràng buộc quy tắc: 1 ô chỉ chứa 1 vật tư
                    if (voucher.VoucherType is 1 or 4 or 7)
                    {
                        if (locationsUsedInThisVoucher.TryGetValue(detail.LocationId.Value, out int usedItemId) && usedItemId != detail.ItemId)
                        {
                            var conflictNameLocal = (await _db.Items.FindAsync(usedItemId))?.ItemCode ?? usedItemId.ToString();
                            throw new Exception($"Vi phạm quy tắc '1 ô 1 vật tư': Ngay trong cùng 1 phiếu, bạn đang cố xếp [{item.ItemCode}] và [{conflictNameLocal}] đè lên nhau tại cùng 1 vị trí!");
                        }
                        locationsUsedInThisVoucher[detail.LocationId.Value] = detail.ItemId;

                        var otherItemsInLocation = await _db.ItemLocations
                            .Where(il => il.LocationId == detail.LocationId.Value 
                                      && il.ItemId != detail.ItemId 
                                      && il.Quantity > 0)
                            .Include(il => il.Item)
                            .FirstOrDefaultAsync();

                        if (otherItemsInLocation != null)
                        {
                            var conflictName = otherItemsInLocation.Item != null ? otherItemsInLocation.Item.ItemCode : otherItemsInLocation.ItemId.ToString();
                            throw new Exception($"Vi phạm quy tắc '1 ô 1 vật tư': Vị trí này đang chứa mặt hàng [{conflictName}]. Bạn không được xếp [{item.ItemCode}] đè chung vào đây!");
                        }
                    }

                    var itemLocation = await _db.ItemLocations
                        .FirstOrDefaultAsync(il => il.ItemId == detail.ItemId
                            && il.LocationId == detail.LocationId.Value
                            && il.LotNumber == detail.LotNumber
                            && il.ExpiryDate == detail.ExpiryDate);
                    
                    if (itemLocation == null)
                    {
                        itemLocation = new ItemLocation
                        {
                            ItemId = detail.ItemId,
                            LocationId = detail.LocationId.Value,
                            Quantity = 0,
                            ExpiryDate = detail.ExpiryDate,
                            LotNumber = detail.LotNumber,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.ItemLocations.Add(itemLocation);
                    }

                    if (voucher.VoucherType is 1 or 4 or 7)
                    {
                        itemLocation.Quantity += goodBaseQty;
                    }
                    else if (voucher.VoucherType == 5)
                    {
                        itemLocation.Quantity += detail.BaseQty;
                        if (itemLocation.Quantity < 0)
                            throw new Exception($"Điều chỉnh giảm làm âm tồn tại vị trí. Vật tư {item.ItemCode} chỉ còn {itemLocation.Quantity - detail.BaseQty} trước điều chỉnh.");
                    }
                    
                    itemLocation.UpdatedAt = DateTime.UtcNow;
                }

                if (voucher.VoucherType is 1 or 4 or 7)
                {
                    item.CurrentStock += goodBaseQty;
                }
                else if (voucher.VoucherType == 5)
                {
                    item.CurrentStock += detail.BaseQty;
                    if (item.CurrentStock < 0)
                        throw new Exception($"Điều chỉnh giảm làm âm tổng tồn của {item.ItemCode}.");
                }
                
                item.TotalStockValue = item.CurrentStock * item.UnitCost;
                item.UpdatedAt = DateTime.UtcNow;

                // Auto-resolve StockAlert nếu tồn kho phục hồi trên ngưỡng sau duyệt
                if (item.MinThreshold > 0 && item.CurrentStock > item.MinThreshold)
                {
                    var activeAlerts = await _db.StockAlerts
                        .Where(a => a.ItemId == item.ItemId && !a.IsResolved && a.AlertType == 1)
                        .ToListAsync();
                    foreach (var alert in activeAlerts)
                    {
                        alert.IsResolved = true;
                        alert.ResolvedAt = DateTime.UtcNow;
                    }
                }
            }

            voucher.IsPosted = true;
            voucher.ReviewResult = reviewResult;
            voucher.ReviewedBy = reviewer;
            voucher.ReviewedAt = DateTime.UtcNow;
            voucher.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();
            voucher.ResponsibilityScore = responsibilityScore;
            voucher.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = reviewResult == 3
                ? $"Đã kiểm và duyệt phiếu {voucher.VoucherCode}. Hệ thống ghi nhận sai lệch (điểm trách nhiệm: {responsibilityScore:N0})."
                : $"Đã kiểm và duyệt phiếu {voucher.VoucherCode}, tồn kho đã cập nhật thành công.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = $"Lỗi duyệt phiếu: {ex.Message}";
        }

        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateInboundDefect(long id, long detailId, decimal defectQty, string? reviewNote)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var scopedWh = GetScopedWarehouseId();
            var voucher = await _db.Vouchers
                .Include(v => v.Details)
                .FirstOrDefaultAsync(v => v.VoucherId == id);
            if (voucher == null) return NotFound();
            if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value) return Forbid();
            if (voucher.IsCancelled || voucher.IsPosted)
                throw new Exception("Phiếu đã hủy hoặc đã duyệt, không thể chỉnh.");
            if (voucher.VoucherType is not (1 or 4 or 7))
                throw new Exception("Chỉ cho chỉnh sai lệch với phiếu nhập.");
            var reviewer = User.Identity?.Name ?? "system";
            if (string.Equals(voucher.CreatedBy, reviewer, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Người kiểm không được trùng người nhập khi chỉnh sai lệch.");

            var detail = voucher.Details.FirstOrDefault(d => d.VoucherDetailId == detailId);
            if (detail == null) throw new Exception("Không tìm thấy dòng chi tiết cần chỉnh.");

            var rate = detail.ConversionRate == 0 ? 1 : Math.Abs(detail.ConversionRate);
            var maxDefectQty = rate > 0 ? (detail.BaseQty / rate) : detail.BaseQty;
            if (defectQty < 0 || defectQty > maxDefectQty)
                throw new Exception($"SL lỗi phải nằm trong khoảng 0 đến {maxDefectQty:N4}.");

            detail.DefectQty = defectQty;
            detail.DefectBaseQty = defectQty * rate;
            if (!string.IsNullOrWhiteSpace(reviewNote))
                detail.Notes = $"[KIEM:{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {reviewNote.Trim()}";

            voucher.ReviewResult = 3;
            voucher.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? voucher.ReviewNote : reviewNote.Trim();
            voucher.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Success"] = "Đã cập nhật sai lệch kiểm hàng cho dòng vật tư.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = $"Không thể cập nhật sai lệch: {ex.Message}";
        }

        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(long id, string cancelReason)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var user = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            
            var voucher = await _db.Vouchers
                .Include(v => v.Details).ThenInclude(d => d.Item)
                .FirstOrDefaultAsync(v => v.VoucherId == id);

            if (voucher == null) throw new Exception("Không tìm thấy phiếu.");
            if (voucher.IsCancelled) throw new Exception("Phiếu đã được hủy.");

            var scopedWh = GetScopedWarehouseId();
            if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value)
                return Forbid();

            var lockDate = await GetActiveLockDateAsync(voucher.WarehouseId);
            if (IsLocked(voucher.VoucherDate, lockDate))
                throw new Exception($"Kho đã khóa kỳ đến {lockDate:dd/MM/yyyy}. Không thể hủy phiếu ngày {voucher.VoucherDate:dd/MM/yyyy}.");

            var isOutboundVoucher = voucher.VoucherType is 2 or 3 or 6 or 8;
            var hasConsumedReservations = isOutboundVoucher && await _db.StockReservations
                .AnyAsync(r => r.VoucherId == voucher.VoucherId && r.ConsumedQty > 0);

            if (voucher.IsPosted || hasConsumedReservations)
            {
                // Outbound/transfer posted by reservation flow must rollback from consumed reservations
                if (isOutboundVoucher)
                {
                    var consumedReservations = await _db.StockReservations
                        .Where(r => r.VoucherId == voucher.VoucherId && (r.Status == 2 || r.ConsumedQty > 0))
                        .ToListAsync();

                    var itemIds = consumedReservations.Select(r => r.ItemId).Distinct().ToList();
                    var itemsById = await _db.Items.Where(i => itemIds.Contains(i.ItemId)).ToDictionaryAsync(i => i.ItemId, i => i);
                    var affectedItemLocationIds = new List<int>();

                    foreach (var r in consumedReservations)
                    {
                        var consumed = Math.Max(0, r.ConsumedQty);
                        if (consumed == 0) continue;

                        var src = await _db.ItemLocations.FirstOrDefaultAsync(il =>
                            il.ItemId == r.ItemId
                            && il.LocationId == r.LocationId
                            && il.LotNumber == r.LotNumber
                            && il.ExpiryDate == r.ExpiryDate);
                        if (src == null)
                            throw new Exception($"Không tìm thấy tồn nguồn để hoàn tác cho item {r.ItemId}.");
                        src.Quantity += consumed;
                        src.UpdatedAt = DateTime.UtcNow;
                        affectedItemLocationIds.Add(src.ItemLocationId);

                        if (voucher.VoucherType == 6)
                        {
                            var detail = voucher.Details.FirstOrDefault(d => d.VoucherDetailId == r.VoucherDetailId);
                            if (detail?.DestLocationId == null)
                                throw new Exception("Không xác định được vị trí đích khi hoàn tác chuyển kho.");

                            var dest = await _db.ItemLocations.FirstOrDefaultAsync(il =>
                                il.ItemId == r.ItemId
                                && il.LocationId == detail.DestLocationId.Value
                                && il.LotNumber == r.LotNumber
                                && il.ExpiryDate == r.ExpiryDate);
                            if (dest == null)
                                throw new Exception($"Không tìm thấy tồn vị trí đích để hoàn tác chuyển kho cho item {r.ItemId}.");
                            dest.Quantity -= consumed;
                            if (dest.Quantity < 0)
                                throw new Exception($"Hoàn tác chuyển kho làm âm tồn vị trí đích cho item {r.ItemId}.");
                            dest.UpdatedAt = DateTime.UtcNow;
                            affectedItemLocationIds.Add(dest.ItemLocationId);
                        }
                        else if (itemsById.TryGetValue(r.ItemId, out var itemOut))
                        {
                            itemOut.CurrentStock += consumed;
                            itemOut.TotalStockValue = itemOut.CurrentStock * itemOut.UnitCost;
                            itemOut.UpdatedAt = DateTime.UtcNow;
                        }

                        r.ReleasedQty += consumed;
                        r.ConsumedQty = 0;
                        r.Status = 3;
                        r.UpdatedAt = DateTime.UtcNow;
                    }

                    var activeReservations = await _db.StockReservations
                        .Where(r => r.VoucherId == voucher.VoucherId && r.Status == 1)
                        .ToListAsync();
                    foreach (var r in activeReservations)
                    {
                        r.ReleasedQty = r.ReservedQty - r.ConsumedQty;
                        r.Status = 3;
                        r.UpdatedAt = DateTime.UtcNow;

                        var il = await _db.ItemLocations.FirstOrDefaultAsync(x =>
                            x.ItemId == r.ItemId
                            && x.LocationId == r.LocationId
                            && x.LotNumber == r.LotNumber
                            && x.ExpiryDate == r.ExpiryDate);
                        if (il != null) affectedItemLocationIds.Add(il.ItemLocationId);
                    }
                    await RecalculateReservedQtyAsync(affectedItemLocationIds);
                }
                else
                {
                    foreach (var detail in voucher.Details)
                    {
                        var item = detail.Item;
                        if (item == null) continue;

                        // Calculate the exact base quantity that was applied to stock (esp. receipts with defects)
                        decimal appliedBaseQty = detail.BaseQty;
                        if (voucher.VoucherType is 1 or 7)
                        {
                            var defectBase = detail.DefectBaseQty > 0 ? detail.DefectBaseQty : (detail.DefectQty * (detail.ConversionRate == 0 ? 1 : Math.Abs(detail.ConversionRate)));
                            defectBase = Math.Max(0, defectBase);
                            appliedBaseQty = Math.Max(0, detail.BaseQty - defectBase);
                        }

                        if (detail.LocationId.HasValue)
                        {
                            var itemLoc = await _db.ItemLocations.FirstOrDefaultAsync(il =>
                                il.ItemId == detail.ItemId
                                && il.LocationId == detail.LocationId.Value
                                && il.LotNumber == detail.LotNumber
                                && il.ExpiryDate == detail.ExpiryDate);
                            if (itemLoc == null)
                                throw new Exception($"Không tìm thấy tồn vị trí/lô để hoàn tác cho {item.ItemCode}. Vui lòng kiểm tra dữ liệu tồn kho.");

                            if (voucher.VoucherType is 1 or 4 or 7)
                            {
                                itemLoc.Quantity -= appliedBaseQty;
                                if (itemLoc.Quantity < 0)
                                    throw new Exception($"Hủy phiếu làm âm tồn vị trí cho {item.ItemCode}. Vui lòng hủy các phiếu xuất/chuyển liên quan trước.");
                            }
                            else if (voucher.VoucherType == 5)
                            {
                                itemLoc.Quantity -= detail.BaseQty;
                                if (itemLoc.Quantity < 0)
                                    throw new Exception($"Hủy điều chỉnh làm âm tồn tại vị trí. Vật tư {item.ItemCode} chỉ còn {itemLoc.Quantity + detail.BaseQty} trước khi hủy.");
                            }
                            itemLoc.UpdatedAt = DateTime.UtcNow;
                        }

                        if (voucher.VoucherType is 1 or 4 or 7)
                        {
                            item.CurrentStock -= appliedBaseQty;
                            if (item.CurrentStock < 0)
                                throw new Exception($"Hủy phiếu làm âm tổng tồn cho {item.ItemCode}. Vui lòng kiểm tra nghiệp vụ phát sinh sau phiếu này.");
                        }
                        else if (voucher.VoucherType == 5)
                        {
                            item.CurrentStock -= detail.BaseQty;
                            if (item.CurrentStock < 0)
                                throw new Exception($"Hủy điều chỉnh làm âm tổng tồn của {item.ItemCode}.");
                        }

                        item.TotalStockValue = item.CurrentStock * item.UnitCost;
                        item.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
            else
            {
                // Draft cancel: release active reservations to return available stock
                var activeReservations = await _db.StockReservations
                    .Where(r => r.VoucherId == voucher.VoucherId && r.Status == 1)
                    .ToListAsync();
                if (activeReservations.Count > 0)
                {
                    var affectedItemLocationIds = new List<int>();
                    foreach (var r in activeReservations)
                    {
                        r.ReleasedQty = r.ReservedQty - r.ConsumedQty;
                        r.Status = 3;
                        r.UpdatedAt = DateTime.UtcNow;

                        var il = await _db.ItemLocations.FirstOrDefaultAsync(x =>
                            x.ItemId == r.ItemId
                            && x.LocationId == r.LocationId
                            && x.LotNumber == r.LotNumber
                            && x.ExpiryDate == r.ExpiryDate);
                        if (il != null) affectedItemLocationIds.Add(il.ItemLocationId);
                    }
                    await RecalculateReservedQtyAsync(affectedItemLocationIds);
                }
            }

            voucher.IsCancelled = true;
            voucher.CancelledBy = user;
            voucher.CancelledAt = DateTime.UtcNow;
            voucher.CancelReason = cancelReason ?? "";

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = $"Đã hủy phiếu thành công!";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = $"Lỗi hủy phiếu: {ex.Message}";
        }

        return RedirectToAction("Details", new { id });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ReplenishDefect(long id)
    {
        var original = await _db.Vouchers
            .Include(v => v.Details)
            .FirstOrDefaultAsync(v => v.VoucherId == id);

        if (original == null) return NotFound();
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && original.WarehouseId != scopedWh.Value)
            return Forbid();
        if (!original.IsPartial)
        {
            TempData["Error"] = "Phiếu này không có hàng lỗi để bù!";
            return RedirectToAction("Details", new { id });
        }

        var newVoucher = new VoucherCreateViewModel
        {
            VoucherType = 1, // Nhập kho
            WarehouseId = original.WarehouseId,
            PartnerId = original.PartnerId,
            ReferenceNo = $"BUHANG-{original.VoucherCode}",
            Description = $"Bù hàng lỗi cho phiếu {original.VoucherCode}",
            ParentVoucherId = original.VoucherId,
        };

        foreach (var d in original.Details.Where(x => x.DefectQty > 0))
        {
            newVoucher.Lines.Add(new VoucherDetailLine
            {
                ItemId = d.ItemId,
                LocationId = d.LocationId,
                TransactionQty = d.DefectQty, // Lấy số lượng lỗi làm số lượng chính để nhập bù
                DefectQty = 0,
                UnitPrice = d.UnitPrice,
                LineAmount = d.UnitPrice * d.DefectQty, // Base quantity for replenishment is DefectQty
                TransactionUomId = d.TransactionUomId
            });
        }

        TempData["Info"] = $"Đang tạo phiếu bù hàng từ {original.VoucherCode}. Vui lòng kiểm tra và lưu lại.";
        
        // Pass the prefilled model to standard Create view
        newVoucher.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
        newVoucher.Partners = await _db.Partners.Where(p => p.IsActive).ToListAsync();
        newVoucher.Items = await _db.Items.Include(i => i.BaseUom).Where(i => i.IsActive).OrderBy(i => i.ItemCode).ToListAsync();
        newVoucher.Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).ToListAsync();
        newVoucher.Locations = await _db.Locations.Where(l => l.IsActive).ToListAsync();

        return View("Create", newVoucher);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnalyzeReceipt(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("Vui lòng chọn ảnh hóa đơn.");
        var canAutoCreateItem = User.IsInRole("Admin") || User.IsInRole("Manager");

        try 
        {
            var apiKey = _config["GeminiApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return BadRequest("Thiếu cấu hình Gemini API Key.");

            // Basic upload hardening
            const long maxBytes = 8 * 1024 * 1024; // 8 MB
            if (file.Length > maxBytes) return BadRequest("Ảnh quá lớn. Vui lòng chọn ảnh ≤ 8MB.");

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
            var allowedExt = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExt.Contains(ext)) return BadRequest("Định dạng ảnh không hợp lệ. Chỉ hỗ trợ JPG/PNG/WEBP.");

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var base64Image = Convert.ToBase64String(memoryStream.ToArray());
            var mimeType = file.ContentType;

            // LƯU ẢNH VÀO Ổ ĐĨA ĐỂ ĐỐI SOÁT VỚI AI OCR
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "receipts");
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);
            
            var safeOriginalName = Path.GetFileName(file.FileName);
            var uniqueFileName = Guid.NewGuid().ToString("N") + "_" + safeOriginalName;
            var physicalPath = Path.Combine(uploadDir, uniqueFileName);
            
            await System.IO.File.WriteAllBytesAsync(physicalPath, memoryStream.ToArray());
            var imageUrl = "/uploads/receipts/" + uniqueFileName;

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = "Extract ALL items from this receipt/invoice image. Return ONLY a raw JSON array without markdown formatting or code blocks. Include the unit of measurement. Format: [{\"ItemCode\":\"abc\",\"ItemName\":\"xyz\",\"Quantity\":1.0,\"UnitPrice\":1000,\"UnitName\":\"Cái\"}]. UnitName examples: Cái, Bộ, Cuộn, Chai, m², kg, Hộp, Thùng, Pcs, Pair, Set." },
                            new { inlineData = new { mimeType = mimeType, data = base64Image } }
                        }
                    }
                }
            };

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}", content);
            
            var responseString = await response.Content.ReadAsStringAsync();
            
            // Fallback to 2.0-flash if 2.5 is overloaded (503)
            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                var fallbackContent = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
                response = await client.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}", fallbackContent);
                responseString = await response.Content.ReadAsStringAsync();
            }

            if (!response.IsSuccessStatusCode) return BadRequest($"Gemini Error: {responseString}");

            var jsonDoc = JsonDocument.Parse(responseString);
            var textResult = jsonDoc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

            if (textResult != null)
            {
                // Strip common markdown code fences if returned
                textResult = textResult.Trim();
                if (textResult.StartsWith("```", StringComparison.Ordinal))
                {
                    var firstNewline = textResult.IndexOf('\n');
                    if (firstNewline >= 0) textResult = textResult[(firstNewline + 1)..];
                    if (textResult.EndsWith("```", StringComparison.Ordinal)) textResult = textResult[..^3];
                }
                textResult = textResult.Trim();
            }
            else
            {
                textResult = "[]";
            }

            var ocrLog = new AiOcrLog {
                ImageUrl = imageUrl,
                FileName = file.FileName,
                FileSize = file.Length,
                ParsedData = textResult,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow
            };
            _db.Set<AiOcrLog>().Add(ocrLog);
            await _db.SaveChangesAsync();

            // Tự động xử lý vật tư
            var mappedItems = new List<object>();
            try 
            {
                using var doc = JsonDocument.Parse(textResult);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var defaultUomId = await _db.UnitsOfMeasure.Select(u => u.UomId).FirstOrDefaultAsync();
                    var allUoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).ToListAsync();
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        var code = el.TryGetProperty("ItemCode", out var codeProp) ? codeProp.GetString() : null;
                        var name = el.TryGetProperty("ItemName", out var nameProp) ? nameProp.GetString() : null;
                        var price = el.TryGetProperty("UnitPrice", out var priceProp) && priceProp.TryGetDecimal(out var decPrice) ? decPrice : 0;
                        var qty = el.TryGetProperty("Quantity", out var qtyProp) && qtyProp.TryGetDecimal(out var decQty) ? decQty : 1;
                        var unitName = el.TryGetProperty("UnitName", out var unitProp) ? unitProp.GetString() : null;
                        
                        // Match UOM from bill's unit name
                        int matchedUomId = defaultUomId;
                        if (!string.IsNullOrEmpty(unitName))
                        {
                            var unitLower = unitName.ToLower().Trim();
                            var matchedUom = allUoms.FirstOrDefault(u => 
                                u.UomCode.ToLower() == unitLower || 
                                u.UomName.ToLower() == unitLower ||
                                u.UomName.ToLower().Contains(unitLower) || 
                                unitLower.Contains(u.UomName.ToLower()) ||
                                u.UomCode.ToLower().Contains(unitLower) ||
                                unitLower.Contains(u.UomCode.ToLower())
                            );
                            if (matchedUom != null) matchedUomId = matchedUom.UomId;
                        }
                        
                        if (!string.IsNullOrEmpty(code) || !string.IsNullOrEmpty(name))
                        {
                            var searchName = name ?? code;
                            bool isNew = false;
                            // 1. Exact match by code or name
                            var existingItem = await _db.Items.FirstOrDefaultAsync(x => (code != null && x.ItemCode == code) || (name != null && x.ItemName == name));
                            // 2. Fuzzy match: Contains on name (handles typos like "bột" vs "bọt")
                            if (existingItem == null && !string.IsNullOrEmpty(name))
                            {
                                var nameLower = name.ToLower();
                                var candidates = await _db.Items.Where(x => x.IsActive).ToListAsync();
                                existingItem = candidates.FirstOrDefault(x => 
                                    x.ItemName.ToLower().Contains(nameLower) || 
                                    nameLower.Contains(x.ItemName.ToLower()) ||
                                    nameLower.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                        .Where(w => w.Length >= 3)
                                        .Count(w => x.ItemName.ToLower().Contains(w)) >= 2
                                );
                            }
                            // 3. Fuzzy match by code fragments
                            if (existingItem == null && !string.IsNullOrEmpty(code))
                            {
                                var codeLower = code.ToLower();
                                existingItem = await _db.Items.FirstOrDefaultAsync(x => x.ItemCode.ToLower().Contains(codeLower) || codeLower.Contains(x.ItemCode.ToLower()));
                            }
                            // 4. Not found → Auto-create new item
                            if (existingItem == null)
                            {
                                if (!canAutoCreateItem)
                                    throw new Exception($"Không thể tự tạo vật tư mới '{searchName ?? code}'. Nhân viên chỉ được map vào vật tư đã có sẵn.");
                                var nCode = string.IsNullOrEmpty(code) ? $"AI-{Guid.NewGuid().ToString().Substring(0,6)}" : code;
                                existingItem = new Item { 
                                    ItemCode = nCode, 
                                    ItemName = searchName ?? nCode, 
                                    ItemType = 1, 
                                    BaseUomId = matchedUomId, 
                                    UnitCost = price, 
                                    IsActive = true,
                                    CreatedBy = "AI OCR",
                                    CreatedAt = DateTime.UtcNow
                                };
                                _db.Items.Add(existingItem);
                                await _db.SaveChangesAsync();
                                isNew = true;
                            }
                            mappedItems.Add(new { ItemId = existingItem.ItemId, ItemCode = existingItem.ItemCode, ItemName = existingItem.ItemName, Quantity = qty, UnitPrice = price, BaseUomId = existingItem.BaseUomId, IsNew = isNew });
                        }
                    }
                }
            } 
            catch { }

            return Ok(new { data = System.Text.Json.JsonSerializer.Serialize(mappedItems), logId = ocrLog.AiOcrLogId });
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi AI OCR: {ex.Message}");
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public IActionResult DownloadImportTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("ImportLines");

        ws.Cell(1, 1).Value = "ItemCode";
        ws.Cell(1, 2).Value = "ItemName";
        ws.Cell(1, 3).Value = "Quantity";
        ws.Cell(1, 4).Value = "UnitPrice";
        ws.Cell(1, 5).Value = "UnitName";
        ws.Cell(1, 6).Value = "LocationCode";
        ws.Cell(1, 7).Value = "ExpiryDate (yyyy-MM-dd)";
        ws.Cell(1, 8).Value = "LotNumber";
        ws.Cell(1, 9).Value = "DefectQty";
        ws.Cell(1, 10).Value = "Notes";

        ws.Range(1, 1, 1, 10).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#111827");
        ws.Range(1, 1, 1, 10).Style.Font.FontColor = XLColor.White;

        ws.Cell(2, 1).Value = "VT-001";
        ws.Cell(2, 2).Value = "Bu-lông neo M20x500";
        ws.Cell(2, 3).Value = 10;
        ws.Cell(2, 4).Value = 0;
        ws.Cell(2, 5).Value = "Pcs";
        ws.Cell(2, 6).Value = "A1-01";
        ws.Cell(2, 7).Value = "";
        ws.Cell(2, 8).Value = "";
        ws.Cell(2, 9).Value = 0;
        ws.Cell(2, 10).Value = "";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "WMS_ImportLines_Template.xlsx");
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> DownloadDemoImport100()
    {
        // Generate a 100-row demo file (best-effort using current master data)
        var items = await _db.Items
            .Include(i => i.BaseUom)
            .Where(i => i.IsActive)
            .OrderBy(i => i.ItemCode)
            .Take(300)
            .ToListAsync();

        var locations = await _db.Locations
            .Where(l => l.IsActive)
            .OrderBy(l => l.LocationCode)
            .Take(300)
            .Select(l => l.LocationCode)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("ImportLines");

        // Header
        ws.Cell(1, 1).Value = "ItemCode";
        ws.Cell(1, 2).Value = "ItemName";
        ws.Cell(1, 3).Value = "Quantity";
        ws.Cell(1, 4).Value = "UnitPrice";
        ws.Cell(1, 5).Value = "UnitName";
        ws.Cell(1, 6).Value = "LocationCode";
        ws.Cell(1, 7).Value = "ExpiryDate (yyyy-MM-dd)";
        ws.Cell(1, 8).Value = "LotNumber";
        ws.Cell(1, 9).Value = "DefectQty";
        ws.Cell(1, 10).Value = "Notes";

        ws.Range(1, 1, 1, 10).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#111827");
        ws.Range(1, 1, 1, 10).Style.Font.FontColor = XLColor.White;

        var rng = new Random();
        for (int i = 0; i < 100; i++)
        {
            var row = i + 2;
            var it = items.Count > 0 ? items[rng.Next(items.Count)] : null;
            var loc = locations.Count > 0 ? locations[rng.Next(locations.Count)] : "";

            var qty = rng.Next(1, 30);
            var defect = rng.NextDouble() < 0.10 ? rng.Next(1, Math.Min(3, qty + 1)) : 0; // ~10% rows have small defect
            var unitName = it?.BaseUom?.UomCode ?? "Pcs";
            var price = it != null ? it.UnitCost : 0;

            ws.Cell(row, 1).Value = it?.ItemCode ?? $"DEMO-{(i + 1):D3}";
            ws.Cell(row, 2).Value = it?.ItemName ?? $"Demo Item {(i + 1):D3}";
            ws.Cell(row, 3).Value = qty;
            ws.Cell(row, 4).Value = price;
            ws.Cell(row, 5).Value = unitName;
            ws.Cell(row, 6).Value = loc;

            // Add expiry dates to some rows to demo FEFO
            if (rng.NextDouble() < 0.35)
            {
                var days = rng.Next(5, 180);
                ws.Cell(row, 7).Value = DateTime.UtcNow.Date.AddDays(days).ToString("yyyy-MM-dd");
            }
            else
            {
                ws.Cell(row, 7).Value = "";
            }

            // Always include a LotNumber so demo/import showcases batch tracking
            ws.Cell(row, 8).Value = $"LOT-{DateTime.UtcNow:yyMMdd}-{rng.Next(1000, 9999)}";
            ws.Cell(row, 9).Value = defect;
            ws.Cell(row, 10).Value = defect > 0 ? "Có lỗi/thiếu (demo)" : "";
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "WMS_ImportLines_Demo_100rows.xlsx");
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportLinesExcel(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("Vui lòng chọn file Excel.");
        var canAutoCreateItem = User.IsInRole("Admin") || User.IsInRole("Manager");
        const long maxBytes = 5 * 1024 * 1024; // 5MB
        if (file.Length > maxBytes) return BadRequest("File quá lớn. Vui lòng chọn file ≤ 5MB.");

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
        if (ext != ".xlsx") return BadRequest("Định dạng không hợp lệ. Chỉ hỗ trợ .xlsx");

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws == null) return BadRequest("File Excel không có worksheet.");

            var defaultUomId = await _db.UnitsOfMeasure.Select(u => u.UomId).FirstOrDefaultAsync();
            var allUoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).ToListAsync();

            var locMap = await _db.Locations
                .Where(l => l.IsActive)
                .ToDictionaryAsync(l => l.LocationCode.ToLower(), l => l.LocationId);

            var mappedItems = new List<object>();

            var row = 2;
            while (true)
            {
                var code = ws.Cell(row, 1).GetString()?.Trim();
                var name = ws.Cell(row, 2).GetString()?.Trim();
                var qtyStr = ws.Cell(row, 3).GetString()?.Trim();
                var priceStr = ws.Cell(row, 4).GetString()?.Trim();
                var unitName = ws.Cell(row, 5).GetString()?.Trim();
                var locationCode = ws.Cell(row, 6).GetString()?.Trim();
                var expiryStr = ws.Cell(row, 7).GetString()?.Trim();
                var lotNumber = ws.Cell(row, 8).GetString()?.Trim();
                var defectStr = ws.Cell(row, 9).GetString()?.Trim();
                var notes = ws.Cell(row, 10).GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(qtyStr))
                    break;

                if (!decimal.TryParse(qtyStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var qty))
                    decimal.TryParse(qtyStr, NumberStyles.Any, new CultureInfo("vi-VN"), out qty);
                if (qty <= 0) qty = 1;

                decimal price = 0;
                if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out price))
                    decimal.TryParse(priceStr, NumberStyles.Any, new CultureInfo("vi-VN"), out price);

                decimal defectQty = 0;
                if (!decimal.TryParse(defectStr, NumberStyles.Any, CultureInfo.InvariantCulture, out defectQty))
                    decimal.TryParse(defectStr, NumberStyles.Any, new CultureInfo("vi-VN"), out defectQty);
                if (defectQty < 0) defectQty = 0;

                DateTime? expiry = null;
                if (!string.IsNullOrWhiteSpace(expiryStr))
                {
                    if (DateTime.TryParseExact(expiryStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                        expiry = dt.Date;
                    else if (DateTime.TryParse(expiryStr, out var dt2))
                        expiry = dt2.Date;
                }

                int matchedUomId = defaultUomId;
                if (!string.IsNullOrEmpty(unitName))
                {
                    var unitLower = unitName.ToLower().Trim();
                    var matchedUom = allUoms.FirstOrDefault(u =>
                        u.UomCode.ToLower() == unitLower ||
                        u.UomName.ToLower() == unitLower ||
                        u.UomName.ToLower().Contains(unitLower) ||
                        unitLower.Contains(u.UomName.ToLower()) ||
                        u.UomCode.ToLower().Contains(unitLower) ||
                        unitLower.Contains(u.UomCode.ToLower())
                    );
                    if (matchedUom != null) matchedUomId = matchedUom.UomId;
                }

                var searchName = name ?? code;
                bool isNew = false;

                Item? existingItem = null;
                if (!string.IsNullOrWhiteSpace(code))
                    existingItem = await _db.Items.FirstOrDefaultAsync(x => x.ItemCode == code);
                if (existingItem == null && !string.IsNullOrWhiteSpace(name))
                    existingItem = await _db.Items.FirstOrDefaultAsync(x => x.ItemName == name);

                if (existingItem == null)
                {
                    if (!canAutoCreateItem)
                        return BadRequest($"Dòng {row}: vật tư '{searchName ?? code}' chưa tồn tại. Nhân viên không được tự tạo vật tư mới.");
                    var nCode = string.IsNullOrEmpty(code) ? $"IMP-{Guid.NewGuid().ToString()[..6]}" : code!;
                    existingItem = new Item
                    {
                        ItemCode = nCode,
                        ItemName = searchName ?? nCode,
                        ItemType = 1,
                        BaseUomId = matchedUomId,
                        UnitCost = price,
                        IsActive = true,
                        CreatedBy = "Excel Import",
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Items.Add(existingItem);
                    await _db.SaveChangesAsync();
                    isNew = true;
                }

                int? locationId = null;
                if (!string.IsNullOrWhiteSpace(locationCode))
                {
                    var key = locationCode.ToLower().Trim();
                    if (locMap.TryGetValue(key, out var lid)) locationId = lid;
                }

                mappedItems.Add(new
                {
                    ItemId = existingItem.ItemId,
                    ItemCode = existingItem.ItemCode,
                    ItemName = existingItem.ItemName,
                    Quantity = qty,
                    UnitPrice = price,
                    BaseUomId = existingItem.BaseUomId,
                    IsNew = isNew,
                    LocationId = locationId,
                    ExpiryDate = expiry?.ToString("yyyy-MM-dd"),
                    LotNumber = lotNumber,
                    DefectQty = defectQty,
                    Notes = notes
                });

                row++;
                if (row > 5000) break;
            }

            return Ok(new { data = System.Text.Json.JsonSerializer.Serialize(mappedItems) });
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi import Excel: {ex.Message}");
        }
    }



    [HttpGet]
    public async Task<IActionResult> GetConversionRate(int fromUomId, int toUomId)
    {
        if (fromUomId == toUomId) return Json(new { rate = 1m, found = true });

        // Direct lookup (global)
        var directMatches = await _db.UnitConversions
            .Where(uc => uc.FromUomId == fromUomId && uc.ToUomId == toUomId && uc.IsActive && uc.ItemId == null)
            .ToListAsync();
        if (directMatches.Count > 1)
            return Json(new { rate = 1m, found = false, error = "Trùng quy đổi toàn cục cho cùng cặp ĐVT." });
        var conversion = directMatches.FirstOrDefault();
        if (conversion != null)
            return Json(new { rate = conversion.ConversionRate, found = true });

        // Reverse lookup (global)
        var reverseMatches = await _db.UnitConversions
            .Where(uc => uc.FromUomId == toUomId && uc.ToUomId == fromUomId && uc.IsActive && uc.ItemId == null)
            .ToListAsync();
        if (reverseMatches.Count > 1)
            return Json(new { rate = 1m, found = false, error = "Trùng quy đổi ngược toàn cục cho cùng cặp ĐVT." });
        var reverse = reverseMatches.FirstOrDefault();
        if (reverse != null && reverse.ConversionRate != 0)
            return Json(new { rate = 1m / reverse.ConversionRate, found = true });

        return Json(new { rate = 1m, found = false });
    }
}
