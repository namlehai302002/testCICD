using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("ItemLocations")]
public class ItemLocation
{
    [Key]
    public int ItemLocationId { get; set; }

    public int ItemId { get; set; }

    public int LocationId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ReservedQty { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? MaxCapacity { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? TotalCapacity { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [NotMapped]
    public decimal AvailableQty => Quantity - ReservedQty;

    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [ForeignKey("LocationId")]
    public Location? Location { get; set; }
}
