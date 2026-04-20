using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.ViewModels;

namespace WMS.Controllers;

[Authorize]
public class OperationsController : Controller
{
    private readonly AppDbContext _db;
    public OperationsController(AppDbContext db) => _db = db;

    private int? GetScopedWarehouseId()
    {
        if (User.IsInRole("Admin")) return null;
        var claim = User.FindFirst("WarehouseId")?.Value;
        return int.TryParse(claim, out var id) ? id : -1;
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> Waves(int? warehouseId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var q = _db.Waves.AsNoTracking().Include(w => w.Warehouse).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(w => w.WarehouseId == warehouseId.Value);

        var waves = await q.OrderByDescending(w => w.CreatedAt).Take(200)
            .Select(w => new WaveBoardRow
            {
                WaveId = w.WaveId,
                WaveCode = w.WaveCode,
                WarehouseId = w.WarehouseId,
                WarehouseName = w.Warehouse != null ? w.Warehouse.WarehouseName : "",
                Status = w.Status,
                OpenTasks = _db.PickTasks.Count(t => t.WaveId == w.WaveId && (t.Status == 1 || t.Status == 2 || t.Status == 3)),
                DoneTasks = _db.PickTasks.Count(t => t.WaveId == w.WaveId && t.Status == 4),
                CreatedAt = w.CreatedAt,
                CompletedAt = w.CompletedAt
            }).ToListAsync();

        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync();
        ViewBag.WarehouseId = warehouseId;
        return View(waves);
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> PickTasks(long? waveId, byte? status)
    {
        var scopedWh = GetScopedWarehouseId();
        var q = _db.PickTasks.AsNoTracking()
            .Include(t => t.Wave)
            .Include(t => t.Voucher)
            .Include(t => t.Item)
            .Include(t => t.SourceLocation)
            .AsQueryable();

        if (waveId.HasValue) q = q.Where(t => t.WaveId == waveId.Value);
        if (status.HasValue) q = q.Where(t => t.Status == status.Value);
        if (scopedWh.HasValue) q = q.Where(t => t.Wave != null && t.Wave.WarehouseId == scopedWh.Value);

        var tasks = await q.OrderByDescending(t => t.PickTaskId).Take(500)
            .Select(t => new PickTaskBoardRow
            {
                PickTaskId = t.PickTaskId,
                TaskCode = t.TaskCode,
                WaveId = t.WaveId,
                WaveCode = t.Wave != null ? t.Wave.WaveCode : "",
                VoucherCode = t.Voucher != null ? t.Voucher.VoucherCode : "",
                ItemCode = t.Item != null ? t.Item.ItemCode : "",
                LocationCode = t.SourceLocation != null ? t.SourceLocation.LocationCode : "",
                TargetQty = t.TargetQty,
                PickedQty = t.PickedQty,
                Status = t.Status,
                AssignedTo = t.AssignedTo,
                CompletedAt = t.CompletedAt
            })
            .ToListAsync();

        var wavesQuery = _db.Waves.AsNoTracking().AsQueryable();
        if (scopedWh.HasValue) wavesQuery = wavesQuery.Where(w => w.WarehouseId == scopedWh.Value);
        ViewBag.Waves = await wavesQuery.OrderByDescending(w => w.CreatedAt).Take(100).ToListAsync();
        ViewBag.WaveId = waveId;
        ViewBag.Status = status;
        return View(tasks);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTask(long id, string assignedTo)
    {
        var task = await _db.PickTasks.Include(t => t.Wave).FirstOrDefaultAsync(t => t.PickTaskId == id);
        if (task == null) return NotFound();
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && task.Wave != null && task.Wave.WarehouseId != scopedWh.Value)
            return Forbid();

        task.AssignedTo = string.IsNullOrWhiteSpace(assignedTo) ? null : assignedTo.Trim();
        task.AssignedAt = DateTime.UtcNow;
        task.Status = task.Status == 1 ? (byte)2 : task.Status;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã gán task {task.TaskCode}.";
        return RedirectToAction(nameof(PickTasks), new { waveId = task.WaveId });
    }
}

