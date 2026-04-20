using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;
using WMS.ViewModels;

using Microsoft.AspNetCore.Authorization;

namespace WMS.Controllers;

public class ItemsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ItemsController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index(string? search, int? categoryId, byte? itemType, string? stockStatus)
    {
        var query = _db.Items.Include(i => i.Category).Include(i => i.BaseUom)
            .Where(i => i.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i => i.ItemCode.Contains(search) || i.ItemName.Contains(search) || (i.Barcode != null && i.Barcode.Contains(search)));
        if (categoryId.HasValue)
            query = query.Where(i => i.CategoryId == categoryId.Value);
        if (itemType.HasValue)
            query = query.Where(i => i.ItemType == itemType.Value);

        var items = await query.OrderBy(i => i.ItemCode).ToListAsync();

        if (stockStatus == "low")
            items = items.Where(i => i.StockStatus == "Sắp Hết" || i.StockStatus == "Hết Hàng").ToList();
        else if (stockStatus == "out")
            items = items.Where(i => i.StockStatus == "Hết Hàng").ToList();
        else if (stockStatus == "over")
            items = items.Where(i => i.StockStatus == "Vượt Định Mức").ToList();

        ViewBag.Categories = await _db.ItemCategories.Where(c => c.IsActive).ToListAsync();
        ViewBag.Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).OrderBy(u => u.UomCode).ToListAsync();
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.ItemType = itemType;
        ViewBag.StockStatus = stockStatus;

        return View(items);
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create()
    {
        // Default ItemType = 1 (NVL)
        var count = await _db.Items.Where(i => i.ItemType == 1).CountAsync();
        var newCode = $"NVL-{(count + 1):D3}";

        var occupied = _db.ItemLocations.Where(il => il.Quantity > 0).Select(il => il.LocationId)
            .Union(_db.Items.Where(i => i.IsActive && i.DefaultLocationId.HasValue).Select(i => i.DefaultLocationId!.Value));

        var vm = new ItemFormViewModel
        {
            Item = new Item { ItemCode = newCode, ItemType = 1 },
            Categories = await _db.ItemCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync(),
            Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).OrderBy(u => u.UomCode).ToListAsync(),
            Locations = await _db.Locations.Where(l => l.IsActive && !occupied.Contains(l.LocationId)).OrderBy(l => l.LocationCode).ToListAsync()
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> GenerateItemCode(byte itemType)
    {
        string prefix = itemType switch
        {
            1 => "NVL",
            2 => "TP",
            3 => "BTP",
            4 => "BB",
            5 => "VTTH",
            _ => "VT"
        };
        var count = await _db.Items.Where(i => i.ItemType == itemType).CountAsync();
        var newCode = $"{prefix}-{(count + 1):D3}";
        return Json(new { code = newCode });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ItemFormViewModel vm, IFormFile? ImageFile)
    {
        var item = vm.Item;
        if (string.IsNullOrWhiteSpace(item.Barcode))
        {
            item.Barcode = item.ItemCode;
        }
        
        // Handle image upload
        if (ImageFile != null && ImageFile.Length > 0)
        {
            item.ImageUrl = await SaveImageFile(ImageFile);
        }
        
        item.CreatedAt = DateTime.UtcNow;
        item.CreatedBy = User.Identity?.Name ?? "system";
        _db.Items.Add(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm vật tư '{item.ItemName}' thành công!";
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Items.FindAsync(id);
        if (item == null) return NotFound();

        var occupied = _db.ItemLocations.Where(il => il.Quantity > 0 && il.ItemId != id).Select(il => il.LocationId)
            .Union(_db.Items.Where(i => i.IsActive && i.DefaultLocationId.HasValue && i.ItemId != id).Select(i => i.DefaultLocationId!.Value));

        var vm = new ItemFormViewModel
        {
            Item = item,
            Categories = await _db.ItemCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync(),
            Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).OrderBy(u => u.UomCode).ToListAsync(),
            Locations = await _db.Locations.Where(l => l.IsActive && !occupied.Contains(l.LocationId)).OrderBy(l => l.LocationCode).ToListAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ItemFormViewModel vm, IFormFile? ImageFile)
    {
        var existing = await _db.Items.FindAsync(id);
        if (existing == null) return NotFound();

        existing.ItemCode = vm.Item.ItemCode;
        existing.ItemName = vm.Item.ItemName;
        existing.Barcode = string.IsNullOrWhiteSpace(vm.Item.Barcode) ? vm.Item.ItemCode : vm.Item.Barcode;
        existing.SkuCode = vm.Item.SkuCode;
        existing.CategoryId = vm.Item.CategoryId;
        existing.ItemType = vm.Item.ItemType;
        existing.BaseUomId = vm.Item.BaseUomId;
        existing.Weight = vm.Item.Weight;
        existing.ReorderPoint = vm.Item.ReorderPoint;
        existing.Description = vm.Item.Description;
        existing.Specifications = vm.Item.Specifications;
        existing.MinThreshold = vm.Item.MinThreshold;
        existing.MaxThreshold = vm.Item.MaxThreshold;
        existing.DefaultLocationId = vm.Item.DefaultLocationId;
        existing.UpdatedAt = DateTime.UtcNow;

        // Handle image upload
        if (ImageFile != null && ImageFile.Length > 0)
        {
            existing.ImageUrl = await SaveImageFile(ImageFile);
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật vật tư '{existing.ItemName}'!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Items.FindAsync(id);
        if (item == null) return NotFound();
        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa vật tư '{item.ItemName}'!";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _db.Items
            .Include(i => i.Category).Include(i => i.BaseUom)
            .FirstOrDefaultAsync(i => i.ItemId == id);
        if (item == null) return NotFound();

        ViewBag.Locations = await _db.ItemLocations
            .Include(il => il.Location).ThenInclude(l => l!.Zone).ThenInclude(z => z!.Warehouse)
            .Where(il => il.ItemId == id && il.Quantity > 0)
            .ToListAsync();

        ViewBag.RecentVouchers = await _db.VoucherDetails
            .Include(vd => vd.Voucher).ThenInclude(v => v!.Warehouse)
            .Where(vd => vd.ItemId == id)
            .OrderByDescending(vd => vd.Voucher!.VoucherDate)
            .Take(20).ToListAsync();

        return View(item);
    }

    [HttpGet]
    public async Task<IActionResult> GetItemJson(int id)
    {
        var item = await _db.Items.Include(i => i.BaseUom).FirstOrDefaultAsync(i => i.ItemId == id);
        if (item == null) return NotFound();
        return Json(new { item.ItemId, item.ItemCode, item.ItemName, item.UnitCost, item.Weight, BaseUom = item.BaseUom?.UomCode, BaseUomId = item.BaseUomId });
    }

    [HttpGet]
    public async Task<IActionResult> GetItemByBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return BadRequest("Mã vạch trống.");

        var code = barcode.Trim();

        // Search by Barcode first (exact match), then ItemCode, then SkuCode
        var item = await _db.Items.Include(i => i.BaseUom)
            .FirstOrDefaultAsync(i => i.IsActive && (
                i.Barcode == code ||
                i.ItemCode == code ||
                i.SkuCode == code
            ));

        // Fallback: case-insensitive search if exact match failed
        if (item == null)
        {
            item = await _db.Items.Include(i => i.BaseUom)
                .FirstOrDefaultAsync(i => i.IsActive && (
                    (i.Barcode != null && i.Barcode.ToLower() == code.ToLower()) ||
                    i.ItemCode.ToLower() == code.ToLower() ||
                    (i.SkuCode != null && i.SkuCode.ToLower() == code.ToLower())
                ));
        }

        if (item == null)
            return NotFound(new { message = $"Không tìm thấy vật tư với mã: {code}" });

        return Json(new { item.ItemId, item.ItemCode, item.ItemName, item.UnitCost, BaseUom = item.BaseUom?.UomId, item.Barcode });
    }

    private async Task<string> SaveImageFile(IFormFile file)
    {
        try
        {
            // Validate file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            
            if (!allowedExtensions.Contains(extension))
                throw new InvalidOperationException("Định dạng file không được hỗ trợ");

            if (file.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException("Kích thước file vượt quá 5MB");

            // Create uploads directory if not exists
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "items");
            Directory.CreateDirectory(uploadsDir);

            // Generate unique filename
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative URL
            return $"/uploads/items/{fileName}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Image upload error: {ex.Message}");
            throw;
        }
    }
}

