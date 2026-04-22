using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using WMS.Controllers;
using WMS.Data;
using WMS.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

public class UsersControllerTests
{
    // ================= DB =================
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    // ================= CONTROLLER =================
    private UsersController GetController(AppDbContext db)
    {
        var controller = new UsersController(db);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        controller.TempData = new TempDataDictionary(
            controller.ControllerContext.HttpContext,
            Mock.Of<ITempDataProvider>()
        );

        return controller;
    }

    // ================= SEED DATA =================
    private void SeedRole(AppDbContext db)
    {
        db.AppRoles.Add(new AppRole
        {
            RoleId = 1,
            RoleName = "Admin"
        });

        db.AppRoles.Add(new AppRole
        {
            RoleId = 2,
            RoleName = "Manager"
        });

        db.AppRoles.Add(new AppRole
        {
            RoleId = 3,
            RoleName = "Staff"
        });

        db.SaveChanges();
    }

    // =====================================================
    // ===================== CREATE USER ===================
    // =====================================================

    [Fact]
    public async Task CreateUser_Valid_ShouldSuccess()
    {
        // TEST: tạo user hợp lệ → lưu DB thành công

        var db = GetDbContext();
        SeedRole(db);

        var controller = GetController(db);

        var result = await controller.Create(
            "admin1",
            "Admin User",
            "admin@test.com",
            "Aa@123456",
            1,
            null
        );

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Single(db.AppUsers);
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_ShouldFail()
    {
        // TEST: trùng username → không tạo user mới

        var db = GetDbContext();
        SeedRole(db);

        db.AppUsers.Add(new AppUser
        {
            UserName = "admin1",
            FullName = "Old",
            PasswordHash = "hash",
            RoleId = 1
        });
        db.SaveChanges();

        var controller = GetController(db);

        var result = await controller.Create(
            "admin1",
            "New User",
            "test@test.com",
            "Aa@123456",
            1,
            null
        );

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Single(db.AppUsers);
    }

    [Fact]
    public async Task CreateUser_InvalidRole_ShouldFail()
    {
        // TEST: roleId không tồn tại → báo lỗi

        var db = GetDbContext();
        SeedRole(db);

        var controller = GetController(db);

        var result = await controller.Create(
            "user1",
            "User",
            "user@test.com",
            "Aa@123456",
            999,
            null
        );

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Empty(db.AppUsers);
    }

    [Fact]
    public async Task CreateUser_NonAdminWithoutWarehouse_ShouldFail()
    {
        // TEST: Staff/Manager bắt buộc phải có WarehouseId

        var db = GetDbContext();
        SeedRole(db);

        var controller = GetController(db);

        var result = await controller.Create(
            "user1",
            "User",
            "user@test.com",
            "Aa@123456",
            3, // Staff
            null
        );

        Assert.Empty(db.AppUsers);
    }

    [Fact]
    public async Task CreateUser_WeakPassword_ShouldFail()
    {
        // TEST: password yếu → reject

        var db = GetDbContext();
        SeedRole(db);

        var controller = GetController(db);

        var result = await controller.Create(
            "user1",
            "User",
            "user@test.com",
            "123", // weak
            1,
            null
        );

        Assert.Empty(db.AppUsers);
    }

    // =====================================================
    // ================= RESET PASSWORD ====================
    // =====================================================

    [Fact]
    public async Task ResetPassword_Valid_ShouldSuccess()
    {
        // TEST: đổi password hợp lệ

        var db = GetDbContext();
        SeedRole(db);

        var user = new AppUser
        {
            UserName = "user1",
            FullName = "Test",
            PasswordHash = "oldhash",
            RoleId = 1
        };

        db.AppUsers.Add(user);
        db.SaveChanges();

        var controller = GetController(db);

        var result = await controller.ResetPassword(user.UserId, "Aa@123456");

        var updated = await db.AppUsers.FirstAsync();

        Assert.NotEqual("oldhash", updated.PasswordHash);
    }

    [Fact]
    public async Task ResetPassword_WeakPassword_ShouldFail()
    {
        // TEST: password yếu → không update

        var db = GetDbContext();
        SeedRole(db);

        var user = new AppUser
        {
            UserName = "user1",
            FullName = "Test",
            PasswordHash = "oldhash",
            RoleId = 1
        };

        db.AppUsers.Add(user);
        db.SaveChanges();

        var controller = GetController(db);

        var result = await controller.ResetPassword(user.UserId, "123");

        var updated = await db.AppUsers.FirstAsync();

        Assert.Equal("oldhash", updated.PasswordHash);
    }

    // =====================================================
    // ===================== DELETE USER ===================
    // =====================================================

    [Fact]
    public async Task DeleteUser_ShouldSetInactive()
    {
        // TEST: soft delete user

        var db = GetDbContext();
        SeedRole(db);

        var user = new AppUser
        {
            UserName = "user1",
            FullName = "Test",
            PasswordHash = "hash",
            RoleId = 1,
            IsActive = true
        };

        db.AppUsers.Add(user);
        db.SaveChanges();

        var controller = GetController(db);

        var result = await controller.Delete(user.UserId);

        var updated = await db.AppUsers.FirstAsync();

        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task DeleteUser_NotFound_ShouldReturnNotFound()
    {
        // TEST: id không tồn tại

        var db = GetDbContext();
        SeedRole(db);

        var controller = GetController(db);

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }
}