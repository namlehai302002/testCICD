using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("AppUsers")]
public class AppUser
{
    [Key]
    public int UserId { get; set; }

    [Required, MaxLength(100)]
    public string UserName { get; set; } = "";

    [Required, MaxLength(200)]
    public string FullName { get; set; } = "";

    [MaxLength(200)]
    public string? Email { get; set; }

    [Required, MaxLength(500)]
    public string PasswordHash { get; set; } = "";

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    public int? WarehouseId { get; set; }

    public int RoleId { get; set; } = 3;

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("WarehouseId")]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey("RoleId")]
    public AppRole? Role { get; set; }
}

[Table("AppRoles")]
public class AppRole
{
    [Key]
    public int RoleId { get; set; }

    [Required, MaxLength(50)]
    public string RoleName { get; set; } = "";

    [MaxLength(200)]
    public string? Description { get; set; }
}
