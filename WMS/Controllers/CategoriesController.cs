using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Controllers;

public class CategoriesController : Controller
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var cats = await _db.ItemCategories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
        return View(cats);
    }

    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create() => View(new ItemCategory());

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ItemCategory cat)
    {
        if (await _db.ItemCategories.AnyAsync(c => c.CategoryCode == cat.CategoryCode))
        {
            TempData["Error"] = $"Mã danh mục '{cat.CategoryCode}' đã tồn tại! Vui lòng chọn mã khác.";
            return View(cat);
        }
        
        cat.CreatedAt = DateTime.UtcNow;
        _db.ItemCategories.Add(cat);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm danh mục '{cat.CategoryName}'!";
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int id)
    {
        var cat = await _db.ItemCategories.FindAsync(id);
        return cat == null ? NotFound() : View(cat);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ItemCategory cat)
    {
        var existing = await _db.ItemCategories.FindAsync(id);
        if (existing == null) return NotFound();
        
        if (await _db.ItemCategories.AnyAsync(c => c.CategoryCode == cat.CategoryCode && c.CategoryId != id))
        {
            TempData["Error"] = $"Mã danh mục '{cat.CategoryCode}' đã tồn tại! Vui lòng chọn mã khác.";
            return View(cat);
        }
        existing.CategoryCode = cat.CategoryCode;
        existing.CategoryName = cat.CategoryName;
        existing.SortOrder = cat.SortOrder;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật danh mục!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.ItemCategories.FindAsync(id);
        if (cat == null) return NotFound();
        cat.IsActive = false;
        cat.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa danh mục '{cat.CategoryName}'!";
        return RedirectToAction("Index");
    }
}

public class UnitsController : Controller
{
    private readonly AppDbContext _db;
    public UnitsController(AppDbContext db) => _db = db;


    public async Task<IActionResult> Index()
    {
        var uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).OrderBy(u => u.UomCode).ToListAsync();
        ViewBag.Conversions = await _db.UnitConversions
            .Include(c => c.FromUom).Include(c => c.ToUom).Include(c => c.Item)
            .Where(c => c.IsActive).ToListAsync();
        ViewBag.PackagingUnits = await _db.PackagingUnits
            .Include(p => p.BaseUom)
            .Where(p => p.IsActive).OrderBy(p => p.TenDongGoi).ToListAsync();
        ViewBag.AllUoms = uoms;
        return View(uoms);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string uomCode, string uomName)
    {
        if (await _db.UnitsOfMeasure.AnyAsync(u => u.UomCode == uomCode))
        {
            TempData["Error"] = $"Mã đơn vị '{uomCode}' đã tồn tại! Vui lòng chọn mã khác.";
            return RedirectToAction("Index");
        }
        
        _db.UnitsOfMeasure.Add(new UnitOfMeasure { UomCode = uomCode, UomName = uomName });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm đơn vị '{uomName}'!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var uom = await _db.UnitsOfMeasure.FindAsync(id);
        if (uom == null) return NotFound();
        uom.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa đơn vị '{uom.UomName}'!";
        return RedirectToAction("Index");
    }

    // === ĐÓNG GÓI (Packaging) ===
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
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
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePackaging(int id)
    {
        var pkg = await _db.PackagingUnits.FindAsync(id);
        if (pkg != null)
        {
            pkg.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa đóng gói '{pkg.TenDongGoi}'!";
        }
        return RedirectToAction("Index");
    }
}
