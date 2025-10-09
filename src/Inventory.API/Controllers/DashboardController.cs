using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Inventory.API.Models;
using Inventory.Shared.DTOs;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController(AppDbContext context, ILogger<DashboardController> logger) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats()
    {
        try
        {
            logger.LogInformation("Getting dashboard statistics");
            
            var totalProducts = await context.Products.CountAsync(p => p.IsActive);
            var totalCategories = await context.Categories.CountAsync(c => c.IsActive);
            var totalManufacturers = await context.Manufacturers.CountAsync();
            var totalWarehouses = await context.Warehouses.CountAsync(w => w.IsActive);
            
            // Low stock products based on Kanban cards (counting distinct products)
            var lowStockKanbanCards = await BuildLowStockKanbanAsync();
            var lowStockProducts = lowStockKanbanCards
                .Select(card => card.ProductId)
                .Distinct()
                .Count();
            
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

            var stats = new DashboardStatsDto
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

            return Ok(ApiResponse<DashboardStatsDto>.SuccessResult(stats));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Database operation error while retrieving dashboard statistics");
            return StatusCode(500, ApiResponse<DashboardStatsDto>.ErrorResult("Database operation failed. Please try again."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving dashboard statistics");
            return StatusCode(500, ApiResponse<DashboardStatsDto>.ErrorResult("Failed to retrieve dashboard statistics"));
        }
    }

    [HttpGet("recent-activity")]
    public async Task<ActionResult<ApiResponse<RecentActivityDto>>> GetRecentActivity()
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
                .Select(t => new RecentTransactionDto
                {
                    Id = t.Id,
                    ProductName = t.Product.Name,
                    ProductSku = t.Product.SKU,
                    Type = t.Type.ToString(),
                    Quantity = t.Quantity,
                    Date = t.Date,
                    UserName = t.User.UserName ?? "N/A",
                    WarehouseName = t.Warehouse.Name,
                    Description = t.Description
                })
                .ToListAsync();

            // Recent products
            var recentProducts = await context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new RecentProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    SKU = p.SKU,
                    Quantity = 0, // Will be populated from ProductOnHandView
                    CategoryName = p.Category.Name,
                    ManufacturerName = null, // Manufacturer removed
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            var activity = new RecentActivityDto
            {
                RecentTransactions = recentTransactions,
                RecentProducts = recentProducts
            };

            logger.LogInformation("Recent activity retrieved successfully: {TransactionCount} transactions, {ProductCount} products", 
                recentTransactions.Count, recentProducts.Count);

            return Ok(ApiResponse<RecentActivityDto>.SuccessResult(activity));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Database operation error while retrieving recent activity");
            return StatusCode(500, ApiResponse<RecentActivityDto>.ErrorResult("Database operation failed. Please try again."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving recent activity");
            return StatusCode(500, ApiResponse<RecentActivityDto>.ErrorResult("Failed to retrieve recent activity"));
        }
    }

    [HttpGet("low-stock-products")]
    public async Task<ActionResult<ApiResponse<List<LowStockProductDto>>>> GetLowStockProducts()
    {
        try
        {
            logger.LogInformation("Getting low stock products");

            var lowStockKanbanCards = await BuildLowStockKanbanAsync();

            var groupedProducts = lowStockKanbanCards
                .GroupBy(card => card.ProductId)
                .Select(group =>
                {
                    var first = group.First();
                    return new LowStockProductDto
                    {
                        ProductId = first.ProductId,
                        ProductName = first.ProductName,
                        SKU = first.SKU,
                        CategoryName = first.CategoryName,
                        UnitOfMeasureSymbol = first.UnitOfMeasureSymbol,
                        KanbanCards = group.ToList()
                    };
                })
                .OrderBy(p => p.ProductName)
                .ToList();

            logger.LogInformation($"Low stock products retrieved successfully: {groupedProducts.Count} products");

            return Ok(ApiResponse<List<LowStockProductDto>>.SuccessResult(groupedProducts));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Database operation error while retrieving low stock products");
            return StatusCode(500, ApiResponse<List<LowStockProductDto>>.ErrorResult("Database operation failed. Please try again."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving low stock products");
            return StatusCode(500, ApiResponse<List<LowStockProductDto>>.ErrorResult("Failed to retrieve low stock products"));
        }
    }

[HttpGet("low-stock-kanban")]
    public async Task<ActionResult<ApiResponse<List<LowStockKanbanDto>>>> GetLowStockKanban()
    {
        try
        {
            logger.LogInformation("Getting low stock kanban cards");

            var result = await BuildLowStockKanbanAsync();

            logger.LogInformation("Low stock kanban retrieved: {Count} items", result.Count);
            return Ok(ApiResponse<List<LowStockKanbanDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving low stock kanban");
            return StatusCode(500, ApiResponse<List<LowStockKanbanDto>>.ErrorResult("Failed to retrieve low stock kanban"));
        }
    }



    private async Task<List<LowStockKanbanDto>> BuildLowStockKanbanAsync()
    {
        var kanbanCards = await context.KanbanCards
            .Where(k => k.Product.IsActive && k.Warehouse.IsActive)
            .Select(k => new
            {
                k.Id,
                k.ProductId,
                ProductName = k.Product.Name,
                k.Product.SKU,
                CategoryName = k.Product.Category.Name,
                k.WarehouseId,
                WarehouseName = k.Warehouse.Name,
                k.MinThreshold,
                k.MaxThreshold,
                UnitSymbol = k.Product.UnitOfMeasure.Symbol
            })
            .ToListAsync();

        if (kanbanCards.Count == 0)
        {
            return new List<LowStockKanbanDto>();
        }

        var productIds = kanbanCards.Select(k => k.ProductId).Distinct().ToList();
        var warehouseIds = kanbanCards.Select(k => k.WarehouseId).Distinct().ToList();

        var onHandLookup = await context.InventoryTransactions
            .Where(t => productIds.Contains(t.ProductId) && warehouseIds.Contains(t.WarehouseId))
            .GroupBy(t => new { t.ProductId, t.WarehouseId })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.WarehouseId,
                Quantity = g.Sum(t =>
                    t.Type == TransactionType.Income ? t.Quantity :
                    (t.Type == TransactionType.Outcome || t.Type == TransactionType.Install) ? -t.Quantity : 0)
            })
            .ToDictionaryAsync(x => new ValueTuple<int, int>(x.ProductId, x.WarehouseId), x => x.Quantity);

        return kanbanCards
            .Select(card =>
            {
                var key = (card.ProductId, card.WarehouseId);
                var quantity = onHandLookup.TryGetValue(key, out var value) ? value : 0;
                return new LowStockKanbanDto
                {
                    KanbanCardId = card.Id,
                    ProductId = card.ProductId,
                    ProductName = card.ProductName,
                    SKU = card.SKU,
                    CategoryName = card.CategoryName,
                    WarehouseId = card.WarehouseId,
                    WarehouseName = card.WarehouseName,
                    CurrentQuantity = quantity,
                    MinThreshold = card.MinThreshold,
                    MaxThreshold = card.MaxThreshold,
                    UnitOfMeasureSymbol = card.UnitSymbol
                };
            })
            .Where(dto => dto.CurrentQuantity <= dto.MinThreshold)
            .OrderBy(dto => dto.ProductName)
            .ThenBy(dto => dto.WarehouseName)
            .ToList();
    }

}



