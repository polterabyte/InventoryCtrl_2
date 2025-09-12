using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Models
{
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;

    public class AppDbContext : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<User, Microsoft.AspNetCore.Identity.IdentityRole, string>
    {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
           public DbSet<Product> Products { get; set; } = null!;
           public DbSet<Category> Categories { get; set; } = null!;
           public DbSet<Warehouse> Warehouses { get; set; } = null!;
           public DbSet<Location> Locations { get; set; } = null!;
           public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = null!;
           public DbSet<ProductHistory> ProductHistories { get; set; } = null!;
           public DbSet<Manufacturer> Manufacturers { get; set; } = null!;
           public DbSet<ProductModel> ProductModels { get; set; } = null!;
           public DbSet<ProductGroup> ProductGroups { get; set; } = null!;
    }
}
