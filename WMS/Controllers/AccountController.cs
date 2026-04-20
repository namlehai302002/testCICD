using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;
using WMS.ViewModels;

namespace WMS.Controllers;

[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public AccountController(AppDbContext db, IWebHostEnvironment env, IConfiguration config)
    {
        _db = db;
        _env = env;
        _config = config;
    }

    private static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return false;
        bool hasLower = password.Any(char.IsLower);
        bool hasUpper = password.Any(char.IsUpper);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));
        return hasLower && hasUpper && hasDigit && hasSymbol;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        // First-time setup: if there are no users yet, redirect to SetupAdmin
        if (!await _db.AppUsers.AnyAsync())
        {
            return RedirectToAction(nameof(SetupAdmin), new { returnUrl });
        }

        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!await _db.AppUsers.AnyAsync())
        {
            return RedirectToAction(nameof(SetupAdmin), new { returnUrl = model.ReturnUrl });
        }

        var user = await _db.AppUsers
            .Include(u => u.Role)
            .Include(u => u.Warehouse)
            .FirstOrDefaultAsync(u => u.UserName == model.UserName && u.IsActive);

        if (user == null)
        {
            model.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng!";
            return View(model);
        }

        bool isPasswordValid;
        try
        {
            isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
        }
        catch
        {
            // If password hash is corrupted/invalid format, treat as invalid (no plaintext fallback)
            isPasswordValid = false;
        }

        if (!isPasswordValid)
        {
            model.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng!";
            return View(model);
        }

        var roleName = user.Role?.RoleName ?? "Viewer";
        if (!string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase) && !user.WarehouseId.HasValue)
        {
            model.ErrorMessage = "Tài khoản chưa được gán kho làm việc. Vui lòng liên hệ quản trị viên.";
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new("FullName", user.FullName),
            new(ClaimTypes.Role, roleName),
        };

        if (user.WarehouseId.HasValue)
            claims.Add(new Claim("WarehouseId", user.WarehouseId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = model.RememberMe, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
            ? Redirect(model.ReturnUrl)
            : RedirectToAction("Index", "Home");
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> SetupAdmin(string? returnUrl = null)
    {
        if (await _db.AppUsers.AnyAsync())
            return RedirectToAction(nameof(Login), new { returnUrl });

        // Safety: allow first-admin bootstrap only in Development unless explicitly enabled
        if (!_env.IsDevelopment() && !string.Equals(_config["System:AllowFirstAdminBootstrap"], "true", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetupAdmin(string userName, string fullName, string? email, string password, string? returnUrl = null)
    {
        if (await _db.AppUsers.AnyAsync())
            return RedirectToAction(nameof(Login), new { returnUrl });

        if (!_env.IsDevelopment() && !string.Equals(_config["System:AllowFirstAdminBootstrap"], "true", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(fullName))
        {
            TempData["Error"] = "Vui lòng nhập đầy đủ tài khoản và họ tên.";
            return RedirectToAction(nameof(SetupAdmin), new { returnUrl });
        }

        if (!IsStrongPassword(password))
        {
            TempData["Error"] = "Mật khẩu yếu. Yêu cầu tối thiểu 8 ký tự gồm chữ hoa + chữ thường + số + ký tự đặc biệt.";
            return RedirectToAction(nameof(SetupAdmin), new { returnUrl });
        }

        // Ensure base roles exist
        if (!await _db.AppRoles.AnyAsync())
        {
            _db.AppRoles.AddRange(
                new AppRole { RoleId = 1, RoleName = "Admin", Description = "Quản trị hệ thống" },
                new AppRole { RoleId = 2, RoleName = "Manager", Description = "Quản lý kho" },
                new AppRole { RoleId = 3, RoleName = "Staff", Description = "Nhân viên kho" },
                new AppRole { RoleId = 4, RoleName = "Viewer", Description = "Chỉ xem" }
            );
            await _db.SaveChangesAsync();
        }

        // Create initial admin user
        var admin = new AppUser
        {
            UserName = userName.Trim(),
            FullName = fullName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RoleId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.AppUsers.Add(admin);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Tạo Admin lần đầu thành công. Vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login), new { returnUrl });
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (await _db.AppUsers.AnyAsync(u => u.UserName == model.UserName))
        {
            model.ErrorMessage = "Tên đăng nhập đã tồn tại!";
            return View(model);
        }

        if (!IsStrongPassword(model.Password))
        {
            model.ErrorMessage = "Mật khẩu yếu. Yêu cầu tối thiểu 8 ký tự gồm chữ hoa + chữ thường + số + ký tự đặc biệt.";
            return View(model);
        }

        var newUser = new AppUser
        {
            UserName = model.UserName?.Trim() ?? "",
            FullName = model.FullName?.Trim() ?? "",
            Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            RoleId = 4, // Default Role 4 = Viewer or lowest privilege
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.AppUsers.Add(newUser);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đăng ký thành công! Tài khoản đang chờ Admin kích hoạt.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult DevResetPassword()
    {
        if (!_env.IsDevelopment()) return NotFound();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DevResetPassword(string token, string userName, string newPassword)
    {
        if (!_env.IsDevelopment()) return NotFound();

        var expected = _config["DevResetToken"];
        if (string.IsNullOrWhiteSpace(expected) || token != expected)
        {
            TempData["Error"] = "Token không hợp lệ.";
            return RedirectToAction(nameof(DevResetPassword));
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            TempData["Error"] = "Thiếu tài khoản.";
            return RedirectToAction(nameof(DevResetPassword));
        }

        if (!IsStrongPassword(newPassword))
        {
            TempData["Error"] = "Mật khẩu yếu. Yêu cầu tối thiểu 8 ký tự gồm chữ hoa + chữ thường + số + ký tự đặc biệt.";
            return RedirectToAction(nameof(DevResetPassword));
        }

        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.UserName == userName && u.IsActive);
        if (user == null)
        {
            TempData["Error"] = "Không tìm thấy user (hoặc user đã bị khóa).";
            return RedirectToAction(nameof(DevResetPassword));
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã reset mật khẩu cho '{user.UserName}'.";
        return RedirectToAction(nameof(Login));
    }
}
