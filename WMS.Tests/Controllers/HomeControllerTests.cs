using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Controllers;
using WMS.Data;
using WMS.ViewModels;
using WMS.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

public class HomeControllerTests
{
    // ================= DB IN-MEMORY =================
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    // ================= CONTROLLER =================
    private HomeController GetController(AppDbContext db)
    {
        return new HomeController(db);
    }

    // ================= TEST INDEX =================
    [Fact]
    public async Task Index_ShouldReturnDashboardViewModel()
    {
        // Arrange
        var db = GetDbContext();

        // seed data minimal để không bị null
        db.Items.Add(new Item
        {
            ItemId = 1,
            IsActive = true,
            CurrentStock = 10,
            MinThreshold = 5,
            MaxThreshold = 20,
            TotalStockValue = 1000
        });

        db.Warehouses.Add(new Warehouse { WarehouseId = 1, IsActive = true });
        db.Partners.Add(new Partner { PartnerId = 1, IsActive = true });

        db.SaveChanges();

        var controller = GetController(db);

        // Act
        var result = await controller.Index();

        // Assert
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardViewModel>(view.Model);

        Assert.True(model.TotalItems >= 0);
        Assert.True(model.TotalWarehouses >= 0);
        Assert.True(model.TotalPartners >= 0);
        Assert.NotNull(model.LowStockItems);
        Assert.NotNull(model.RecentVouchers);
        Assert.NotNull(model.UnresolvedAlerts);
    }
}