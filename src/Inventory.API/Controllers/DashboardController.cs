using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.Shared.DTOs;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController(AppDbContext context, ILogger<DashboardController> logger) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        try
        {
            logger.LogInformation("Getting dashboard statistics");
            
            var totalProducts = await context.Products.CountAsync(p => p.IsActive);
            var totalCategories = await context.Categories.CountAsync(c => c.IsActive);
            var totalManufacturers = await context.Manufacturers.CountAsync();
            var totalWarehouses = await context.Warehouses.CountAsync(w => w.IsActive);
            
            // Low stock products (quantity <= MinStock) - using direct ProductOnHandView query
            var lowStockProducts = await (from oh in context.ProductOnHand
                                        join p in context.Products on oh.ProductId equals p.Id
                                        where p.IsActive && oh.OnHandQty <= p.MinStock
                                        select oh.ProductId)
                                        .CountAsync();
            
            // Out of stock products - using direct ProductOnHandView query
            var outOfStockProducts = await (from oh in context.ProductOnHand
                                          join p in context.Products on oh.ProductId equals p.Id
                                          where p.IsActive && oh.OnHandQty == 0
                                          select oh.ProductId)
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

            logger.LogInformation("Dashboard statistics retrieved successfully: {Stats}", 
                new { totalProducts, lowStockProducts, outOfStockProducts });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = stats
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Database operation error while retrieving dashboard statistics");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Database operation failed. Please try again."
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving dashboard statistics");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve dashboard statistics"
            });
        }
    }

    [HttpGet("recent-activity")]
    public async Task<IActionResult> GetRecentActivity()
    {
        try
        {
            logger.LogInformation("Getting recent activity data");

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
                    Quantity = 0, // Will be populated from ProductOnHandView
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

            logger.LogInformation("Recent activity retrieved successfully: {TransactionCount} transactions, {ProductCount} products", 
                recentTransactions.Count, recentProducts.Count);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = activity
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Database operation error while retrieving recent activity");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Database operation failed. Please try again."
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving recent activity");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve recent activity"
            });
        }
    }

    [HttpGet("low-stock-products")]
    public async Task<IActionResult> GetLowStockProducts()
    {
        try
        {
            logger.LogInformation("Getting low stock products");

            // Get products with their quantities from ProductOnHandView - optimized query
            var lowStockProducts = await (from oh in context.ProductOnHand
                                        join p in context.Products on oh.ProductId equals p.Id
                                        where p.IsActive && oh.OnHandQty <= p.MinStock
                                        orderby oh.OnHandQty
                                        select new LowStockProductDto
                                        {
                                            Id = p.Id,
                                            Name = p.Name,
                                            SKU = p.SKU,
                                            CurrentQuantity = oh.OnHandQty,
                                            MinStock = p.MinStock,
                                            MaxStock = p.MaxStock,
                                            CategoryName = p.Category.Name,
                                            ManufacturerName = p.Manufacturer.Name,
                                            UnitOfMeasureSymbol = p.UnitOfMeasure.Symbol
                                        })
                                        .ToListAsync();

            logger.LogInformation("Low stock products retrieved successfully: {Count} products", lowStockProducts.Count);

            return Ok(new ApiResponse<List<LowStockProductDto>>
            {
                Success = true,
                Data = lowStockProducts
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Database operation error while retrieving low stock products");
            return StatusCode(500, new ApiResponse<List<LowStockProductDto>>
            {
                Success = false,
                ErrorMessage = "Database operation failed. Please try again."
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving low stock products");
            return StatusCode(500, new ApiResponse<List<LowStockProductDto>>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve low stock products"
            });
        }
    }
}
