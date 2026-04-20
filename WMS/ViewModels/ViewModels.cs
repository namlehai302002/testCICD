using WMS.Models;
using System.ComponentModel.DataAnnotations;

namespace WMS.ViewModels;

public class DashboardViewModel
{
    public int TotalItems { get; set; }
    public int TotalWarehouses { get; set; }
    public int TotalPartners { get; set; }
    public int TodayVouchers { get; set; }
    public decimal TotalStockValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int OverStockCount { get; set; }
    public List<Item> LowStockItems { get; set; } = new();
    public List<Voucher> RecentVouchers { get; set; } = new();
    public List<StockAlert> UnresolvedAlerts { get; set; } = new();
    public Dictionary<string, int> VouchersByType { get; set; } = new();
    public Dictionary<string, decimal> StockByCategory { get; set; } = new();
    public int OpenWaves { get; set; }
    public int OpenPickTasks { get; set; }
    public int ShortPickTasks { get; set; }
    public decimal ReservationFillRate { get; set; }
}

public class LoginViewModel
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public class VoucherCreateViewModel
{
    public byte VoucherType { get; set; }
    public int WarehouseId { get; set; }
    public int? DestWarehouseId { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn Đối Tác")]
    public int? PartnerId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
    public long? ParentVoucherId { get; set; }
    public List<VoucherDetailLine> Lines { get; set; } = new();

    public byte ExportMode { get; set; } = 1;
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Partner> Partners { get; set; } = new();
    public List<Item> Items { get; set; } = new();
    public List<UnitOfMeasure> Uoms { get; set; } = new();
    public List<Location> Locations { get; set; } = new();
    public List<PackagingUnit> PackagingUnits { get; set; } = new();
}

public class VoucherDetailLine
{
    public int ItemId { get; set; }
    public int? LocationId { get; set; }
    public int? DestLocationId { get; set; }
    [Range(0.01, double.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public decimal TransactionQty { get; set; }
    public decimal DestQty { get; set; }
    public decimal DefectQty { get; set; }
    public int TransactionUomId { get; set; }
    public int? PackagingUnitId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public byte QualityStatus { get; set; } = 1;
    public DateTime? ExpiryDate { get; set; }
    public string? LotNumber { get; set; }
    public string? Notes { get; set; }

    // Used for VoucherType = 5 (Điều chỉnh). 1 = Tăng, -1 = Giảm
    public sbyte AdjustSign { get; set; } = 1;
}

public class ItemFormViewModel
{
    public Item Item { get; set; } = new();
    public List<ItemCategory> Categories { get; set; } = new();
    public List<UnitOfMeasure> Uoms { get; set; } = new();
    public List<Location> Locations { get; set; } = new();
}

public class ReportFilterViewModel
{
    public int? ItemId { get; set; }
    public int? WarehouseId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public List<Item> Items { get; set; } = new();
    public List<Warehouse> Warehouses { get; set; } = new();
}

public class StockSnapshotCompareRow
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string UomCode { get; set; } = "";

    public decimal SnapshotQty { get; set; }
    public decimal CurrentQty { get; set; }
    public decimal DiffQty { get; set; } // Snapshot - Current

    public decimal UnitCost { get; set; }
    public decimal SnapshotValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal DiffValue { get; set; }
}

public class StockCountLineInput
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public int LocationId { get; set; }
    public string LocationCode { get; set; } = "";
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal SystemQty { get; set; }
    public decimal CountedQty { get; set; }
}

public class StockCountPageViewModel
{
    public int? WarehouseId { get; set; }
    public DateTime CountDate { get; set; } = DateTime.UtcNow.Date;
    public string? Notes { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<StockCountLineInput> Lines { get; set; } = new();
    public List<StockCountSheetSummary> ExistingSheets { get; set; } = new();
}

public class StockCountSheetSummary
{
    public long StockCountSheetId { get; set; }
    public DateTime CountDate { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public byte Status { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalReason { get; set; }
    public string? UnlockedBy { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public string? UnlockReason { get; set; }
    public int TotalLines { get; set; }
    public int DiffLines { get; set; }
    public string? VoucherCode { get; set; }
}

public class WaveBoardRow
{
    public long WaveId { get; set; }
    public string WaveCode { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public byte Status { get; set; }
    public int OpenTasks { get; set; }
    public int DoneTasks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class PickTaskBoardRow
{
    public long PickTaskId { get; set; }
    public string TaskCode { get; set; } = "";
    public long WaveId { get; set; }
    public string WaveCode { get; set; } = "";
    public string VoucherCode { get; set; } = "";
    public string ItemCode { get; set; } = "";
    public string LocationCode { get; set; } = "";
    public decimal TargetQty { get; set; }
    public decimal PickedQty { get; set; }
    public byte Status { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? CompletedAt { get; set; }
}
