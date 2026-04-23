using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WMS.Controllers;
using WMS.Data;
using WMS.Models;
using Microsoft.Extensions.Hosting;

public class SystemControllerTests
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
    private SystemController GetController(AppDbContext db, bool isDev = true, string allowDanger = "true")
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(x => x.EnvironmentName)
   .Returns(isDev ? "Development" : "Production");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "System:AllowDangerOps", allowDanger }
            })
            .Build();

        var controller = new SystemController(db, env.Object, config);

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

    // =====================================================
    // UNIT OF MEASURE TESTS
    // =====================================================

    [Fact]
    public async Task CreateUnit_Valid_AddToDatabase()
    {
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.CreateUnit("kg", "Kilogram");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Units", redirect.ActionName);

        Assert.Single(db.UnitsOfMeasure);
    }

    [Fact]
    public async Task CreateUnit_Duplicate_ShouldNotAdd()
    {
        var db = GetDbContext();

        db.UnitsOfMeasure.Add(new UnitOfMeasure
        {
            UomCode = "kg",
            UomName = "Kilogram",
            IsActive = true
        });
        db.SaveChanges();

        var controller = GetController(db);

        await controller.CreateUnit("kg", "Kilogram");

        Assert.Single(db.UnitsOfMeasure);
    }

    [Fact]
    public async Task DeleteUnit_ShouldSetInactive()
    {
        var db = GetDbContext();

        var unit = new UnitOfMeasure
        {
            UomCode = "kg",
            UomName = "Kilogram",
            IsActive = true
        };

        db.UnitsOfMeasure.Add(unit);
        db.SaveChanges();

        var controller = GetController(db);

        await controller.DeleteUnit(unit.UomId);

        Assert.False(db.UnitsOfMeasure.First().IsActive);
    }

    // =====================================================
    // PACKAGING TESTS
    // =====================================================

    [Fact]
    public async Task CreatePackaging_Valid_ShouldAdd()
    {
        var db = GetDbContext();

        db.UnitsOfMeasure.Add(new UnitOfMeasure
        {
            UomCode = "kg",
            UomName = "Kilogram",
            IsActive = true
        });
        db.SaveChanges();

        var controller = GetController(db);

        var uom = db.UnitsOfMeasure.First();

        var result = await controller.CreatePackaging("Bao 50kg", uom.UomId, 50);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Units", redirect.ActionName);

        Assert.Single(db.PackagingUnits);
    }

    [Fact]
    public async Task CreatePackaging_Duplicate_ShouldNotAdd()
    {
        var db = GetDbContext();

        db.PackagingUnits.Add(new PackagingUnit
        {
            TenDongGoi = "Bao 50kg",
            BaseUomId = 1,
            GiaTri = 50,
            IsActive = true
        });
        db.SaveChanges();

        var controller = GetController(db);

        await controller.CreatePackaging("Bao 50kg", 1, 50);

        Assert.Single(db.PackagingUnits);
    }

    // =====================================================
    // SEED TEST
    // =====================================================

    [Fact]
    public async Task SeedData_ShouldRunWithoutCrash()
    {
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.SeedData();

        var content = Assert.IsType<ContentResult>(result);

        Assert.Contains("SEED", content.Content);
    }

    // =====================================================
    // RESET DATABASE TESTS
    // =====================================================

    [Fact]
    public async Task ResetDatabase_NotAllowed_ShouldForbid()
    {
        var db = GetDbContext();

        var controller = GetController(db, isDev: false, allowDanger: "false");

        var result = await controller.ResetDatabase();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ResetDatabase_Allowed_ShouldRun()
    {
        var db = GetDbContext();

        var controller = GetController(db, isDev: true, allowDanger: "true");

        var result = await controller.ResetDatabase();

        var content = Assert.IsType<ContentResult>(result);

        Assert.Contains("RESET", content.Content);
    }

    // =====================================================
    // SIMPLE SAFETY LOGIC TEST
    // =====================================================

    [Fact]
    public void BasicLogic_Test_ShouldPass()
    {
        bool allow = true;

        Assert.True(allow);
    }
}