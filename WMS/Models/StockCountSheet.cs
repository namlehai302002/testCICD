using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("StockCountSheets")]
public class StockCountSheet
{
    [Key]
    public long StockCountSheetId { get; set; }

    public int WarehouseId { get; set; }

    [Column(TypeName = "date")]
    public DateTime CountDate { get; set; } = DateTime.UtcNow.Date;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public byte Status { get; set; } = 1; // 1=Draft, 2=Approved

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    [MaxLength(100)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [MaxLength(500)]
    public string? ApprovalReason { get; set; }

    [MaxLength(100)]
    public string? UnlockedBy { get; set; }

    public DateTime? UnlockedAt { get; set; }

    [MaxLength(500)]
    public string? UnlockReason { get; set; }

    public long? GeneratedAdjustmentVoucherId { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(GeneratedAdjustmentVoucherId))]
    public Voucher? GeneratedAdjustmentVoucher { get; set; }

    public ICollection<StockCountLine> Lines { get; set; } = new List<StockCountLine>();
}

