using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("Waves")]
public class Wave
{
    [Key]
    public long WaveId { get; set; }

    [Required, MaxLength(30)]
    public string WaveCode { get; set; } = "";

    public int WarehouseId { get; set; }

    public byte Status { get; set; } = 1; // 1=Draft,2=Released,3=Picking,4=Completed,5=Cancelled

    [MaxLength(300)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReleasedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    public ICollection<WaveLine> Lines { get; set; } = new List<WaveLine>();
}

