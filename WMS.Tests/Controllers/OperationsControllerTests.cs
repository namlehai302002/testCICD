/*
========================================================
 FILE: OperationsControllerTests.cs
 MỤC ĐÍCH:
    Test nghiệp vụ module Operations trong hệ thống WMS

 PHẠM VI TEST:
    - Waves: danh sách + phân quyền kho
    - PickTasks: lọc theo Wave / Status
    - AssignTask: gán công việc + kiểm tra phân quyền

 PHONG CÁCH:
    - AAA (Arrange - Act - Assert)
    - Dùng InMemory Database (EF Core)
    - Giả lập user Admin / Staff
    - Có TempData + HttpContext như thật

========================================================
*/

using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Security.Claims;
using WMS.Controllers;
using WMS.Data;
using WMS.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

public class OperationsControllerTests
{
    // =====================================================
    // KHỞI TẠO DATABASE TEST (INMEMORY)
    // =====================================================
    /*
        Mỗi test sẽ có 1 database riêng
        → tránh ảnh hưởng dữ liệu giữa các test case
    */
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    // =====================================================
    // KHỞI TẠO CONTROLLER GIẢ LẬP
    // =====================================================
    /*
        Mục đích:
        - Tạo controller giống môi trường thật
        - Gắn HttpContext + User + TempData
    */
    private OperationsController GetController(AppDbContext db, ClaimsPrincipal user = null)
    {
        var controller = new OperationsController(db);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        controller.ControllerContext.HttpContext.User = user ?? new ClaimsPrincipal();

        controller.TempData = new TempDataDictionary(
            controller.ControllerContext.HttpContext,
            Mock.Of<ITempDataProvider>()
        );

        return controller;
    }

    // =====================================================
    // GIẢ LẬP USER ADMIN
    // =====================================================
    /*
        Admin:
        - Có quyền xem tất cả warehouse
        - Không bị giới hạn scope
    */
    private ClaimsPrincipal AdminUser()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin")
        });

        return new ClaimsPrincipal(identity);
    }

    // =====================================================
    // GIẢ LẬP USER STAFF
    // =====================================================
    /*
        Staff:
        - Chỉ được xem warehouse được gán (WarehouseId claim)
    */
    private ClaimsPrincipal StaffUser(int warehouseId = 1)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Staff"),
            new Claim("WarehouseId", warehouseId.ToString())
        });

        return new ClaimsPrincipal(identity);
    }

    // =====================================================
    // TEST WAVES - ADMIN
    // =====================================================
    /*
        TEST: Admin xem danh sách Waves

        KỲ VỌNG:
        - Trả về ViewResult
        - Có dữ liệu Model
    */
    [Fact]
    public async Task Waves_Admin_ShouldReturnAll()
    {
        var db = GetDbContext();

        db.Warehouses.Add(new Warehouse
        {
            WarehouseId = 1,
            WarehouseName = "WH1",
            WarehouseCode = "WH1",
            IsActive = true
        });

        db.Waves.Add(new Wave
        {
            WaveId = 1,
            WaveCode = "WV001",
            WarehouseId = 1,
            CreatedAt = DateTime.UtcNow
        });

        db.SaveChanges();

        var controller = GetController(db, AdminUser());

        var result = await controller.Waves(null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.NotNull(view.Model);
    }

    // =====================================================
    // TEST WAVES - STAFF SCOPE
    // =====================================================
    /*
        TEST: Staff chỉ xem dữ liệu warehouse của mình

        MỤC ĐÍCH:
        - Kiểm tra logic phân quyền kho (multi-warehouse)
    */
    [Fact]
    public async Task Waves_Staff_ShouldBeScopedByWarehouse()
    {
        var db = GetDbContext();

        db.Waves.Add(new Wave { WaveId = 1, WaveCode = "WV001", WarehouseId = 1 });
        db.Waves.Add(new Wave { WaveId = 2, WaveCode = "WV002", WarehouseId = 2 });

        db.SaveChanges();

        var controller = GetController(db, StaffUser(1));

        var result = await controller.Waves(null);

        var view = Assert.IsType<ViewResult>(result);

        Assert.NotNull(view.Model);
    }

    // =====================================================
    // TEST PICKTASK FILTER BY WAVE
    // =====================================================
    /*
        TEST: Lọc task theo WaveId
    */
    [Fact]
    public async Task PickTasks_FilterByWave_ShouldWork()
    {
        var db = GetDbContext();

        db.Waves.Add(new Wave { WaveId = 1, WaveCode = "WV001", WarehouseId = 1 });

        db.PickTasks.Add(new PickTask
        {
            PickTaskId = 1,
            TaskCode = "TK001",
            WaveId = 1,
            Status = 1
        });

        db.SaveChanges();

        var controller = GetController(db, AdminUser());

        var result = await controller.PickTasks(1, null);

        var view = Assert.IsType<ViewResult>(result);

        Assert.NotNull(view.Model);
    }

    // =====================================================
    // TEST PICKTASK FILTER BY STATUS
    // =====================================================
    /*
        TEST: Lọc task theo trạng thái
    */
    [Fact]
    public async Task PickTasks_FilterByStatus_ShouldWork()
    {
        var db = GetDbContext();

        db.PickTasks.Add(new PickTask { PickTaskId = 1, Status = 1 });
        db.PickTasks.Add(new PickTask { PickTaskId = 2, Status = 4 });

        db.SaveChanges();

        var controller = GetController(db, AdminUser());

        var result = await controller.PickTasks(null, 4);

        var view = Assert.IsType<ViewResult>(result);

        Assert.NotNull(view.Model);
    }

    // =====================================================
    // TEST ASSIGN TASK - THÀNH CÔNG
    // =====================================================
    /*
        TEST:
        - Gán user cho task
        - Tự động update status 1 → 2
    */
    [Fact]
    public async Task AssignTask_Valid_ShouldUpdate()
    {
        var db = GetDbContext();

        db.Waves.Add(new Wave { WaveId = 1, WarehouseId = 1 });

        db.PickTasks.Add(new PickTask
        {
            PickTaskId = 1,
            TaskCode = "TK001",
            WaveId = 1,
            Status = 1
        });

        db.SaveChanges();

        var controller = GetController(db, AdminUser());

        var result = await controller.AssignTask(1, "userA");

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        var updated = db.PickTasks.First();

        Assert.Equal("userA", updated.AssignedTo);
        Assert.Equal(2, updated.Status);
    }

    // =====================================================
    // TEST ASSIGN TASK - KHÔNG TÌM THẤY
    // =====================================================
    /*
        TEST: Task không tồn tại → trả NotFound
    */
    [Fact]
    public async Task AssignTask_NotFound_ShouldReturn404()
    {
        var db = GetDbContext();

        var controller = GetController(db, AdminUser());

        var result = await controller.AssignTask(999, "userA");

        Assert.IsType<NotFoundResult>(result);
    }

    // =====================================================
    // TEST ASSIGN TASK - SAI KHO
    // =====================================================
    /*
        TEST:
        Staff không được assign task ngoài warehouse
    */
    [Fact]
    public async Task AssignTask_WrongWarehouse_ShouldForbid()
    {
        var db = GetDbContext();

        db.Waves.Add(new Wave { WaveId = 1, WarehouseId = 2 });

        db.PickTasks.Add(new PickTask
        {
            PickTaskId = 1,
            TaskCode = "TK001",
            WaveId = 1,
            Status = 1
        });

        db.SaveChanges();

        var controller = GetController(db, StaffUser(1));

        var result = await controller.AssignTask(1, "userA");

        Assert.IsType<ForbidResult>(result);
    }

    // =====================================================
    // TEST ASSIGN TASK - RỖNG USER
    // =====================================================
    /*
        TEST:
        Chuỗi rỗng → phải convert thành NULL
    */
    [Fact]
    public async Task AssignTask_EmptyUser_ShouldSetNull()
    {
        var db = GetDbContext();

        db.Waves.Add(new Wave { WaveId = 1, WarehouseId = 1 });

        db.PickTasks.Add(new PickTask
        {
            PickTaskId = 1,
            TaskCode = "TK001",
            WaveId = 1,
            Status = 1
        });

        db.SaveChanges();

        var controller = GetController(db, AdminUser());

        var result = await controller.AssignTask(1, "   ");

        var updated = db.PickTasks.First();

        Assert.Null(updated.AssignedTo);
    }
}