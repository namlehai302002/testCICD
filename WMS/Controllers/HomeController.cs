using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;
using WMS.ViewModels;

namespace WMS.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        var vm = new DashboardViewModel
        {
            TotalItems = await _db.Items.CountAsync(i => i.IsActive),
            TotalWarehouses = await _db.Warehouses.CountAsync(w => w.IsActive),
            TotalPartners = await _db.Partners.CountAsync(p => p.IsActive),
            TodayVouchers = await _db.Vouchers.CountAsync(v => v.VoucherDate == today && !v.IsCancelled),
            TotalStockValue = await _db.Items.Where(i => i.IsActive).SumAsync(i => i.TotalStockValue),
            LowStockCount = await _db.Items.CountAsync(i => i.IsActive && i.MinThreshold > 0 && i.CurrentStock <= i.MinThreshold && i.CurrentStock > 0),
            OutOfStockCount = await _db.Items.CountAsync(i => i.IsActive && i.CurrentStock <= 0),
            OverStockCount = await _db.Items.CountAsync(i => i.IsActive && i.MaxThreshold.HasValue && i.CurrentStock >= i.MaxThreshold.Value),
            LowStockItems = await _db.Items
                .Include(i => i.Category).Include(i => i.BaseUom)
                .Where(i => i.IsActive && i.MinThreshold > 0 && i.CurrentStock <= i.MinThreshold)
                .OrderBy(i => i.CurrentStock)
                .Take(10).ToListAsync(),
            RecentVouchers = await _db.Vouchers
                .Include(v => v.Warehouse).Include(v => v.Partner)
                .OrderByDescending(v => v.CreatedAt)
                .Take(10).ToListAsync(),
            UnresolvedAlerts = await _db.StockAlerts
                .Include(a => a.Item)
                .Where(a => !a.IsResolved)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10).ToListAsync()
        };

        vm.OpenWaves = await _db.Waves.CountAsync(w => w.Status == 2 || w.Status == 3);
        vm.OpenPickTasks = await _db.PickTasks.CountAsync(t => t.Status == 1 || t.Status == 2 || t.Status == 3);
        vm.ShortPickTasks = await _db.PickTasks.CountAsync(t => t.Status == 5);
        var activeReservations = await _db.StockReservations
            .Where(r => r.Status == 1)
            .Select(r => new { r.ReservedQty, r.ConsumedQty })
            .ToListAsync();
        var totalReserved = activeReservations.Sum(x => x.ReservedQty);
        var totalConsumed = activeReservations.Sum(x => x.ConsumedQty);
        vm.ReservationFillRate = totalReserved > 0 ? (totalConsumed / totalReserved) * 100m : 0m;

        // Vouchers by type (last 30 days)
        var thirtyDaysAgo = today.AddDays(-30);
        var vouchersByType = await _db.Vouchers
            .Where(v => v.VoucherDate >= thirtyDaysAgo && !v.IsCancelled)
            .GroupBy(v => v.VoucherType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        var typeNames = new Dictionary<byte, string>
        {
            {1, "Nhập Kho"}, {2, "Xuất Kho"}, {3, "Trả NCC"}, {4, "Khách Trả"},
            {5, "Điều Chỉnh"}, {6, "Chuyển Kho"}, {7, "Nhập TP"}, {8, "Xuất SX"}
        };
        vm.VouchersByType = vouchersByType.ToDictionary(
            v => typeNames.GetValueOrDefault(v.Type, "Khác"),
            v => v.Count);

        // Stock value by category
        var stockByCategory = await _db.Items
            .Where(i => i.IsActive)
            .Include(i => i.Category)
            .GroupBy(i => i.Category != null ? i.Category.CategoryName : "Chưa phân loại")
            .Select(g => new { Category = g.Key, Value = g.Sum(i => i.TotalStockValue) })
            .ToListAsync();
        vm.StockByCategory = stockByCategory.ToDictionary(s => s.Category, s => s.Value);

        return View(vm);
    }
}
