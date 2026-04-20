using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("StockReservations")]
public class StockReservation
{
    [Key]
    public long StockReservationId { get; set; }

    public long VoucherId { get; set; }

    public long? VoucherDetailId { get; set; }

    public int ItemId { get; set; }

    public int LocationId { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ReservedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ConsumedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ReleasedQty { get; set; }

    public byte Status { get; set; } = 1; // 1=Active, 2=Consumed, 3=Released

    [MaxLength(300)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    [ForeignKey(nameof(VoucherDetailId))]
    public VoucherDetail? VoucherDetail { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }
}

