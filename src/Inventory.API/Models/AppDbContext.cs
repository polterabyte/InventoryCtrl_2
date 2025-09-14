using Microsoft.EntityFrameworkCore;

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

        // Configure Product-ProductTag many-to-many relationship
        modelBuilder.Entity<Product>()
            .HasMany(p => p.ProductTags)
            .WithMany(pt => pt.Products)
            .UsingEntity(j => j.ToTable("ProductProductTags"));
    }
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
           public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    }
}
