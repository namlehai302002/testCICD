using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("StockCountLines")]
public class StockCountLine
{
    [Key]
    public long StockCountLineId { get; set; }

    public long StockCountSheetId { get; set; }

    public int ItemId { get; set; }

    public int LocationId { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal SystemQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CountedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DiffQty { get; set; }

    [ForeignKey(nameof(StockCountSheetId))]
    public StockCountSheet? StockCountSheet { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }
}

