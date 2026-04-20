using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;
using Microsoft.AspNetCore.Authorization;

namespace WMS.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
public class SystemController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public SystemController(AppDbContext db, IWebHostEnvironment env, IConfiguration config)
    {
        _db = db;
        _env = env;
        _config = config;
    }

    private bool IsDangerOpsAllowed()
    {
        // Safety: allow destructive ops only in Development unless explicitly enabled
        return _env.IsDevelopment() || string.Equals(_config["System:AllowDangerOps"], "true", StringComparison.OrdinalIgnoreCase);
    }

    // === ĐƠN VỊ TÍNH (UOM) ===
    [HttpGet]
    public async Task<IActionResult> Units()
    {
        var uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).OrderBy(u => u.UomCode).ToListAsync();
        ViewBag.PackagingUnits = await _db.PackagingUnits
            .Include(p => p.BaseUom)
            .Where(p => p.IsActive).OrderBy(p => p.TenDongGoi).ToListAsync();
        ViewBag.AllUoms = uoms;
        return View("~/Views/Units/Index.cshtml", uoms);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUnit(string uomCode, string uomName)
    {
        if (!string.IsNullOrWhiteSpace(uomCode) && !string.IsNullOrWhiteSpace(uomName))
        {
            if (!await _db.UnitsOfMeasure.AnyAsync(u => u.UomCode == uomCode))
            {
                _db.UnitsOfMeasure.Add(new UnitOfMeasure { UomCode = uomCode, UomName = uomName, IsActive = true });
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm ĐVT '{uomName}'!";
            }
            else
            {
                TempData["Error"] = $"Mã ĐVT '{uomCode}' đã tồn tại!";
            }
        }
        return RedirectToAction("Units");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUnit(int id)
    {
        var uom = await _db.UnitsOfMeasure.FindAsync(id);
        if (uom != null)
        {
            uom.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa ĐVT '{uom.UomName}'!";
        }
        return RedirectToAction("Units");
    }

    // === ĐÓNG GÓI (Packaging) ===
    [HttpPost]
    public async Task<IActionResult> CreatePackaging(string tenDongGoi, int baseUomId, decimal giaTri)
    {
        if (!string.IsNullOrWhiteSpace(tenDongGoi) && giaTri > 0)
        {
            if (await _db.PackagingUnits.AnyAsync(p => p.TenDongGoi == tenDongGoi && p.IsActive))
            {
                TempData["Error"] = $"Tên đóng gói '{tenDongGoi}' đã tồn tại!";
            }
            else
            {
                _db.PackagingUnits.Add(new PackagingUnit
                {
                    TenDongGoi = tenDongGoi,
                    BaseUomId = baseUomId,
                    GiaTri = giaTri,
                    IsActive = true
                });
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm quy cách đóng gói '{tenDongGoi}'!";
            }
        }
        return RedirectToAction("Units");
    }

    [HttpPost]
    public async Task<IActionResult> DeletePackaging(int id)
    {
        var pkg = await _db.PackagingUnits.FindAsync(id);
        if (pkg != null)
        {
            pkg.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa đóng gói '{pkg.TenDongGoi}'!";
        }
        return RedirectToAction("Units");
    }

    // === SEED DATA ===
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedData()
    {
        if (!IsDangerOpsAllowed()) return Forbid();

        var output = new System.Text.StringBuilder();

        // ══════════════════════════════════════════════════════════════
        // 1. SEED ĐƠN VỊ TÍNH (Units of Measure) - Đầy đủ & Chuẩn
        // ══════════════════════════════════════════════════════════════
        var uoms = new List<(string Code, string Name)>
        {
            // --- Khối lượng (Weight/Mass) ---
            ("kg",    "Kilogram"),
            ("g",     "Gram"),
            ("mg",    "Miligram"),
            ("tấn",   "Tấn"),
            ("lạng",  "Lạng (100g)"),
            ("yến",   "Yến (10kg)"),
            ("tạ",    "Tạ (100kg)"),

            // --- Thể tích (Volume) ---
            ("L",     "Lít"),
            ("mL",    "Mililit"),
            ("m³",    "Mét khối"),
            ("cm³",   "Centimet khối"),
            ("gal",   "Gallon"),

            // --- Chiều dài (Length) ---
            ("m",     "Mét"),
            ("cm",    "Centimet"),
            ("mm",    "Milimet"),
            ("km",    "Kilomet"),
            ("in",    "Inch"),
            ("ft",    "Feet"),

            // --- Diện tích (Area) ---
            ("m²",    "Mét vuông"),
            ("cm²",   "Centimet vuông"),

            // --- Số lượng đếm (Counting/Quantity) ---
            ("cái",   "Cái"),
            ("chiếc", "Chiếc"),
            ("bộ",    "Bộ"),
            ("đôi",   "Đôi"),
            ("con",   "Con"),
            ("tờ",    "Tờ / Tấm"),
            ("cuộn",  "Cuộn"),
            ("thanh", "Thanh"),
            ("ống",   "Ống"),
            ("sợi",   "Sợi"),
            ("viên",  "Viên"),
            ("gói",   "Gói"),
            ("túi",   "Túi"),
            ("chai",  "Chai"),
            ("lon",   "Lon"),
            ("lọ",    "Lọ"),
            ("hũ",    "Hũ"),
            ("bình",  "Bình"),
            ("can",   "Can"),
            ("xô",    "Xô"),

            // --- Đóng gói / Bao bì (Packaging) ---
            ("thùng", "Thùng"),
            ("hộp",   "Hộp"),
            ("bao",   "Bao"),
            ("kiện",  "Kiện"),
            ("pallet","Pallet"),
            ("lô",    "Lô"),
            ("két",   "Két"),
            ("khay",  "Khay"),
            ("bịch",  "Bịch"),

            // --- Thời gian (Time) ---
            ("giờ",   "Giờ"),
            ("ngày",  "Ngày"),
            ("tháng", "Tháng"),
        };

        foreach (var u in uoms)
        {
            if (!await _db.UnitsOfMeasure.AnyAsync(x => x.UomCode == u.Code))
            {
                _db.UnitsOfMeasure.Add(new UnitOfMeasure { UomCode = u.Code, UomName = u.Name, IsActive = true });
                output.AppendLine($"✓ ĐVT: {u.Code} - {u.Name}");
            }
        }
        await _db.SaveChangesAsync();

        // ══════════════════════════════════════════════════════════════
        // 2. SEED QUY ĐỔI ĐƠN VỊ (Unit Conversions) - Chuẩn quốc tế
        // ══════════════════════════════════════════════════════════════
        var conversions = new List<(string From, string To, decimal Rate)>
        {
            // Khối lượng
            ("kg",   "g",     1000m),
            ("kg",   "mg",    1000000m),
            ("tấn",  "kg",    1000m),
            ("tạ",   "kg",    100m),
            ("yến",  "kg",    10m),
            ("lạng", "g",     100m),
            ("kg",   "lạng",  10m),

            // Thể tích
            ("L",    "mL",    1000m),
            ("m³",   "L",     1000m),
            ("m³",   "cm³",   1000000m),
            ("L",    "cm³",   1000m),
            ("gal",  "L",     3.78541m),

            // Chiều dài
            ("m",    "cm",    100m),
            ("m",    "mm",    1000m),
            ("km",   "m",     1000m),
            ("cm",   "mm",    10m),
            ("ft",   "cm",    30.48m),
            ("in",   "cm",    2.54m),
            ("ft",   "in",    12m),
            ("m",    "ft",    3.28084m),
            ("m",    "in",    39.3701m),

            // Diện tích
            ("m²",   "cm²",   10000m),

            // Thời gian
            ("ngày", "giờ",   24m),
            ("tháng","ngày",  30m),
        };

        foreach (var c in conversions)
        {
            var fromUom = await _db.UnitsOfMeasure.FirstOrDefaultAsync(u => u.UomCode == c.From);
            var toUom = await _db.UnitsOfMeasure.FirstOrDefaultAsync(u => u.UomCode == c.To);
            if (fromUom != null && toUom != null)
            {
                bool exists = await _db.UnitConversions.AnyAsync(x =>
                    x.FromUomId == fromUom.UomId && x.ToUomId == toUom.UomId && x.ItemId == null);
                if (!exists)
                {
                    _db.UnitConversions.Add(new UnitConversion
                    {
                        ItemId = null, // Quy đổi toàn cục (global)
                        FromUomId = fromUom.UomId,
                        ToUomId = toUom.UomId,
                        ConversionRate = c.Rate,
                        IsActive = true
                    });
                    output.AppendLine($"✓ Quy đổi: 1 {c.From} = {c.Rate} {c.To}");
                }
            }
        }
        await _db.SaveChangesAsync();

        // ══════════════════════════════════════════════════════════════
        // 3. SEED KHO & KHU VỰC (Warehouse & Zones & Locations)
        // ══════════════════════════════════════════════════════════════
        var mainWarehouse = await _db.Warehouses.FirstOrDefaultAsync();
        if (mainWarehouse == null)
        {
            mainWarehouse = new Warehouse { WarehouseCode = "WH01", WarehouseName = "Kho Chính", IsActive = true };
            _db.Warehouses.Add(mainWarehouse);
            await _db.SaveChangesAsync();
            output.AppendLine("✓ Kho: WH01 - Kho Chính");
        }

        var storageZone = await _db.Zones.FirstOrDefaultAsync(z => z.WarehouseId == mainWarehouse.WarehouseId && z.ZoneType == 1);
        if (storageZone == null)
        {
            storageZone = new Zone { 
                WarehouseId = mainWarehouse.WarehouseId, 
                ZoneCode = "LT", 
                ZoneName = "Khu Vực Lưu Trữ", 
                ZoneType = 1, 
                IsActive = true 
            };
            _db.Zones.Add(storageZone);
            await _db.SaveChangesAsync();
            output.AppendLine("✓ Khu vực: LT - Khu Vực Lưu Trữ");
        }

        // Create Racks A14, A15
        string[] rackCodes = { "A14", "A15" };
        int levels = 6;
        int binsPerRow = 10;

        foreach (var rack in rackCodes)
        {
            for (int level = 1; level <= levels; level++)
            {
                for (int bin = 1; bin <= binsPerRow; bin++)
                {
                    string locCode = $"{rack}-{level:D2}-{bin:D2}";
                    if (!await _db.Locations.AnyAsync(l => l.LocationCode == locCode))
                    {
                        _db.Locations.Add(new Location {
                            ZoneId = storageZone.ZoneId,
                            LocationCode = locCode,
                            RackCode = rack,
                            ShelfCode = level.ToString(),
                            BinCode = bin.ToString(),
                            IsActive = true
                        });
                    }
                }
            }
        }
        await _db.SaveChangesAsync();
        output.AppendLine("✓ Vị trí kho: A14/A15 (6 tầng × 10 ô)");

        // ══════════════════════════════════════════════════════════════
        // 4. SEED QUY CÁCH ĐÓNG GÓI (Packaging Units) - Đa dạng
        // ══════════════════════════════════════════════════════════════
        var packagings = new List<(string Name, string UomCode, decimal Value)>
        {
            // Đóng gói theo Lít
            ("Thùng 200L (phuy)", "L", 200),
            ("Thùng 50L",         "L", 50),
            ("Thùng 30L",         "L", 30),
            ("Thùng 20L",         "L", 20),
            ("Can 5L",            "L", 5),
            ("Can 1L",            "L", 1),
            ("Chai 500mL",        "mL", 500),
            ("Chai 330mL",        "mL", 330),

            // Đóng gói theo Cái/Chiếc
            ("Thùng 100 cái",   "cái",  100),
            ("Thùng 50 cái",    "cái",  50),
            ("Hộp 1000 cái",    "cái",  1000),
            ("Hộp 500 cái",     "cái",  500),
            ("Hộp 100 cái",     "cái",  100),
            ("Gói 10 cái",      "cái",  10),

            // Đóng gói theo Kg
            ("Bao 50kg",   "kg", 50),
            ("Bao 25kg",   "kg", 25),
            ("Bao 10kg",   "kg", 10),
            ("Bao 5kg",    "kg", 5),
            ("Gói 1kg",    "kg", 1),
            ("Gói 500g",   "g",  500),

            // Đóng gói theo Mét
            ("Cuộn 100m",  "m",  100),
            ("Cuộn 50m",   "m",  50),

            // Đóng gói theo Viên
            ("Hộp 100 viên", "viên", 100),
            ("Vỉ 10 viên",  "viên", 10),

            // Đóng gói theo Tờ/Tấm
            ("Ream 500 tờ",  "tờ",  500),
            ("Kiện 100 tấm", "tờ",  100),
        };

        foreach (var p in packagings)
        {
            if (!await _db.PackagingUnits.AnyAsync(x => x.TenDongGoi == p.Name))
            {
                var uom = await _db.UnitsOfMeasure.FirstOrDefaultAsync(u => u.UomCode == p.UomCode);
                if (uom != null)
                {
                    _db.PackagingUnits.Add(new PackagingUnit { TenDongGoi = p.Name, BaseUomId = uom.UomId, GiaTri = p.Value, IsActive = true });
                    output.AppendLine($"✓ Đóng gói: {p.Name} ({p.Value} {p.UomCode})");
                }
            }
        }
        await _db.SaveChangesAsync();

        return Content($"═══ SEED HOÀN TẤT ═══\n\n{output.ToString()}");
    }

    // === CẤU HÌNH DUNG TÍCH CHO CÁC ITEM ===
    [HttpGet]
    public async Task<IActionResult> ConfigureCapacity()
    {
        try
        {
            // Lấy UomId của "Lít" hoặc "L"
            var literUom = await _db.UnitsOfMeasure.FirstOrDefaultAsync(u => u.UomCode == "L" || u.UomCode == "Lít");
            if (literUom == null)
            {
                return Content("Lỗi: Không tìm thấy đơn vị tính 'Lít' trong hệ thống!");
            }

            var output = new System.Text.StringBuilder();
            output.AppendLine("═══ CẤU HÌNH DUNG TÍCH CHO ITEMS ═══\n");

            // Cấu hình dung tích cho các item sơn
            // Removed - capacity tracking not needed

            // Tính lại TotalCapacity cho tất cả ItemLocation
            // Removed - capacity tracking not needed
            output.AppendLine("\n═══ HOÀN TẤT ═══");

            return Content(output.ToString(), "text/plain");
        }
        catch (Exception ex)
        {
            return Content($"Lỗi: {ex.Message}");
        }
    }

    /// <summary>
    /// Gom 10 ô/tầng thành 1 ô/tầng trong database.
    /// Truy cập: /System/MergeLocationsPerLevel
    /// CHỈ CHẠY 1 LẦN!
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MergeLocationsPerLevel()
    {
        if (!IsDangerOpsAllowed()) return Forbid();

        var output = new System.Text.StringBuilder();
        output.AppendLine("═══ BẮT ĐẦU GOM Ô KHO (10 slot → 1 ô/tầng) ═══\n");

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // Lấy tất cả zone loại kệ (ZoneType=1)
            var zones = await _db.Zones.Where(z => z.IsActive && z.ZoneType == 1).ToListAsync();
            output.AppendLine($"Tìm thấy {zones.Count} khu kệ cần xử lý.\n");

            foreach (var zone in zones)
            {
                var locations = await _db.Locations
                    .Where(l => l.ZoneId == zone.ZoneId && l.IsActive)
                    .OrderBy(l => l.ShelfCode).ThenBy(l => l.BinCode)
                    .ToListAsync();

                if (locations.Count <= 6)
                {
                    output.AppendLine($"  [{zone.ZoneCode}] Đã có ≤6 ô, bỏ qua.");
                    continue;
                }

                // Group by ShelfCode (level)
                var levels = locations.GroupBy(l => l.ShelfCode ?? "1").OrderBy(g => g.Key);

                foreach (var level in levels)
                {
                    var locsInLevel = level.OrderBy(l => l.LocationCode).ToList();
                    if (locsInLevel.Count <= 1) continue;

                    var master = locsInLevel[0]; // Keep first slot as master
                    var toRemove = locsInLevel.Skip(1).ToList();

                    output.AppendLine($"  [{zone.ZoneCode}] Tầng {level.Key}: Gom {locsInLevel.Count} ô → 1 (master: {master.LocationCode})");

                    foreach (var loc in toRemove)
                    {
                        // 1. Move ItemLocations to master
                        var itemLocs = await _db.ItemLocations
                            .Where(il => il.LocationId == loc.LocationId)
                            .ToListAsync();

                        foreach (var il in itemLocs)
                        {
                            var existing = await _db.ItemLocations
                                .FirstOrDefaultAsync(m => m.LocationId == master.LocationId && m.ItemId == il.ItemId);

                            if (existing != null)
                            {
                                existing.Quantity += il.Quantity;
                                existing.UpdatedAt = DateTime.UtcNow;
                                output.AppendLine($"    → Gom {il.Quantity} từ {loc.LocationCode} vào master (Item #{il.ItemId})");
                            }
                            else
                            {
                                // Move to master
                                il.LocationId = master.LocationId;
                                output.AppendLine($"    → Chuyển ItemLocation từ {loc.LocationCode} → master (Item #{il.ItemId}, SL: {il.Quantity})");
                            }
                        }

                        // Remove itemLocations that were merged (duplicates)
                        var dupeItemLocs = await _db.ItemLocations
                            .Where(il => il.LocationId == loc.LocationId)
                            .ToListAsync();
                        _db.ItemLocations.RemoveRange(dupeItemLocs);

                        // 2. Update VoucherDetails pointing to this location
                        var voucherDetails = await _db.VoucherDetails
                            .Where(vd => vd.LocationId == loc.LocationId)
                            .ToListAsync();
                        foreach (var vd in voucherDetails)
                        {
                            vd.LocationId = master.LocationId;
                        }

                        var destVoucherDetails = await _db.VoucherDetails
                            .Where(vd => vd.DestLocationId == loc.LocationId)
                            .ToListAsync();
                        foreach (var vd in destVoucherDetails)
                        {
                            vd.DestLocationId = master.LocationId;
                        }

                        // 3. Update Items.DefaultLocationId
                        var items = await _db.Items
                            .Where(i => i.DefaultLocationId == loc.LocationId)
                            .ToListAsync();
                        foreach (var item in items)
                        {
                            item.DefaultLocationId = master.LocationId;
                        }

                        // 4. Deactivate old location
                        loc.IsActive = false;
                    }

                    // 5. Update master location code (remove bin suffix)
                    var newCode = $"{zone.ZoneCode}-{level.Key.PadLeft(2, '0')}";
                    output.AppendLine($"    → Đổi mã master: {master.LocationCode} → {newCode}");
                    master.LocationCode = newCode;
                    master.BinCode = "1";
                }
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            output.AppendLine("\n═══ HOÀN TẤT GOM Ô KHO ═══");
            output.AppendLine("Các ô cũ đã bị đánh dấu IsActive=false (không xóa hẳn để giữ lịch sử).");
            return Content(output.ToString(), "text/plain");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            output.AppendLine($"\n❌ LỖI: {ex.Message}");
            if (ex.InnerException != null) output.AppendLine($"Inner: {ex.InnerException.Message}");
            return Content(output.ToString(), "text/plain");
        }
    }

    /// <summary>
    /// Reset toàn bộ database, chỉ giữ ĐVT (UnitsOfMeasure + UnitConversions).
    /// Truy cập: /System/ResetDatabase
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetDatabase()
    {
        if (!IsDangerOpsAllowed()) return Forbid();

        var output = new System.Text.StringBuilder();
        output.AppendLine("═══ RESET DATABASE ═══\n");
        output.AppendLine("Giữ lại: UnitsOfMeasure, UnitConversions\n");

        try
        {
            // Disable FK checks and delete in order
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM [StockAlerts]");
            output.AppendLine("✓ Xóa StockAlerts");

            await _db.Database.ExecuteSqlRawAsync("DELETE FROM [VoucherDetails]");
            output.AppendLine("✓ Xóa VoucherDetails");

            await _db.Database.ExecuteSqlRawAsync("DELETE FROM [Vouchers]");
            output.AppendLine("✓ Xóa Vouchers");

            await _db.Database.ExecuteSqlRawAsync("DELETE FROM [ItemLocations]");
            output.AppendLine("✓ Xóa ItemLocations");

            await _db.Database.ExecuteSqlRawAsync("DELETE FROM [Locations]");
            output.AppendLine("✓ Xóa Locations");

            await _db.Database.ExecuteSqlRawAsync("DELETE FROM [Zones]");
            output.AppendLine("✓ Xóa Zones");

            await _db.Database.ExecuteSqlRawAsync("DELETE FROM [Warehouses]");
            output.AppendLine("✓ Xóa Warehouses");

            await _db.Database.ExecuteSqlRawAsync("UPDATE [Items] SET [DefaultLocationId] = NULL");
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM [Items]");
            output.AppendLine("✓ Xóa Items");

            await _db.Database.ExecuteSqlRawAsync("DELETE FROM [ItemCategories]");
            output.AppendLine("✓ Xóa ItemCategories");

            await _db.Database.ExecuteSqlRawAsync("DELETE FROM [Partners]");
            output.AppendLine("✓ Xóa Partners");

            await _db.Database.ExecuteSqlRawAsync("DELETE FROM [PackagingUnits]");
            output.AppendLine("✓ Xóa PackagingUnits");

            // Reseed identity columns
            var tables = new[] { "StockAlerts", "VoucherDetails", "Vouchers", "ItemLocations", "Locations", "Zones", "Warehouses", "Items", "ItemCategories", "Partners", "PackagingUnits" };
            foreach (var table in tables)
            {
                // Build command only from an internal whitelist of table names to avoid SQL injection risks.
                var safeTable = "[" + table.Replace("]", "]]") + "]";
                var cmd = $"DBCC CHECKIDENT({safeTable}, RESEED, 0)";
                try { await _db.Database.ExecuteSqlRawAsync(cmd); }
                catch { /* Table might not have identity */ }
            }
            output.AppendLine("\n✓ Reset identity columns");

            output.AppendLine("\n═══ RESET HOÀN TẤT ═══");
            output.AppendLine("Dữ liệu ĐVT (UnitsOfMeasure + UnitConversions) được giữ nguyên.");
            output.AppendLine("Bạn có thể tạo Kho, Vật tư, Phiếu mới.");
            return Content(output.ToString(), "text/plain");
        }
        catch (Exception ex)
        {
            output.AppendLine($"\n❌ LỖI: {ex.Message}");
            if (ex.InnerException != null) output.AppendLine($"Inner: {ex.InnerException.Message}");
            return Content(output.ToString(), "text/plain");
        }
    }
}
