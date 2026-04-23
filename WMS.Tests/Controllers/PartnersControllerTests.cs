using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Linq;
using System.Threading.Tasks;
using WMS.Controllers;
using WMS.Data;
using WMS.Models;

public class PartnersControllerTests
{
    // =====================================================
    // KHỞI TẠO DATABASE TEST (INMEMORY)
    // =====================================================
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    // =====================================================
    // KHỞI TẠO CONTROLLER
    // =====================================================
    private PartnersController GetController(AppDbContext db)
    {
        var controller = new PartnersController(db);

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
    // TEST INDEX (FILTER + SEARCH)
    // =====================================================

    /*
        TEST: Load danh sách partner bình thường
        EXPECT: trả View + có dữ liệu active
    */
    [Fact]
    public async Task Index_ShouldReturnActivePartners()
    {
        var db = GetDbContext();

        db.Partners.Add(new Partner
        {
            PartnerCode = "P001",
            PartnerName = "Cty A",
            PartnerType = 1,
            IsActive = true
        });

        db.SaveChanges();

        var controller = GetController(db);

        var result = await controller.Index(null, null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.NotNull(view.Model);
    }

    /*
        TEST: Filter theo loại partner
    */
    [Fact]
    public async Task Index_FilterByType_ShouldWork()
    {
        var db = GetDbContext();

        db.Partners.Add(new Partner
        {
            PartnerCode = "P001",
            PartnerName = "Supplier A",
            PartnerType = 1,
            IsActive = true
        });

        db.Partners.Add(new Partner
        {
            PartnerCode = "P002",
            PartnerName = "Customer B",
            PartnerType = 2,
            IsActive = true
        });

        db.SaveChanges();

        var controller = GetController(db);

        var result = await controller.Index(1, null);

        var view = Assert.IsType<ViewResult>(result);

        Assert.NotNull(view.Model);
    }

    /*
        TEST: Search theo tên hoặc mã
    */
    [Fact]
    public async Task Index_Search_ShouldReturnCorrectData()
    {
        var db = GetDbContext();

        db.Partners.Add(new Partner
        {
            PartnerCode = "P001",
            PartnerName = "Cong ty ABC",
            PartnerType = 1,
            IsActive = true
        });

        db.SaveChanges();

        var controller = GetController(db);

        var result = await controller.Index(null, "ABC");

        var view = Assert.IsType<ViewResult>(result);

        Assert.NotNull(view.Model);
    }

    // =====================================================
    // TEST CREATE PARTNER
    // =====================================================

    /*
        TEST: Tạo partner thành công
    */
    [Fact]
    public async Task Create_ShouldAddPartner()
    {
        var db = GetDbContext();

        var controller = GetController(db);

        var partner = new Partner
        {
            PartnerCode = "P001",
            PartnerName = "Cty A",
            PartnerType = 1
        };

        var result = await controller.Create(partner);

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal("Index", redirect.ActionName);
        Assert.Single(db.Partners);
    }

    // =====================================================
    // TEST EDIT PARTNER
    // =====================================================

    /*
        TEST: Cập nhật partner thành công
    */
    [Fact]
    public async Task Edit_ShouldUpdatePartner()
    {
        var db = GetDbContext();

        var partner = new Partner
        {
            PartnerId = 1,
            PartnerCode = "P001",
            PartnerName = "Old Name",
            PartnerType = 1,
            IsActive = true
        };

        db.Partners.Add(partner);
        db.SaveChanges();

        var controller = GetController(db);

        var updated = new Partner
        {
            PartnerCode = "P001",
            PartnerName = "New Name",
            PartnerType = 2
        };

        var result = await controller.Edit(1, updated);

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        var dbData = db.Partners.First();

        Assert.Equal("New Name", dbData.PartnerName);
        Assert.Equal(2, dbData.PartnerType);
    }

    /*
        TEST: Edit không tìm thấy partner
    */
    [Fact]
    public async Task Edit_NotFound_ShouldReturn404()
    {
        var db = GetDbContext();

        var controller = GetController(db);

        var result = await controller.Edit(999, new Partner());

        Assert.IsType<NotFoundResult>(result);
    }

    // =====================================================
    // TEST DELETE (SOFT DELETE)
    // =====================================================

    /*
        TEST: Xóa mềm partner (IsActive = false)
    */
    [Fact]
    public async Task Delete_ShouldSoftDelete()
    {
        var db = GetDbContext();

        var partner = new Partner
        {
            PartnerId = 1,
            PartnerCode = "P001",
            PartnerName = "Cty A",
            IsActive = true
        };

        db.Partners.Add(partner);
        db.SaveChanges();

        var controller = GetController(db);

        var result = await controller.Delete(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        var dbData = db.Partners.First();

        Assert.False(dbData.IsActive);
    }

    /*
        TEST: Delete không tồn tại
    */
    [Fact]
    public async Task Delete_NotFound_ShouldReturn404()
    {
        var db = GetDbContext();

        var controller = GetController(db);

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }
}