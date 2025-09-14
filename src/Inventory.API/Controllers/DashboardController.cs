using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController(AppDbContext context) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        try
        {
            var totalProducts = await context.Products.CountAsync(p => p.IsActive);
            var totalCategories = await context.Categories.CountAsync(c => c.IsActive);
            var totalManufacturers = await context.Manufacturers.CountAsync();
            var totalWarehouses = await context.Warehouses.CountAsync(w => w.IsActive);
            
            // Low stock products (quantity <= MinStock)
            var lowStockProducts = await context.Products
                .Where(p => p.IsActive && p.Quantity <= p.MinStock)
                .CountAsync();
            
            // Out of stock products
            var outOfStockProducts = await context.Products
                .Where(p => p.IsActive && p.Quantity == 0)
                .CountAsync();
            
            // Recent transactions (last 7 days)
            var recentTransactions = await context.InventoryTransactions
                .Where(t => t.Date >= DateTime.UtcNow.AddDays(-7))
                .CountAsync();
            
            // Recent products (last 30 days)
            var recentProducts = await context.Products
                .Where(p => p.IsActive && p.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .CountAsync();

            var stats = new
            {
                TotalProducts = totalProducts,
                TotalCategories = totalCategories,
                TotalManufacturers = totalManufacturers,
                TotalWarehouses = totalWarehouses,
                LowStockProducts = lowStockProducts,
                OutOfStockProducts = outOfStockProducts,
                RecentTransactions = recentTransactions,
                RecentProducts = recentProducts
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve dashboard statistics", details = ex.Message });
        }
    }

    [HttpGet("recent-activity")]
    public async Task<IActionResult> GetRecentActivity()
    {
        try
        {
            // Recent transactions with product details
            var recentTransactions = await context.InventoryTransactions
                .Include(t => t.Product)
                .Include(t => t.Warehouse)
                .Include(t => t.User)
                .Where(t => t.Date >= DateTime.UtcNow.AddDays(-7))
                .OrderByDescending(t => t.Date)
                .Take(10)
                .Select(t => new
                {
                    Id = t.Id,
                    ProductName = t.Product.Name,
                    ProductSku = t.Product.SKU,
                    Type = t.Type.ToString(),
                    Quantity = t.Quantity,
                    Date = t.Date,
                    UserName = t.User.UserName,
                    WarehouseName = t.Warehouse.Name,
                    Description = t.Description
                })
                .ToListAsync();

            // Recent products
            var recentProducts = await context.Products
                .Include(p => p.Category)
                .Include(p => p.Manufacturer)
                .Where(p => p.IsActive && p.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name,
                    SKU = p.SKU,
                    Quantity = p.Quantity,
                    CategoryName = p.Category.Name,
                    ManufacturerName = p.Manufacturer.Name,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            var activity = new
            {
                RecentTransactions = recentTransactions,
                RecentProducts = recentProducts
            };

            return Ok(activity);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve recent activity", details = ex.Message });
        }
    }

    [HttpGet("low-stock-products")]
    public async Task<IActionResult> GetLowStockProducts()
    {
        try
        {
            var lowStockProducts = await context.Products
                .Include(p => p.Category)
                .Include(p => p.Manufacturer)
                .Where(p => p.IsActive && p.Quantity <= p.MinStock)
                .OrderBy(p => p.Quantity)
                .Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name,
                    SKU = p.SKU,
                    CurrentQuantity = p.Quantity,
                    MinStock = p.MinStock,
                    MaxStock = p.MaxStock,
                    CategoryName = p.Category.Name,
                    ManufacturerName = p.Manufacturer.Name,
                    Unit = p.Unit
                })
                .ToListAsync();

            return Ok(lowStockProducts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve low stock products", details = ex.Message });
        }
    }
}
