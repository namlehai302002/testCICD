using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("PickTasks")]
public class PickTask
{
    [Key]
    public long PickTaskId { get; set; }

    [Required, MaxLength(40)]
    public string TaskCode { get; set; } = "";

    public long WaveId { get; set; }

    public long VoucherId { get; set; }

    public long? VoucherDetailId { get; set; }

    public int ItemId { get; set; }

    public int SourceLocationId { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TargetQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal PickedQty { get; set; }

    public byte Status { get; set; } = 1; // 1=Open,2=Assigned,3=InProgress,4=Done,5=ShortPicked,6=Cancelled

    [MaxLength(100)]
    public string? AssignedTo { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [ForeignKey(nameof(WaveId))]
    public Wave? Wave { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    [ForeignKey(nameof(VoucherDetailId))]
    public VoucherDetail? VoucherDetail { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(SourceLocationId))]
    public Location? SourceLocation { get; set; }
}

