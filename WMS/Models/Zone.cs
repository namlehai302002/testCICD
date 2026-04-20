using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("Zones")]
public class Zone
{
    [Key]
    public int ZoneId { get; set; }

    public int WarehouseId { get; set; }

    [Required, MaxLength(20)]
    public string ZoneCode { get; set; } = "";

    [Required, MaxLength(100)]
    public string ZoneName { get; set; } = "";

    public byte ZoneType { get; set; } = 1; // 1=Storage, 2=Receiving, 3=Shipping, 4=Quarantine, 5=Production

    public bool IsActive { get; set; } = true;

    [ForeignKey("WarehouseId")]
    public Warehouse? Warehouse { get; set; }

    public ICollection<Location> Locations { get; set; } = new List<Location>();

    public string ZoneTypeName => ZoneType switch
    {
        1 => "Lưu Trữ",
        2 => "Tiếp Nhận",
        3 => "Xuất Hàng",
        4 => "Cách Ly/QC",
        5 => "Sản Xuất",
        _ => "Khác"
    };
}
