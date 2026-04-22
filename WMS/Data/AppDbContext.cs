using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WMS.Models;
using System.Text.Json;

namespace WMS.Data;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AppDbContext(DbContextOptions<AppDbContext> options)
    : base(options)
{
    _httpContextAccessor = null;
}

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // ═══════════════════════════════════════════════════════════════
    // Bảng cần theo dõi trong Audit Trail
    // ═══════════════════════════════════════════════════════════════
    private static readonly HashSet<string> _trackedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "Item", "Voucher", "VoucherDetail", "Warehouse", "Zone", "Location",
        "ItemLocation", "Partner", "ItemCategory", "PackagingUnit", "UnitOfMeasure",
        "UnitConversion", "AppUser", "AppRole", "StockReservation", "Wave", "WaveLine", "PickTask", "PickTaskScanLog"
    };

    // Thuộc tính nên bỏ qua khi so sánh (navigation properties, computed, etc.)
    private static readonly HashSet<string> _ignoredProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Voucher", "Item", "Location", "DestLocation", "TransactionUom", "PackagingUnit",
        "Category", "BaseUom", "ParentCategory", "ChildCategories", "Items", "Warehouse",
        "DestWarehouse", "Partner", "Details", "Zones", "Locations", "ItemLocations",
        "DefaultLocation", "Zone", "Role", "AiOcrLog", "FromUom", "ToUom",
        "ParentItem", "ChildItem", "Uom", "BaseUom", "PasswordHash"
    };

public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // ❌ KHÔNG dùng transaction khi InMemory
    if (Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
    {
        if (Database.CurrentTransaction == null)
        {
            await Database.BeginTransactionAsync(cancellationToken);
        }
    }

    var auditEntries = new List<AuditLog>();

    var httpContext = _httpContextAccessor?.HttpContext;
    var userName = httpContext?.User?.Identity?.Name ?? "system";
    var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "test";

    var entries = ChangeTracker.Entries()
        .Where(e => _trackedTables.Contains(e.Entity.GetType().Name)
                 && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
        .ToList();

    var insertEntries = entries.Where(e => e.State == EntityState.Added).ToList();

    foreach (var entry in entries)
    {
        var tableName = entry.Entity.GetType().Name;

        if (entry.State == EntityState.Deleted)
        {
            auditEntries.Add(new AuditLog
            {
                TableName = tableName,
                RecordId = GetPrimaryKeyValue(entry),
                ActionType = "DELETE",
                OldValue = SerializeDict(GetPropertyValues(entry, EntityState.Deleted)),
                ChangedBy = userName,
                ChangedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                AppModule = "EF_AutoAudit"
            });
        }
    }

    var result = await base.SaveChangesAsync(cancellationToken);

    foreach (var entry in insertEntries)
    {
        auditEntries.Add(new AuditLog
        {
            TableName = entry.Entity.GetType().Name,
            RecordId = GetPrimaryKeyValue(entry),
            ActionType = "INSERT",
            NewValue = SerializeDict(GetPropertyValues(entry, EntityState.Added)),
            ChangedBy = userName,
            ChangedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            AppModule = "EF_AutoAudit"
        });
    }

    if (auditEntries.Count > 0)
    {
        AuditLogs.AddRange(auditEntries);
        await base.SaveChangesAsync(cancellationToken);
    }

    if (Database.CurrentTransaction != null)
        await Database.CommitTransactionAsync(cancellationToken);

    return result;
}

    private static string GetPrimaryKeyValue(EntityEntry entry)
    {
        var keyProps = entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).ToList();
        if (keyProps.Count == 1)
            return keyProps[0].CurrentValue?.ToString() ?? "0";
        return string.Join("-", keyProps.Select(p => p.CurrentValue?.ToString() ?? "0"));
    }

    private Dictionary<string, object?> GetPropertyValues(EntityEntry entry, EntityState forState)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (_ignoredProperties.Contains(prop.Metadata.Name)) continue;
            var value = forState == EntityState.Deleted ? prop.OriginalValue : prop.CurrentValue;
            dict[prop.Metadata.Name] = value;
        }
        return dict;
    }

    private static string? SerializeDict(Dictionary<string, object?> dict)
    {
        if (dict.Count == 0) return null;
        try
        {
            return JsonSerializer.Serialize(dict, new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch
        {
            return JsonSerializer.Serialize(dict.ToDictionary(k => k.Key, k => k.Value?.ToString()));
        }
    }

    // Lookup & Reference
    public DbSet<ItemCategory> ItemCategories { get; set; }
    public DbSet<UnitOfMeasure> UnitsOfMeasure { get; set; }
    public DbSet<UnitConversion> UnitConversions { get; set; }
    public DbSet<PackagingUnit> PackagingUnits { get; set; }

    // Item Master
    public DbSet<Item> Items { get; set; }
    public DbSet<BillOfMaterial> BillOfMaterials { get; set; }

    // Warehouse Topology
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<Zone> Zones { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<ItemLocation> ItemLocations { get; set; }

    // Partners
    public DbSet<Partner> Partners { get; set; }

    // Vouchers
    public DbSet<Voucher> Vouchers { get; set; }
    public DbSet<VoucherDetail> VoucherDetails { get; set; }

    // Users & Roles
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<AppRole> AppRoles { get; set; }

    // Audit & AI
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<AiOcrLog> AiOcrLogs { get; set; }
    public DbSet<AiOcrAdjustment> AiOcrAdjustments { get; set; }

    // Stock
    public DbSet<StockSnapshot> StockSnapshots { get; set; }
    public DbSet<StockAlert> StockAlerts { get; set; }
    public DbSet<StockCountSheet> StockCountSheets { get; set; }
    public DbSet<StockCountLine> StockCountLines { get; set; }
    public DbSet<WarehousePeriodLock> WarehousePeriodLocks { get; set; }
    public DbSet<StockReservation> StockReservations { get; set; }
    public DbSet<Wave> Waves { get; set; }
    public DbSet<WaveLine> WaveLines { get; set; }
    public DbSet<PickTask> PickTasks { get; set; }
    public DbSet<PickTaskScanLog> PickTaskScanLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ItemCategory self-referencing
        modelBuilder.Entity<ItemCategory>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.ChildCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ItemCategory>()
            .HasIndex(c => c.CategoryCode).IsUnique();

        // UnitOfMeasure
        modelBuilder.Entity<UnitOfMeasure>()
            .HasIndex(u => u.UomCode).IsUnique();

        // UnitConversion
        // Item-specific conversion unique
        modelBuilder.Entity<UnitConversion>()
            .HasIndex(uc => new { uc.ItemId, uc.FromUomId, uc.ToUomId })
            .IsUnique()
            .HasFilter("[ItemId] IS NOT NULL");

        // Global conversion unique (ItemId = NULL)
        modelBuilder.Entity<UnitConversion>()
            .HasIndex(uc => new { uc.FromUomId, uc.ToUomId })
            .IsUnique()
            .HasFilter("[ItemId] IS NULL");

        modelBuilder.Entity<UnitConversion>()
            .HasOne(uc => uc.FromUom)
            .WithMany()
            .HasForeignKey(uc => uc.FromUomId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UnitConversion>()
            .HasOne(uc => uc.ToUom)
            .WithMany()
            .HasForeignKey(uc => uc.ToUomId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UnitConversion>()
            .HasOne(uc => uc.Item)
            .WithMany()
            .HasForeignKey(uc => uc.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        // PackagingUnit
        modelBuilder.Entity<PackagingUnit>()
            .HasIndex(p => p.TenDongGoi).IsUnique();

        modelBuilder.Entity<PackagingUnit>()
            .HasOne(p => p.BaseUom)
            .WithMany()
            .HasForeignKey(p => p.BaseUomId)
            .OnDelete(DeleteBehavior.NoAction);

        // Item
        modelBuilder.Entity<Item>()
            .HasIndex(i => i.ItemCode).IsUnique();

        modelBuilder.Entity<Item>()
            .HasOne(i => i.BaseUom)
            .WithMany()
            .HasForeignKey(i => i.BaseUomId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Item>()
            .HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // BOM
        modelBuilder.Entity<BillOfMaterial>()
            .HasOne(b => b.ParentItem)
            .WithMany()
            .HasForeignKey(b => b.ParentItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<BillOfMaterial>()
            .HasOne(b => b.ChildItem)
            .WithMany()
            .HasForeignKey(b => b.ChildItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<BillOfMaterial>()
            .HasOne(b => b.Uom)
            .WithMany()
            .HasForeignKey(b => b.UomId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<BillOfMaterial>()
            .HasIndex(b => new { b.ParentItemId, b.ChildItemId, b.EffectiveFrom }).IsUnique();

        // Warehouse
        modelBuilder.Entity<Warehouse>()
            .HasIndex(w => w.WarehouseCode).IsUnique();

        modelBuilder.Entity<Warehouse>()
            .HasOne(w => w.ManagerUser)
            .WithMany()
            .HasForeignKey(w => w.ManagerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Zone
        modelBuilder.Entity<Zone>()
            .HasIndex(z => new { z.WarehouseId, z.ZoneCode }).IsUnique();

        // Location
        modelBuilder.Entity<Location>()
            .HasIndex(l => l.LocationCode).IsUnique();

        // ItemLocation
        // ItemLocation uniqueness by batch:
        // SQL Server unique indexes allow multiple NULLs, so we use filtered unique indexes to prevent duplicates.
        // 1) No lot + no expiry => only ONE row per (ItemId, LocationId)
        modelBuilder.Entity<ItemLocation>()
            .HasIndex(il => new { il.ItemId, il.LocationId })
            .IsUnique()
            .HasFilter("[LotNumber] IS NULL AND [ExpiryDate] IS NULL");

        // 2) Lot present + expiry NULL => unique by (ItemId, LocationId, LotNumber)
        modelBuilder.Entity<ItemLocation>()
            .HasIndex(il => new { il.ItemId, il.LocationId, il.LotNumber })
            .IsUnique()
            .HasFilter("[LotNumber] IS NOT NULL AND [ExpiryDate] IS NULL");

        // 3) Lot NULL + expiry present => unique by (ItemId, LocationId, ExpiryDate)
        modelBuilder.Entity<ItemLocation>()
            .HasIndex(il => new { il.ItemId, il.LocationId, il.ExpiryDate })
            .IsUnique()
            .HasFilter("[LotNumber] IS NULL AND [ExpiryDate] IS NOT NULL");

        // 4) Lot present + expiry present => unique by full key
        modelBuilder.Entity<ItemLocation>()
            .HasIndex(il => new { il.ItemId, il.LocationId, il.LotNumber, il.ExpiryDate })
            .IsUnique()
            .HasFilter("[LotNumber] IS NOT NULL AND [ExpiryDate] IS NOT NULL");

        // Partner
        modelBuilder.Entity<Partner>()
            .HasIndex(p => p.PartnerCode).IsUnique();

        // Voucher
        modelBuilder.Entity<Voucher>()
            .HasIndex(v => v.VoucherCode).IsUnique();

        modelBuilder.Entity<Voucher>()
            .HasOne(v => v.Warehouse)
            .WithMany()
            .HasForeignKey(v => v.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Voucher>()
            .HasOne(v => v.DestWarehouse)
            .WithMany()
            .HasForeignKey(v => v.DestWarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Voucher>()
            .HasOne(v => v.Partner)
            .WithMany()
            .HasForeignKey(v => v.PartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Voucher>()
            .HasOne<Wave>()
            .WithMany()
            .HasForeignKey(v => v.WaveId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Voucher>()
            .Property(v => v.ReviewResult)
            .HasDefaultValue((byte)1);

        modelBuilder.Entity<Voucher>()
            .Property(v => v.ResponsibilityScore)
            .HasDefaultValue(0m);

        modelBuilder.Entity<Voucher>()
            .ToTable(tb => tb.HasCheckConstraint("CK_Vouchers_ResponsibilityScore_Range", "[ResponsibilityScore] >= 0 AND [ResponsibilityScore] <= 100"));

        // VoucherDetail
        modelBuilder.Entity<VoucherDetail>()
            .ToTable(tb => {
                tb.HasTrigger("TR_VoucherDetails_AfterInsert");
                tb.HasTrigger("TR_VoucherDetails_PreventModify");
            });

        modelBuilder.Entity<VoucherDetail>()
            .HasOne(vd => vd.Item)
            .WithMany()
            .HasForeignKey(vd => vd.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VoucherDetail>()
            .HasOne(vd => vd.Location)
            .WithMany()
            .HasForeignKey(vd => vd.LocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VoucherDetail>()
            .HasOne(vd => vd.DestLocation)
            .WithMany()
            .HasForeignKey(vd => vd.DestLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VoucherDetail>()
            .HasOne(vd => vd.TransactionUom)
            .WithMany()
            .HasForeignKey(vd => vd.TransactionUomId)
            .OnDelete(DeleteBehavior.NoAction);

        // AppUser
        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.UserName).IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasOne(u => u.Warehouse)
            .WithMany()
            .HasForeignKey(u => u.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AppUser>()
            .HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.NoAction);

        // AppRole
        modelBuilder.Entity<AppRole>()
            .HasIndex(r => r.RoleName).IsUnique();

        // StockSnapshot
        modelBuilder.Entity<StockSnapshot>()
            .HasIndex(s => new { s.SnapshotDate, s.ItemId, s.WarehouseId }).IsUnique();

        modelBuilder.Entity<StockSnapshot>()
            .HasOne(s => s.Item)
            .WithMany()
            .HasForeignKey(s => s.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockSnapshot>()
            .HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        // StockAlert
        modelBuilder.Entity<StockAlert>()
            .HasOne(a => a.Item)
            .WithMany()
            .HasForeignKey(a => a.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        // Stock count sheet/lines
        modelBuilder.Entity<StockCountSheet>()
            .HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockCountSheet>()
            .HasOne(s => s.GeneratedAdjustmentVoucher)
            .WithMany()
            .HasForeignKey(s => s.GeneratedAdjustmentVoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockCountLine>()
            .HasOne(l => l.StockCountSheet)
            .WithMany(s => s.Lines)
            .HasForeignKey(l => l.StockCountSheetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockCountLine>()
            .HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockCountLine>()
            .HasOne(l => l.Location)
            .WithMany()
            .HasForeignKey(l => l.LocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockCountLine>()
            .HasIndex(l => new { l.StockCountSheetId, l.ItemId, l.LocationId, l.LotNumber, l.ExpiryDate })
            .IsUnique();

        // Warehouse period lock
        modelBuilder.Entity<WarehousePeriodLock>()
            .HasOne(p => p.Warehouse)
            .WithMany()
            .HasForeignKey(p => p.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WarehousePeriodLock>()
            .HasIndex(p => new { p.WarehouseId, p.IsActive });

        // Reservation
        modelBuilder.Entity<StockReservation>()
            .HasOne(r => r.Voucher)
            .WithMany()
            .HasForeignKey(r => r.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockReservation>()
            .HasOne(r => r.VoucherDetail)
            .WithMany()
            .HasForeignKey(r => r.VoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockReservation>()
            .HasOne(r => r.Item)
            .WithMany()
            .HasForeignKey(r => r.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockReservation>()
            .HasOne(r => r.Location)
            .WithMany()
            .HasForeignKey(r => r.LocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockReservation>()
            .HasIndex(r => new { r.VoucherId, r.Status });

        modelBuilder.Entity<StockReservation>()
            .HasIndex(r => new { r.ItemId, r.LocationId, r.LotNumber, r.ExpiryDate, r.Status });

        modelBuilder.Entity<StockReservation>()
            .HasIndex(r => new { r.VoucherId, r.VoucherDetailId, r.ItemId, r.LocationId, r.LotNumber, r.ExpiryDate })
            .IsUnique()
            .HasFilter("[Status] = 1");

        // Wave + picking
        modelBuilder.Entity<Wave>()
            .HasOne(w => w.Warehouse)
            .WithMany()
            .HasForeignKey(w => w.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Wave>()
            .HasIndex(w => w.WaveCode)
            .IsUnique();

        modelBuilder.Entity<WaveLine>()
            .HasOne(wl => wl.Wave)
            .WithMany(w => w.Lines)
            .HasForeignKey(wl => wl.WaveId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WaveLine>()
            .HasOne(wl => wl.Voucher)
            .WithMany()
            .HasForeignKey(wl => wl.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WaveLine>()
            .HasOne(wl => wl.Item)
            .WithMany()
            .HasForeignKey(wl => wl.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WaveLine>()
            .HasIndex(wl => new { wl.WaveId, wl.VoucherId, wl.ItemId });

        modelBuilder.Entity<PickTask>()
            .HasOne(t => t.Wave)
            .WithMany()
            .HasForeignKey(t => t.WaveId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTask>()
            .HasOne(t => t.Voucher)
            .WithMany()
            .HasForeignKey(t => t.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTask>()
            .HasOne(t => t.VoucherDetail)
            .WithMany()
            .HasForeignKey(t => t.VoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTask>()
            .HasOne(t => t.Item)
            .WithMany()
            .HasForeignKey(t => t.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTask>()
            .HasOne(t => t.SourceLocation)
            .WithMany()
            .HasForeignKey(t => t.SourceLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTask>()
            .HasIndex(t => t.TaskCode)
            .IsUnique();

        modelBuilder.Entity<PickTask>()
            .HasIndex(t => new { t.WaveId, t.Status, t.AssignedTo });

        modelBuilder.Entity<PickTaskScanLog>()
            .HasOne(l => l.PickTask)
            .WithMany()
            .HasForeignKey(l => l.PickTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // AiOcrLog
        modelBuilder.Entity<AiOcrLog>()
            .HasOne(a => a.Voucher)
            .WithMany()
            .HasForeignKey(a => a.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        // AiOcrAdjustment
        modelBuilder.Entity<AiOcrAdjustment>()
            .HasOne(a => a.Item)
            .WithMany()
            .HasForeignKey(a => a.ItemId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
