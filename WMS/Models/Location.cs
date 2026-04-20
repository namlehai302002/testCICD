using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("Locations")]
public class Location
{
    [Key]
    public int LocationId { get; set; }

    public int ZoneId { get; set; }

    [Required, MaxLength(50)]
    public string LocationCode { get; set; } = "";

    [MaxLength(20)]
    public string? RackCode { get; set; }

    [MaxLength(20)]
    public string? ShelfCode { get; set; }

    [MaxLength(20)]
    public string? BinCode { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CurrentLoad { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? Barcode { get; set; }

    [ForeignKey("ZoneId")]
    public Zone? Zone { get; set; }

    public virtual ICollection<ItemLocation> ItemLocations { get; set; } = new List<ItemLocation>();
}
