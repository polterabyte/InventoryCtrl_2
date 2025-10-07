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
                .Include(k => k.Product)
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
                    SKU = k.Product.SKU,
                    WarehouseId = k.WarehouseId,
                    WarehouseName = k.Warehouse.Name,
                    MinThreshold = k.MinThreshold,
                    MaxThreshold = k.MaxThreshold,
                    CreatedAt = k.CreatedAt,
                    UpdatedAt = k.UpdatedAt
                })
                .ToListAsync();

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
            .Include(k => k.Product)
            .Include(k => k.Warehouse)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (entity == null)
            return NotFound(ApiResponse<KanbanCardDto>.ErrorResult("Kanban card not found"));

        var dto = new KanbanCardDto
        {
            Id = entity.Id,
            ProductId = entity.ProductId,
            ProductName = entity.Product.Name,
            SKU = entity.Product.SKU,
            WarehouseId = entity.WarehouseId,
            WarehouseName = entity.Warehouse.Name,
            MinThreshold = entity.MinThreshold,
            MaxThreshold = entity.MaxThreshold,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

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
            await context.Entry(entity).Reference(e => e.Warehouse).LoadAsync();

            var dto = new KanbanCardDto
            {
                Id = entity.Id,
                ProductId = entity.ProductId,
                ProductName = entity.Product.Name,
                SKU = entity.Product.SKU,
                WarehouseId = entity.WarehouseId,
                WarehouseName = entity.Warehouse.Name,
                MinThreshold = entity.MinThreshold,
                MaxThreshold = entity.MaxThreshold,
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
                .Include(k => k.Product)
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
                SKU = entity.Product.SKU,
                WarehouseId = entity.WarehouseId,
                WarehouseName = entity.Warehouse.Name,
                MinThreshold = entity.MinThreshold,
                MaxThreshold = entity.MaxThreshold,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
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