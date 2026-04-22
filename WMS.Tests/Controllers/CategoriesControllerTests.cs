using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using WMS.Controllers;
using WMS.Data;
using WMS.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CategoriesUnitsControllerTests
{
    // ================= DB INIT =================
    // Tạo InMemory DB riêng cho từng test → tránh conflict data
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    // ================= CONTROLLER INIT =================
    // Setup CategoriesController + TempData fake để tránh NullException
    private CategoriesController GetCategoriesController(AppDbContext db)
    {
        var controller = new CategoriesController(db);

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

    // Setup UnitsController + TempData fake
    private UnitsController GetUnitsController(AppDbContext db)
    {
        var controller = new UnitsController(db);

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

    // =========================================================
    // ===================== CATEGORIES ========================
    // =========================================================

    [Fact]
    public async Task CreateCategory_Valid_ShouldSuccess()
    {
        // TEST: Tạo category hợp lệ → phải lưu DB + redirect Index

        var db = GetDbContext();
        var controller = GetCategoriesController(db);

        var cat = new ItemCategory
        {
            CategoryCode = "CAT01",
            CategoryName = "Thực phẩm",
            SortOrder = 1
        };

        var result = await controller.Create(cat);

        // EXPECT: Redirect về Index sau khi tạo thành công
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        // EXPECT: DB phải có đúng 1 record
        Assert.Single(db.ItemCategories);
    }

    [Fact]
    public async Task CreateCategory_DuplicateCode_ShouldFail()
    {
        // TEST: Trùng CategoryCode → không cho tạo mới

        var db = GetDbContext();

        db.ItemCategories.Add(new ItemCategory
        {
            CategoryCode = "CAT01",
            CategoryName = "Old"
        });
        db.SaveChanges();

        var controller = GetCategoriesController(db);

        var result = await controller.Create(new ItemCategory
        {
            CategoryCode = "CAT01",
            CategoryName = "New"
        });

        // EXPECT: trả về View (không redirect)
        var view = Assert.IsType<ViewResult>(result);

        // EXPECT: giữ model để hiển thị lỗi
        var model = Assert.IsType<ItemCategory>(view.Model);

        Assert.NotNull(model);

        // EXPECT: DB không bị duplicate
        Assert.Equal(1, db.ItemCategories.Count());
    }

    [Fact]
    public async Task DeleteCategory_ShouldSetInactive()
    {
        // TEST: Xóa category → chỉ soft delete (IsActive=false)

        var db = GetDbContext();

        var cat = new ItemCategory
        {
            CategoryCode = "CAT01",
            CategoryName = "Test",
            IsActive = true
        };

        db.ItemCategories.Add(cat);
        db.SaveChanges();

        var controller = GetCategoriesController(db);

        var result = await controller.Delete(cat.CategoryId);

        // EXPECT: redirect sau khi delete
        var redirect = Assert.IsType<RedirectToActionResult>(result);

        // EXPECT: IsActive phải = false (soft delete)
        var updated = await db.ItemCategories.FirstAsync();
        Assert.False(updated.IsActive);
    }

    // =========================================================
    // ======================== UNITS ==========================
    // =========================================================

    [Fact]
    public async Task CreateUnit_Valid_ShouldSuccess()
    {
        // TEST: Tạo UnitOfMeasure hợp lệ → lưu DB thành công

        var db = GetDbContext();
        var controller = GetUnitsController(db);

        var result = await controller.Create("KG", "Kilogram");

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Single(db.UnitsOfMeasure);
    }

    [Fact]
    public async Task CreateUnit_Duplicate_ShouldFail()
    {
        // TEST: Trùng UomCode → không thêm mới

        var db = GetDbContext();

        db.UnitsOfMeasure.Add(new UnitOfMeasure
        {
            UomCode = "KG",
            UomName = "Old"
        });
        db.SaveChanges();

        var controller = GetUnitsController(db);

        var result = await controller.Create("KG", "New");

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        // EXPECT: không tăng số lượng
        Assert.Single(db.UnitsOfMeasure);
    }

    [Fact]
    public async Task DeleteUnit_ShouldSetInactive()
    {
        // TEST: Delete Unit → soft delete IsActive=false

        var db = GetDbContext();

        var uom = new UnitOfMeasure
        {
            UomCode = "KG",
            UomName = "Kilogram",
            IsActive = true
        };

        db.UnitsOfMeasure.Add(uom);
        db.SaveChanges();

        var controller = GetUnitsController(db);

        var result = await controller.Delete(uom.UomId);

        var updated = await db.UnitsOfMeasure.FirstAsync();

        Assert.False(updated.IsActive);
    }

    // =========================================================
    // ===================== PACKAGING =========================
    // =========================================================

    [Fact]
    public async Task CreatePackaging_Valid_ShouldSuccess()
    {
        // TEST: Tạo packaging hợp lệ → lưu DB

        var db = GetDbContext();
        var controller = GetUnitsController(db);

        var result = await controller.CreatePackaging("Thùng 24", 1, 24);

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Single(db.PackagingUnits);
    }

    [Fact]
    public async Task CreatePackaging_Duplicate_ShouldNotAdd()
    {
        // TEST: Trùng TenDongGoi → không insert mới

        var db = GetDbContext();

        db.PackagingUnits.Add(new PackagingUnit
        {
            TenDongGoi = "Thùng 24",
            BaseUomId = 1,
            GiaTri = 24,
            IsActive = true
        });
        db.SaveChanges();

        var controller = GetUnitsController(db);

        var result = await controller.CreatePackaging("Thùng 24", 1, 24);

        Assert.Single(db.PackagingUnits);
    }

    [Fact]
    public async Task DeletePackaging_ShouldInactive()
    {
        // TEST: Delete packaging → soft delete

        var db = GetDbContext();

        var pkg = new PackagingUnit
        {
            TenDongGoi = "Thùng 12",
            BaseUomId = 1,
            GiaTri = 12,
            IsActive = true
        };

        db.PackagingUnits.Add(pkg);
        db.SaveChanges();

        var controller = GetUnitsController(db);

        var result = await controller.DeletePackaging(pkg.PackagingUnitId);

        var updated = await db.PackagingUnits.FirstAsync();

        Assert.False(updated.IsActive);
    }
}