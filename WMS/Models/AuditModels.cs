using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("AuditLogs")]
public class AuditLog
{
    [Key]
    public long AuditLogId { get; set; }

    [Required, MaxLength(128)]
    public string TableName { get; set; } = "";

    [Required, MaxLength(50)]
    public string RecordId { get; set; } = "";

    [Required, MaxLength(10)]
    public string ActionType { get; set; } = "";

    [MaxLength(128)]
    public string? ColumnChanged { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    [MaxLength(100)]
    public string? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(100)]
    public string? AppModule { get; set; }

    [MaxLength(100)]
    public string? SessionId { get; set; }
}

[Table("AiOcrLogs")]
public class AiOcrLog
{
    [Key]
    public long AiOcrLogId { get; set; }

    [Required, MaxLength(1000)]
    public string ImageUrl { get; set; } = "";

    [MaxLength(255)]
    public string? FileName { get; set; }

    public long? FileSize { get; set; }

    [MaxLength(50)]
    public string OcrProvider { get; set; } = "Gemini_Vision";

    [MaxLength(50)]
    public string? ModelVersion { get; set; }

    public string? RawJsonResponse { get; set; }

    public string? ParsedData { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal? ConfidenceScore { get; set; }

    public int? DetectedItems { get; set; }

    public int? ProcessingTimeMs { get; set; }

    public byte Status { get; set; } = 1;

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public long? VoucherId { get; set; }

    [Required, MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("VoucherId")]
    public Voucher? Voucher { get; set; }
}

[Table("AiOcrAdjustments")]
public class AiOcrAdjustment
{
    [Key]
    public long AdjustmentId { get; set; }

    public long AiOcrLogId { get; set; }

    [Required, MaxLength(100)]
    public string FieldName { get; set; } = "";

    [MaxLength(500)]
    public string? AiOriginalValue { get; set; }

    [MaxLength(500)]
    public string? UserCorrectedValue { get; set; }

    public int? ItemId { get; set; }

    public int? LineNumber { get; set; }

    [MaxLength(300)]
    public string? Reason { get; set; }

    [Required, MaxLength(100)]
    public string CorrectedBy { get; set; } = "";

    public DateTime CorrectedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("AiOcrLogId")]
    public AiOcrLog? AiOcrLog { get; set; }

    [ForeignKey("ItemId")]
    public Item? Item { get; set; }
}

[Table("StockSnapshots")]
public class StockSnapshot
{
    [Key]
    public long SnapshotId { get; set; }

    [Column(TypeName = "date")]
    public DateTime SnapshotDate { get; set; }

    public int ItemId { get; set; }

    public int WarehouseId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ClosingStock { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalValue { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [ForeignKey("WarehouseId")]
    public Warehouse? Warehouse { get; set; }
}

[Table("StockAlerts")]
public class StockAlert
{
    [Key]
    public long AlertId { get; set; }

    public int ItemId { get; set; }

    public byte AlertType { get; set; } = 1; // 1=LowStock, 2=OverStock, 3=Expiry

    [Column(TypeName = "decimal(18,4)")]
    public decimal CurrentStock { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Threshold { get; set; }

    public bool IsRead { get; set; }

    public bool IsResolved { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    public string AlertTypeName => AlertType switch
    {
        1 => "Tồn Kho Thấp",
        2 => "Tồn Kho Cao",
        3 => "Sắp Hết Hạn",
        _ => "Khác"
    };
}
