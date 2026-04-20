using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("Partners")]
public class Partner
{
    [Key]
    public int PartnerId { get; set; }

    [Required, MaxLength(20)]
    public string PartnerCode { get; set; } = "";

    [Required, MaxLength(200)]
    public string PartnerName { get; set; } = "";

    public byte PartnerType { get; set; } = 1; // 1=Supplier, 2=Customer, 3=Both

    [MaxLength(20)]
    public string? TaxCode { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? ContactPerson { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string PartnerTypeName => PartnerType switch
    {
        1 => "Nhà Cung Cấp",
        2 => "Khách Hàng",
        3 => "NCC + Khách Hàng",
        _ => "Khác"
    };
}
