using Microsoft.EntityFrameworkCore;
using Inventory.Shared.Models;

namespace Inventory.API.Models
{
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;

    public class AppDbContext : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<User, Microsoft.AspNetCore.Identity.IdentityRole, string>
    {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ProductTag
        modelBuilder.Entity<ProductTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.OldValues).HasMaxLength(4000);
            entity.Property(e => e.NewValues).HasMaxLength(4000);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Configure relationship with User
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure UnitOfMeasure
        modelBuilder.Entity<UnitOfMeasure>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Unique constraint on Symbol
            entity.HasIndex(e => e.Symbol).IsUnique();
        });

        // Configure Product-ProductTag many-to-many relationship
        modelBuilder.Entity<Product>()
            .HasMany(p => p.ProductTags)
            .WithMany(pt => pt.Products)
            .UsingEntity(j => j.ToTable("ProductProductTags"));

        // Configure Notification
        modelBuilder.Entity<DbNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ActionUrl).HasMaxLength(100);
            entity.Property(e => e.ActionText).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Configure relationships
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.SetNull);
                  
            entity.HasOne(e => e.Transaction)
                  .WithMany()
                  .HasForeignKey(e => e.TransactionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure NotificationRule
        modelBuilder.Entity<NotificationRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.NotificationType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Condition).IsRequired();
            entity.Property(e => e.Template).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure NotificationPreference
        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Configure relationship with User
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            // Unique constraint on UserId + EventType
            entity.HasIndex(e => new { e.UserId, e.EventType }).IsUnique();
        });

        // Configure NotificationTemplate
        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SubjectTemplate).IsRequired();
            entity.Property(e => e.BodyTemplate).IsRequired();
            entity.Property(e => e.NotificationType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure UserWarehouse
        modelBuilder.Entity<UserWarehouse>(entity =>
        {
            // Composite primary key
            entity.HasKey(e => new { e.UserId, e.WarehouseId });
            
            // Properties configuration
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.WarehouseId).IsRequired();
            entity.Property(e => e.AccessLevel).IsRequired().HasMaxLength(20).HasDefaultValue("Full");
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Foreign key relationships
            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserWarehouses)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Warehouse)
                  .WithMany(w => w.UserWarehouses)
                  .HasForeignKey(e => e.WarehouseId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes for performance
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_UserWarehouses_UserId");
            entity.HasIndex(e => e.WarehouseId).HasDatabaseName("IX_UserWarehouses_WarehouseId");
            entity.HasIndex(e => new { e.UserId, e.IsDefault })
                  .HasDatabaseName("IX_UserWarehouses_UserId_IsDefault")
                  .HasFilter("IsDefault = true"); // Partial index for default warehouses only
        });

        // Keyless view mappings
        modelBuilder.Entity<ProductPendingView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_product_pending");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductName).HasColumnName("product_name");
            entity.Property(e => e.SKU).HasColumnName("sku");
            entity.Property(e => e.PendingQty).HasColumnName("pending_qty");
            entity.Property(e => e.FirstPendingDate).HasColumnName("first_pending_date");
            entity.Property(e => e.LastPendingDate).HasColumnName("last_pending_date");
        });

        modelBuilder.Entity<ProductOnHandView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_product_on_hand");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductName).HasColumnName("product_name");
            entity.Property(e => e.SKU).HasColumnName("sku");
            entity.Property(e => e.OnHandQty).HasColumnName("on_hand_qty");
        });

        modelBuilder.Entity<ProductInstalledView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_product_installed");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductName).HasColumnName("product_name");
            entity.Property(e => e.SKU).HasColumnName("sku");
            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.LocationName).HasColumnName("location_name");
            entity.Property(e => e.InstalledQty).HasColumnName("installed_qty");
            entity.Property(e => e.FirstInstallDate).HasColumnName("first_install_date");
            entity.Property(e => e.LastInstallDate).HasColumnName("last_install_date");
        });

        // PushSubscription removed with VAPID/Web Push
    }
           public DbSet<Request> Requests { get; set; } = null!;
           public DbSet<RequestHistory> RequestHistories { get; set; } = null!;
           public DbSet<Product> Products { get; set; } = null!;
           public DbSet<Category> Categories { get; set; } = null!;
           public DbSet<Warehouse> Warehouses { get; set; } = null!;
           public DbSet<Location> Locations { get; set; } = null!;
           public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = null!;
           public DbSet<ProductHistory> ProductHistories { get; set; } = null!;
           public DbSet<Manufacturer> Manufacturers { get; set; } = null!;
           public DbSet<ProductModel> ProductModels { get; set; } = null!;
           public DbSet<ProductGroup> ProductGroups { get; set; } = null!;
           public DbSet<ProductTag> ProductTags { get; set; } = null!;
           public DbSet<UnitOfMeasure> UnitOfMeasures { get; set; } = null!;
           public DbSet<UserWarehouse> UserWarehouses { get; set; } = null!;
           public DbSet<AuditLog> AuditLogs { get; set; } = null!;
           public DbSet<DbNotification> Notifications { get; set; } = null!;
           public DbSet<NotificationRule> NotificationRules { get; set; } = null!;
           public DbSet<NotificationPreference> NotificationPreferences { get; set; } = null!;
           public DbSet<NotificationTemplate> NotificationTemplates { get; set; } = null!;
           // Views
           public DbSet<ProductPendingView> ProductPending { get; set; } = null!;
           public DbSet<ProductOnHandView> ProductOnHand { get; set; } = null!;
           public DbSet<ProductInstalledView> ProductInstalled { get; set; } = null!;
           public DbSet<SignalRConnection> SignalRConnections { get; set; } = null!;
           // public DbSet<PushSubscription> PushSubscriptions { get; set; } = null!;
    }
}
