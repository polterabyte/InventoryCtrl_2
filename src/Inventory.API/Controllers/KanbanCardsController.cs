using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.Shared.DTOs;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/kanban-cards")]
[Authorize]
public class KanbanCardsController(AppDbContext context, ILogger<KanbanCardsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<KanbanCardDto>>>> GetAll([FromQuery] int? productId = null, [FromQuery] int? warehouseId = null)
    {
        try
        {
            var query = context.KanbanCards
                .Include(k => k.Product).ThenInclude(p => p.UnitOfMeasure)
                .Include(k => k.Warehouse)
                .AsQueryable();

            if (productId.HasValue)
                query = query.Where(k => k.ProductId == productId.Value);
            if (warehouseId.HasValue)
                query = query.Where(k => k.WarehouseId == warehouseId.Value);

            var items = await query
                .OrderBy(k => k.Product.Name)
                .ThenBy(k => k.Warehouse.Name)
                 .Select(k => new KanbanCardDto
                 {
                     Id = k.Id,
                     ProductId = k.ProductId,
                     ProductName = k.Product.Name,
                     WarehouseId = k.WarehouseId,
                    WarehouseName = k.Warehouse.Name,
                    MinThreshold = k.MinThreshold,
                    MaxThreshold = k.MaxThreshold,
                    CurrentQuantity = 0,
                    UnitOfMeasureSymbol = k.Product.UnitOfMeasure.Symbol,
                    CreatedAt = k.CreatedAt,
                    UpdatedAt = k.UpdatedAt
                })
                .ToListAsync();

            // Compute current on-hand quantity for listed (ProductId, WarehouseId) pairs
            if (items.Count > 0)
            {
                var pairKeys = items
                    .Select(i => new { i.ProductId, i.WarehouseId })
                    .Distinct()
                    .ToList();

                var productIds = pairKeys.Select(p => p.ProductId).Distinct().ToList();
                var warehouseIds = pairKeys.Select(p => p.WarehouseId).Distinct().ToList();

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

                foreach (var item in items)
                {
                    var key = (item.ProductId, item.WarehouseId);
                    if (onHandLookup.TryGetValue(key, out var qty))
                    {
                        item.CurrentQuantity = qty;
                    }
                    else
                    {
                        item.CurrentQuantity = 0;
                    }
                }
            }

            return Ok(ApiResponse<List<KanbanCardDto>>.SuccessResult(items));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving Kanban cards");
            return StatusCode(500, ApiResponse<List<KanbanCardDto>>.ErrorResult("Failed to retrieve Kanban cards"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<KanbanCardDto>>> GetById(int id)
    {
        var entity = await context.KanbanCards
            .Include(k => k.Product).ThenInclude(p => p.UnitOfMeasure)
            .Include(k => k.Warehouse)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (entity == null)
            return NotFound(ApiResponse<KanbanCardDto>.ErrorResult("Kanban card not found"));

        var dto = new KanbanCardDto
        {
            Id = entity.Id,
            ProductId = entity.ProductId,
            ProductName = entity.Product.Name,
            WarehouseId = entity.WarehouseId,
            WarehouseName = entity.Warehouse.Name,
            MinThreshold = entity.MinThreshold,
            MaxThreshold = entity.MaxThreshold,
            CurrentQuantity = 0,
            UnitOfMeasureSymbol = entity.Product.UnitOfMeasure.Symbol,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

        // Compute on-hand for this card
        var onHand = await context.InventoryTransactions
            .Where(t => t.ProductId == dto.ProductId && t.WarehouseId == dto.WarehouseId)
            .GroupBy(t => 1)
            .Select(g => g.Sum(t =>
                t.Type == TransactionType.Income ? t.Quantity :
                (t.Type == TransactionType.Outcome || t.Type == TransactionType.Install) ? -t.Quantity : 0))
            .FirstOrDefaultAsync();
        dto.CurrentQuantity = onHand;

        return Ok(ApiResponse<KanbanCardDto>.SuccessResult(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<KanbanCardDto>>> Create([FromBody] CreateKanbanCardDto request)
    {
        try
        {
            // Validate existence
            var productExists = await context.Products.AnyAsync(p => p.Id == request.ProductId && p.IsActive);
            var warehouseExists = await context.Warehouses.AnyAsync(w => w.Id == request.WarehouseId && w.IsActive);
            if (!productExists || !warehouseExists)
            {
                return BadRequest(ApiResponse<KanbanCardDto>.ErrorResult("Invalid ProductId or WarehouseId"));
            }

            // Uniqueness check
            var exists = await context.KanbanCards.AnyAsync(k => k.ProductId == request.ProductId && k.WarehouseId == request.WarehouseId);
            if (exists)
            {
                return Conflict(ApiResponse<KanbanCardDto>.ErrorResult("Kanban card for this product and warehouse already exists"));
            }

            if (request.MinThreshold < 0 || request.MaxThreshold < request.MinThreshold)
            {
                return BadRequest(ApiResponse<KanbanCardDto>.ErrorResult("Invalid thresholds: ensure Max >= Min and Min >= 0"));
            }

            var entity = new KanbanCard
            {
                ProductId = request.ProductId,
                WarehouseId = request.WarehouseId,
                MinThreshold = request.MinThreshold,
                MaxThreshold = request.MaxThreshold,
                CreatedAt = DateTime.UtcNow
            };

            context.KanbanCards.Add(entity);
            await context.SaveChangesAsync();

            // Load for dto
            await context.Entry(entity).Reference(e => e.Product).LoadAsync();
            if (entity.Product != null)
            {
                await context.Entry(entity.Product).Reference(p => p.UnitOfMeasure).LoadAsync();
            }
            await context.Entry(entity).Reference(e => e.Warehouse).LoadAsync();

            var dto = new KanbanCardDto
            {
                Id = entity.Id,
                ProductId = entity.ProductId,
                ProductName = entity.Product.Name,
                WarehouseId = entity.WarehouseId,
                WarehouseName = entity.Warehouse.Name,
                MinThreshold = entity.MinThreshold,
                MaxThreshold = entity.MaxThreshold,
                UnitOfMeasureSymbol = entity.Product.UnitOfMeasure.Symbol,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };

            return Ok(ApiResponse<KanbanCardDto>.SuccessResult(dto));
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error creating Kanban card");
            return StatusCode(500, ApiResponse<KanbanCardDto>.ErrorResult("Database error"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating Kanban card");
            return StatusCode(500, ApiResponse<KanbanCardDto>.ErrorResult("Failed to create Kanban card"));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<KanbanCardDto>>> Update(int id, [FromBody] UpdateKanbanCardDto request)
    {
        try
        {
            var entity = await context.KanbanCards
                .Include(k => k.Product).ThenInclude(p => p.UnitOfMeasure)
                .Include(k => k.Warehouse)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (entity == null)
                return NotFound(ApiResponse<KanbanCardDto>.ErrorResult("Kanban card not found"));

            if (request.MinThreshold < 0 || request.MaxThreshold < request.MinThreshold)
            {
                return BadRequest(ApiResponse<KanbanCardDto>.ErrorResult("Invalid thresholds: ensure Max >= Min and Min >= 0"));
            }

            entity.MinThreshold = request.MinThreshold;
            entity.MaxThreshold = request.MaxThreshold;
            entity.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            var dto = new KanbanCardDto
            {
                Id = entity.Id,
                ProductId = entity.ProductId,
                ProductName = entity.Product.Name,
                WarehouseId = entity.WarehouseId,
                WarehouseName = entity.Warehouse.Name,
                MinThreshold = entity.MinThreshold,
                MaxThreshold = entity.MaxThreshold,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                UnitOfMeasureSymbol = entity.Product.UnitOfMeasure.Symbol
            };
            return Ok(ApiResponse<KanbanCardDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating Kanban card {Id}", id);
            return StatusCode(500, ApiResponse<KanbanCardDto>.ErrorResult("Failed to update Kanban card"));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        try
        {
            var entity = await context.KanbanCards.FindAsync(id);
            if (entity == null)
                return NotFound(ApiResponse<object>.ErrorResult("Kanban card not found"));

            context.KanbanCards.Remove(entity);
            await context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResult(new { message = "Kanban card deleted" }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting Kanban card {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to delete Kanban card"));
        }
    }
}
