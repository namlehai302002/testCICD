using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using WMS.Controllers;
using WMS.Data;
using WMS.Models;

public class WarehousesControllerTests
{
    // ================= DB =================
    private AppDbContext GetDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    // ================= CONTROLLER =================
    private WarehousesController GetController(AppDbContext db)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(x => x.EnvironmentName).Returns("Development");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
            { "System:AllowDangerOps", "true" }
            })
            .Build();

        var controller = new WarehousesController(db, config, env.Object);

        // ================= FIX AUTH =================
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
        identity.AddClaim(new Claim("WarehouseId", "1"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        controller.TempData = new TempDataDictionary(
            httpContext,
            Mock.Of<ITempDataProvider>()
        );

        return controller;
    }

    // =====================================================
    // INDEX
    // =====================================================
    [Fact]
    public async Task Index_ReturnView_WithData()
    {
        var db = GetDb();

        db.Warehouses.Add(new Warehouse
        {
            WarehouseId = 1,
            WarehouseCode = "WH01",
            WarehouseName = "Kho A",
            IsActive = true
        });
        db.SaveChanges();

        var controller = GetController(db);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Warehouse>>(view.Model);

        Assert.Single(model);
    }

    // =====================================================
    // CREATE
    // =====================================================
    [Fact]
    public async Task Create_ValidWarehouse_ShouldAdd()
    {
        var db = GetDb();
        var controller = GetController(db);

        var wh = new Warehouse
        {
            WarehouseCode = "WH01",
            WarehouseName = "Kho Test",
            IsActive = true
        };

        var result = await controller.Create(wh);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        Assert.Single(db.Warehouses);
    }

    [Fact]
    public async Task Create_DuplicateCode_ShouldFail()
    {
        var db = GetDb();

        db.Warehouses.Add(new Warehouse
        {
            WarehouseCode = "WH01",
            WarehouseName = "Kho A",
            IsActive = true
        });
        db.SaveChanges();

        var controller = GetController(db);

        var wh = new Warehouse
        {
            WarehouseCode = "WH01",
            WarehouseName = "Kho B",
            IsActive = true
        };

        var result = await controller.Create(wh);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<Warehouse>(view.Model);

        Assert.Single(db.Warehouses);
    }

    // =====================================================
    // EDIT
    // =====================================================
    [Fact]
    public async Task Edit_Valid_ShouldUpdate()
    {
        var db = GetDb();

        var wh = new Warehouse
        {
            WarehouseId = 1,
            WarehouseCode = "WH01",
            WarehouseName = "Old",
            IsActive = true
        };

        db.Warehouses.Add(wh);
        db.SaveChanges();

        var controller = GetController(db);

        var update = new Warehouse
        {
            WarehouseCode = "WH01",
            WarehouseName = "New",
            IsActive = true
        };

        var result = await controller.Edit(wh.WarehouseId, update);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        Assert.Equal("New", db.Warehouses.First().WarehouseName);
    }

    // =====================================================
    // DELETE
    // =====================================================
    [Fact]
    public async Task Delete_Valid_ShouldDeactivate()
    {
        var db = GetDb();

        var wh = new Warehouse
        {
            WarehouseId = 1,
            WarehouseCode = "WH01",
            WarehouseName = "Kho",
            IsActive = true
        };

        db.Warehouses.Add(wh);
        db.SaveChanges();

        var controller = GetController(db);

        var result = await controller.Delete(wh.WarehouseId);

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.False(db.Warehouses.First().IsActive);
    }

    // =====================================================
    // CREATE ZONE
    // =====================================================
    [Fact]
    public async Task CreateZone_ShouldAdd()
    {
        var db = GetDb();

        var wh = new Warehouse
        {
            WarehouseId = 1,
            WarehouseCode = "WH01",
            WarehouseName = "Kho",
            IsActive = true
        };

        db.Warehouses.Add(wh);
        db.SaveChanges();

        var controller = GetController(db);

        var result = await controller.CreateZone(
            wh.WarehouseId,
            "Z01",
            "Zone A",
            1
        );

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Single(db.Zones);
    }

    // =====================================================
    // CREATE LOCATION
    // =====================================================
    [Fact]
    public async Task CreateLocation_ShouldAdd()
    {
        var db = GetDb();

        var wh = new Warehouse
        {
            WarehouseId = 1,
            WarehouseCode = "WH01",
            WarehouseName = "Kho",
            IsActive = true
        };
        db.Warehouses.Add(wh);

        var zone = new Zone
        {
            ZoneId = 1,
            WarehouseId = wh.WarehouseId,
            ZoneCode = "Z01",
            ZoneName = "Zone",
            ZoneType = 1
        };
        db.Zones.Add(zone);

        db.SaveChanges();

        var controller = GetController(db);

        var result = await controller.CreateLocation(
            zone.ZoneId,
            "LOC01",
            "R1",
            "S1",
            "B1"
        );

        Assert.Single(db.Locations);
    }

    // =====================================================
    // GET STOCK
    // =====================================================
    [Fact]
    public async Task GetLocationStock_ShouldReturnJson()
    {
        var db = GetDb();

        db.Locations.Add(new Location
        {
            LocationId = 1,
            LocationCode = "LOC01",
            IsActive = true
        });
        db.SaveChanges();

        var controller = GetController(db);

        var result = await controller.GetLocationStock(1);

        Assert.IsType<JsonResult>(result);
    }

    // =====================================================
    // FIX DATA
    // =====================================================
    [Fact]
    public async Task FixData_ShouldRun()
    {
        var db = GetDb();

        var controller = GetController(db);

        var result = await controller.FixData();

        var content = Assert.IsType<ContentResult>(result);

        Assert.Contains("Fixed", content.Content);
    }
}