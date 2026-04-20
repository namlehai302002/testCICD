using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Controllers;

public class PartnersController : Controller
{
    private readonly AppDbContext _db;
    public PartnersController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(byte? type, string? search)
    {
        var query = _db.Partners.Where(p => p.IsActive).AsQueryable();
        if (type.HasValue)
            query = query.Where(p => p.PartnerType == type.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.PartnerName.Contains(search) || p.PartnerCode.Contains(search));

        ViewBag.Type = type;
        ViewBag.Search = search;
        return View(await query.OrderBy(p => p.PartnerCode).ToListAsync());
    }

    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create() => View(new Partner());

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Partner partner)
    {
        partner.CreatedAt = DateTime.UtcNow;
        _db.Partners.Add(partner);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm đối tác '{partner.PartnerName}'!";
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.Partners.FindAsync(id);
        return p == null ? NotFound() : View(p);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Partner partner)
    {
        var existing = await _db.Partners.FindAsync(id);
        if (existing == null) return NotFound();
        existing.PartnerCode = partner.PartnerCode;
        existing.PartnerName = partner.PartnerName;
        existing.PartnerType = partner.PartnerType;
        existing.TaxCode = partner.TaxCode;
        existing.Phone = partner.Phone;
        existing.Email = partner.Email;
        existing.Address = partner.Address;
        existing.ContactPerson = partner.ContactPerson;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật đối tác!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Partners.FindAsync(id);
        if (p == null) return NotFound();
        p.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa đối tác '{p.PartnerName}'!";
        return RedirectToAction("Index");
    }
}
