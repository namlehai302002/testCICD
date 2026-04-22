using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using WMS.Controllers;
using WMS.Data;
using WMS.Models;
using WMS.ViewModels;

public class HomeControllerTests
{
    // ================= DB MOCK =================
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);

        // ===== Items (stable data only) =====
        db.Items.AddRange(
            new Item
            {
                ItemId = 1,
                ItemCode = "A",
                IsActive = true,
                CurrentStock = 0,
                MinThreshold = 5,
                TotalStockValue = 100
            },
            new Item
            {
                ItemId = 2,
                ItemCode = "B",
                IsActive = true,
                CurrentStock = 3,
                MinThreshold = 5,
                TotalStockValue = 200
            }
        );

        db.Warehouses.Add(new Warehouse { WarehouseId = 1, IsActive = true });
        db.Partners.Add(new Partner { PartnerId = 1, IsActive = true });

        db.Vouchers.Add(new Voucher
        {
            VoucherId = 1,
            VoucherDate = DateTime.UtcNow.Date,
            IsCancelled = false,
            VoucherType = 1,
            CreatedAt = DateTime.UtcNow,
            WarehouseId = 1,
            PartnerId = 1
        });

        db.StockAlerts.Add(new StockAlert
        {
            AlertId = 1,
            IsResolved = false,
            CreatedAt = DateTime.UtcNow,
            ItemId = 1
        });

        db.Waves.Add(new Wave { WaveId = 1, Status = 2 });
        db.PickTasks.Add(new PickTask { PickTaskId = 1, Status = 1 });

        db.StockReservations.Add(new StockReservation
        {
            StockReservationId = 1,
            Status = 1,
            ReservedQty = 10,
            ConsumedQty = 5
        });

        db.SaveChanges();
        return db;
    }

    // ================= CONTROLLER =================
    private HomeController GetController(AppDbContext db)
    {
        var controller = new HomeController(db);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "testuser")
        }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }

    // ================= TESTS (SAFE ONLY) =================

    [Fact]
    public async Task TC01_Return_View()
    {
        // Test: controller trả về View dashboard
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.Index();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task TC02_Total_Items()
    {
        // Test: tổng số item active
        var db = GetDbContext();
        var controller = GetController(db);

        var vm = ((await controller.Index()) as ViewResult)!.Model as DashboardViewModel;

        Assert.Equal(2, vm!.TotalItems);
    }

    [Fact]
    public async Task TC03_Total_Warehouses()
    {
        // Test: số warehouse active
        var db = GetDbContext();
        var controller = GetController(db);

        var vm = ((await controller.Index()) as ViewResult)!.Model as DashboardViewModel;

        Assert.Equal(1, vm!.TotalWarehouses);
    }

    [Fact]
    public async Task TC04_Total_Partners()
    {
        // Test: số partner active
        var db = GetDbContext();
        var controller = GetController(db);

        var vm = ((await controller.Index()) as ViewResult)!.Model as DashboardViewModel;

        Assert.Equal(1, vm!.TotalPartners);
    }

    [Fact]
    public async Task TC05_Reservation_FillRate()
    {
        // Test: % consumption reservation
        var db = GetDbContext();
        var controller = GetController(db);

        var vm = ((await controller.Index()) as ViewResult)!.Model as DashboardViewModel;

        Assert.Equal(50m, vm!.ReservationFillRate);
    }

    [Fact]
    public async Task TC06_Recent_Vouchers_NotEmpty()
    {
        // Test: voucher gần đây không rỗng
        var db = GetDbContext();
        var controller = GetController(db);

        var vm = ((await controller.Index()) as ViewResult)!.Model as DashboardViewModel;

        Assert.NotEmpty(vm!.RecentVouchers);
    }

    [Fact]
    public async Task TC07_Unresolved_Alerts_NotEmpty()
    {
        // Test: alert chưa xử lý không rỗng
        var db = GetDbContext();
        var controller = GetController(db);

        var vm = ((await controller.Index()) as ViewResult)!.Model as DashboardViewModel;

        Assert.NotEmpty(vm!.UnresolvedAlerts);
    }

    [Fact]
    public async Task TC08_View_Returns_Dashboard()
    {
        // Test: không crash + trả View
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.Index();

        Assert.IsType<ViewResult>(result);
    }
}