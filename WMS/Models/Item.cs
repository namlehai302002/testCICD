using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("Items")]
public class Item
{
    [Key]
    public int ItemId { get; set; }

    [Required, MaxLength(50)]
    public string ItemCode { get; set; } = "";

    [Required, MaxLength(200)]
    public string ItemName { get; set; } = "";

    [MaxLength(100)]
    public string? Barcode { get; set; }

    [MaxLength(50)]
    public string? SkuCode { get; set; }

    public int? CategoryId { get; set; }

    public byte ItemType { get; set; } = 1; // 1=Nguyên Vật Liệu, 6=Hóa chất

    public int BaseUomId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CurrentStock { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal MinThreshold { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? MaxThreshold { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? ReorderPoint { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Weight { get; set; } // Khối lượng đơn vị (kg) - dùng để tính sức chứa ô kho

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? LastCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalStockValue { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? Specifications { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    [ForeignKey("CategoryId")]
    public ItemCategory? Category { get; set; }

    [ForeignKey("BaseUomId")]
    public UnitOfMeasure? BaseUom { get; set; }

    public int? DefaultLocationId { get; set; }

    [ForeignKey("DefaultLocationId")]
    public Location? DefaultLocation { get; set; }

    public string ItemTypeName => ItemType switch
    {
        1 => "Nguyên Vật Liệu",
        6 => "Hóa chất",
        _ => "Khác"
    };

    public string StockStatus => CurrentStock switch
    {
        <= 0 => "Hết Hàng",
        _ when CurrentStock <= MinThreshold => "Sắp Hết",
        _ when MaxThreshold.HasValue && CurrentStock >= MaxThreshold.Value => "Vượt Định Mức",
        _ => "Bình Thường"
    };
}
