using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    private static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return false;
        bool hasLower = password.Any(char.IsLower);
        bool hasUpper = password.Any(char.IsUpper);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));
        return hasLower && hasUpper && hasDigit && hasSymbol;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _db.AppUsers.Include(u => u.Role).Include(u => u.Warehouse)
            .Where(u => u.IsActive).OrderBy(u => u.UserName).ToListAsync();
        ViewBag.Roles = await _db.AppRoles.ToListAsync();
        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string userName, string fullName, string email, string password, int roleId, int? warehouseId)
    {
        if (await _db.AppUsers.AnyAsync(u => u.UserName == userName))
        {
            TempData["Error"] = "Tên đăng nhập đã tồn tại!";
            return RedirectToAction("Index");
        }

        var role = await _db.AppRoles.FirstOrDefaultAsync(r => r.RoleId == roleId);
        if (role == null)
        {
            TempData["Error"] = "Vai trò không hợp lệ.";
            return RedirectToAction("Index");
        }

        if (!string.Equals(role.RoleName, "Admin", StringComparison.OrdinalIgnoreCase) && !warehouseId.HasValue)
        {
            TempData["Error"] = "Người dùng không phải Admin bắt buộc phải gán kho.";
            return RedirectToAction("Index");
        }

        if (!IsStrongPassword(password))
        {
            TempData["Error"] = "Mật khẩu yếu. Yêu cầu tối thiểu 8 ký tự gồm chữ hoa + chữ thường + số + ký tự đặc biệt.";
            return RedirectToAction("Index");
        }

        _db.AppUsers.Add(new AppUser
        {
            UserName = userName,
            FullName = fullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RoleId = roleId,
            WarehouseId = warehouseId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã tạo tài khoản '{userName}'!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, string newPassword)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user == null) return NotFound();
        if (!IsStrongPassword(newPassword))
        {
            TempData["Error"] = "Mật khẩu yếu. Yêu cầu tối thiểu 8 ký tự gồm chữ hoa + chữ thường + số + ký tự đặc biệt.";
            return RedirectToAction("Index");
        }
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã đổi mật khẩu cho '{user.UserName}'!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user == null) return NotFound();
        user.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa tài khoản '{user.UserName}'!";
        return RedirectToAction("Index");
    }
}
