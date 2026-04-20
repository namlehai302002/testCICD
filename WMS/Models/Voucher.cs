using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("Vouchers")]
public class Voucher
{
    [Key]
    public long VoucherId { get; set; }

    [Required, MaxLength(30)]
    public string VoucherCode { get; set; } = "";

    public byte VoucherType { get; set; }

    [Column(TypeName = "date")]
    public DateTime VoucherDate { get; set; } = DateTime.UtcNow.Date;

    public int WarehouseId { get; set; }

    public int? DestWarehouseId { get; set; }

    public int? PartnerId { get; set; }

    public byte SourceType { get; set; } = 1; // 1=Manual, 2=Excel, 3=AI_Gemini

    [MaxLength(50)]
    public string? ReferenceNo { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalAmount { get; set; }

    public int TotalLines { get; set; }

    public bool IsPosted { get; set; } = true;

    public bool IsCancelled { get; set; }

    [MaxLength(100)]
    public string? CancelledBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    [MaxLength(500)]
    public string? CancelReason { get; set; }

    [Required, MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    public long? AiOcrLogId { get; set; }

    public long? ParentVoucherId { get; set; }

    public long? WaveId { get; set; }

    public byte FulfillmentStatus { get; set; } = 1; // 1=Draft,2=WaitingForPick,3=Picking,4=Picked,5=Completed,6=PartiallyIssued

    // Inbound check accountability: checker must be different from creator.
    [MaxLength(100)]
    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(1000)]
    public string? ReviewNote { get; set; }

    public byte ReviewResult { get; set; } = 1; // 1=Pending, 2=Pass, 3=PassWithAdjustment

    [Column(TypeName = "decimal(5,2)")]
    public decimal ResponsibilityScore { get; set; } = 0; // 0..100

    [ForeignKey("WarehouseId")]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey("DestWarehouseId")]
    public Warehouse? DestWarehouse { get; set; }

    [ForeignKey("PartnerId")]
    public Partner? Partner { get; set; }

    public ICollection<VoucherDetail> Details { get; set; } = new List<VoucherDetail>();

    public string VoucherTypeName => VoucherType switch
    {
        1 => "Nhập Kho",
        2 => "Xuất Kho",
        3 => "Trả NCC",
        4 => "Khách Trả",
        5 => "Điều Chỉnh",
        6 => "Chuyển Kho",
        7 => "Nhập Thành Phẩm",
        8 => "Xuất Sản Xuất",
        _ => "Khác"
    };

    public string SourceTypeName => SourceType switch
    {
        1 => "Thủ Công",
        2 => "Excel",
        3 => "AI Gemini",
        _ => "Khác"
    };

    [NotMapped]
    public bool IsPartial => Details != null && Details.Any(d => d.DefectQty > 0);

    public string StatusDisplay => IsCancelled
        ? "Đã Hủy"
        : IsPosted
            ? (IsPartial ? "Còn Thiếu" : "Đã Ghi Sổ")
            : (VoucherType is 2 or 3 or 6 or 8) && FulfillmentStatus == 6
                ? "Xuất Một Phần"
                : ((VoucherType == 1 || VoucherType == 7) ? "Chờ Kiểm Kho" : "Nháp");

    public string StatusBadgeClass => IsCancelled
        ? "badge-danger"
        : IsPosted
            ? (IsPartial ? "badge-warning" : "badge-success")
            : (VoucherType is 2 or 3 or 6 or 8) && FulfillmentStatus == 6
                ? "badge-warning"
                : ((VoucherType == 1 || VoucherType == 7) ? "badge-info" : "badge-secondary");
}
