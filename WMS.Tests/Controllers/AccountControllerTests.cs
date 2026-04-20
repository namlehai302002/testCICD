using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using WMS.Controllers;
using WMS.Data;
using WMS.Models;
using WMS.ViewModels;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class AccountControllerTests
{
    // ================= DB =================
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)
            )
            .Options;

        return new AppDbContext(options);
    }

    // ================= CONTROLLER =================
    private AccountController GetController(AppDbContext db)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns("Development");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"System:AllowFirstAdminBootstrap", "true"},
                {"DevResetToken", "123"}
            }).Build();

        var controller = new AccountController(db, env.Object, config);

        // Setup HttpContext
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Setup TempData
        controller.TempData = new TempDataDictionary(
            controller.ControllerContext.HttpContext,
            Mock.Of<ITempDataProvider>()
        );

        return controller;
    }

    // ================= LOGIN =================

    [Fact]
    public async Task Login_NoUser_RedirectToSetupAdmin()
    {
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.Login((string?)null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("SetupAdmin", redirect.ActionName);
    }

    [Fact]
    public async Task Login_InvalidUser_ReturnViewWithError()
    {
        var db = GetDbContext();

        db.AppUsers.Add(new AppUser
        {
            UserName = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456Aa@"),
            IsActive = true
        });
        db.SaveChanges();

        var controller = GetController(db);

        var result = await controller.Login(new LoginViewModel
        {
            UserName = "wrong11",
            Password = "123"
        });

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LoginViewModel>(view.Model);

        Assert.NotNull(model.ErrorMessage);
    }

    // ================= REGISTER =================

    [Fact]
    public async Task Register_UserExists_ReturnError()
    {
        var db = GetDbContext();

        db.AppUsers.Add(new AppUser
        {
            UserName = "test"
        });
        db.SaveChanges();

        var controller = GetController(db);

        var result = await controller.Register(new RegisterViewModel
        {
            UserName = "test",
            Password = "Aa@123456"
        });

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<RegisterViewModel>(view.Model);

        Assert.NotNull(model.ErrorMessage);
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnError()
    {
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.Register(new RegisterViewModel
        {
            UserName = "user1",
            Password = "123"
        });

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<RegisterViewModel>(view.Model);

        Assert.NotNull(model.ErrorMessage);
    }

    [Fact]
    public async Task Register_ValidUser_RedirectToLogin()
    {
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.Register(new RegisterViewModel
        {
            UserName = "user1",
            Password = "Aa@123456",
            FullName = "Test User"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);

        Assert.Single(db.AppUsers);
    }

    // ================= SETUP ADMIN =================

    [Fact]
    public async Task SetupAdmin_CreateFirstAdmin_Success()
    {
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.SetupAdmin(
            "admin",
            "Admin",
            "admin@test.com",
            "Aa@123456"
        );

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);

        Assert.Single(db.AppUsers);
    }

[Fact]
public void Login_SampleData_ShouldFail()
{
    var correctUser = "hieu2";
    var correctPass = "Wms@1234!";

    var inputUser = "hieu2";
    var inputPass = "Wms@123433!";

    bool isMatch =
        inputUser == correctUser &&
        inputPass == correctPass;

    // ép fail khi KHÔNG khớp
    Assert.True(isMatch, "Data mismatch → TEST FAIL");
}
}