using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Security.Claims;

using WMS.Controllers;
using WMS.Data;
using WMS.Models;
using WMS.ViewModels;

public class ItemsControllerTests
{
    // ================= DB MOCK =================
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // ✅ FIX transaction error
            .ConfigureWarnings(w => 
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AppDbContext(options);

        db.UnitsOfMeasure.Add(new UnitOfMeasure
        {
            UomId = 1,
            UomCode = "PCS",
            IsActive = true
        });

        db.ItemCategories.Add(new ItemCategory
        {
            CategoryId = 1,
            CategoryName = "Test Cat",
            CategoryCode = "TC",
            IsActive = true
        });

        db.Items.Add(new Item
        {
            ItemId = 1,
            ItemCode = "NVL-001",
            ItemName = "Item 1",
            BaseUomId = 1,
            CurrentStock = 0,
            MinThreshold = 5,
            IsActive = true
        });

        db.SaveChanges();
        return db;
    }

    // ================= CONTROLLER MOCK =================
   private ItemsController GetController(AppDbContext db)
{
    var env = new Mock<IWebHostEnvironment>();
    env.Setup(e => e.WebRootPath).Returns(Directory.GetCurrentDirectory());

    var controller = new ItemsController(db, env.Object);

    // ✅ Mock User
    var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.Name, "testuser")
    }, "mock"));

    var httpContext = new DefaultHttpContext
    {
        User = user
    };

    // ✅ FIX QUAN TRỌNG: TempData
    var tempData = new TempDataDictionary(
        httpContext,
        Mock.Of<ITempDataProvider>()
    );

    controller.ControllerContext = new ControllerContext()
    {
        HttpContext = httpContext
    };

    controller.TempData = tempData;

    return controller;
}

    // ================= TEST =================

    [Fact]
    public async Task Index_Returns_View_With_Items()
    {
        // Test: Load danh sách item
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.Index(null, null, null, null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Item>>(view.Model);

        Assert.Single(model);
    }

    [Fact]
    public async Task Index_Filter_By_Search()
    {
        // Test: Search theo ItemCode
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.Index("NVL", null, null, null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Item>>(view.Model);

        Assert.Single(model);
    }

    [Fact]
    public async Task Create_Post_Adds_Item()
    {
        // Test: Tạo item mới
        var db = GetDbContext();
        var controller = GetController(db);

        var vm = new ItemFormViewModel
        {
            Item = new Item
            {
                ItemCode = "NVL-002",
                ItemName = "New Item",
                BaseUomId = 1
            }
        };

        var result = await controller.Create(vm, null);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(2, db.Items.Count());
    }

    [Fact]
    public async Task Create_Post_Sets_Barcode_When_Empty()
    {
        // Test: Barcode tự set = ItemCode nếu null
        var db = GetDbContext();
        var controller = GetController(db);

        var vm = new ItemFormViewModel
        {
            Item = new Item
            {
                ItemCode = "NVL-003",
                ItemName = "Item 3",
                BaseUomId = 1,
                Barcode = null
            }
        };

        await controller.Create(vm, null);

        var item = db.Items.First(i => i.ItemCode == "NVL-003");
        Assert.Equal("NVL-003", item.Barcode);
    }

    [Fact]
    public async Task Edit_Post_Updates_Item()
    {
        // Test: Update item
        var db = GetDbContext();
        var controller = GetController(db);

        var vm = new ItemFormViewModel
        {
            Item = new Item
            {
                ItemCode = "UPDATED",
                ItemName = "Updated Name",
                BaseUomId = 1
            }
        };

        await controller.Edit(1, vm, null);

        var updated = db.Items.First();

        Assert.Equal("UPDATED", updated.ItemCode);
        Assert.Equal("Updated Name", updated.ItemName);
    }

    [Fact]
    public async Task Delete_Sets_IsActive_False()
    {
        // Test: Soft delete
        var db = GetDbContext();
        var controller = GetController(db);

        await controller.Delete(1);

        var item = db.Items.First();
        Assert.False(item.IsActive);
    }

    [Fact]
    public async Task GetItemJson_Returns_Json()
    {
        // Test: API JSON item
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.GetItemJson(1);

        Assert.IsType<JsonResult>(result);
    }

    [Fact]
    public async Task GetItemByBarcode_Returns_Item()
    {
        // Test: tìm theo barcode
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.GetItemByBarcode("NVL-001");

        Assert.IsType<JsonResult>(result);
    }

    [Fact]
    public async Task GetItemByBarcode_Returns_NotFound()
    {
        // Test: barcode không tồn tại
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.GetItemByBarcode("NOTFOUND");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GenerateItemCode_Returns_Correct_Format()
    {
        // Test: generate mã NVL-xxx
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.GenerateItemCode(1);

        var json = Assert.IsType<JsonResult>(result);

        var codeProp = json.Value?.GetType().GetProperty("code");
        var code = codeProp?.GetValue(json.Value)?.ToString();

        Assert.NotNull(code);
        Assert.StartsWith("NVL-", code);
    }

    [Fact]
    public async Task Details_Returns_View()
    {
        // Test: load trang details
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.Details(1);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_Filter_StockStatus_Out()
    {
        // Test: filter Hết Hàng
        var db = GetDbContext();
        var controller = GetController(db);

        var result = await controller.Index(null, null, null, "out");

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Item>>(view.Model);

        Assert.Single(model);
        Assert.Equal("Hết Hàng", model.First().StockStatus);
    }

    [Fact]
    public async Task Create_With_Image_Upload()
    {
        // Test: upload ảnh
        var db = GetDbContext();
        var controller = GetController(db);

        var fileMock = new Mock<IFormFile>();
        var content = "Fake Image";
        var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.Length).Returns(ms.Length);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
            .Returns((Stream stream, System.Threading.CancellationToken token) =>
            {
                ms.Position = 0;
                return ms.CopyToAsync(stream);
            });

        var vm = new ItemFormViewModel
        {
            Item = new Item
            {
                ItemCode = "NVL-IMG",
                ItemName = "Item Img",
                BaseUomId = 1
            }
        };

        await controller.Create(vm, fileMock.Object);

        var item = db.Items.First(i => i.ItemCode == "NVL-IMG");

        Assert.NotNull(item.ImageUrl);
    }
}