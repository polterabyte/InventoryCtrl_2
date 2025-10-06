using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.API.Services;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RequestsController(AppDbContext db, IRequestService service, ILogger<RequestsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = db.Requests.AsNoTracking().OrderByDescending(r => r.CreatedAt);
        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RequestDto(
                r.Id,
                r.Title,
                r.Description,
                r.Status.ToString(),
                r.CreatedAt
            ))
            .ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dto = await BuildRequestDetailsAsync(id);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequestDto body)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
            var req = await service.CreateRequestAsync(body, userId);
            var details = await BuildRequestDetailsAsync(req.Id);
            return CreatedAtAction(nameof(GetById), new { id = req.Id }, details);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid request parameters for creating request");
            return BadRequest(new { success = false, errorMessage = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while creating request");
            return Conflict(new { success = false, errorMessage = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while creating request");
            return StatusCode(500, new { success = false, errorMessage = "An error occurred while creating the request. Please try again." });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRequestDto body)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
            await service.UpdateRequestAsync(id, body, userId);
            var details = await BuildRequestDetailsAsync(id);
            return Ok(details);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error while updating request {RequestId}", id);
            return BadRequest(new { success = false, errorMessage = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while updating request {RequestId}", id);
            if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { success = false, errorMessage = ex.Message });
            }

            return Conflict(new { success = false, errorMessage = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while updating request {RequestId}", id);
            return StatusCode(500, new { success = false, errorMessage = "An error occurred while updating the request. Please try again." });
        }
    }

    [HttpPost("{id}/items")]
    public async Task<IActionResult> AddItem(int id, [FromBody] AddRequestItemDto body)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
            await service.AddPendingItemAsync(id, body.ProductId, body.WarehouseId, body.Quantity, userId, body.LocationId, body.UnitPrice, body.Description);
            var details = await BuildRequestDetailsAsync(id);
            return Ok(details);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while adding item to request {RequestId}", id);
            return BadRequest(new { success = false, errorMessage = ex.Message });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid arguments while adding item to request {RequestId}", id);
            return BadRequest(new { success = false, errorMessage = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while adding item to request {RequestId}", id);
            return StatusCode(500, new { success = false, errorMessage = "An error occurred while adding the item. Please try again." });
        }
    }

    public record TransitionBody(string? Comment);

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(int id, [FromBody] TransitionBody body)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
            var req = await service.SubmitAsync(id, userId, body?.Comment);
            return Ok(req);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while submitting request {RequestId}", id);
            return BadRequest(new { success = false, errorMessage = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while submitting request {RequestId}", id);
            return StatusCode(500, new { success = false, errorMessage = "An error occurred while submitting the request. Please try again." });
        }
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] TransitionBody body)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
        var req = await service.ApproveAsync(id, userId, body?.Comment);
        return Ok(req);
    }

    [HttpPost("{id}/received")]
    public async Task<IActionResult> ItemsReceived(int id, [FromBody] TransitionBody body)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
        var req = await service.MarkItemsReceivedAsync(id, userId, body?.Comment);
        return Ok(req);
    }

    [HttpPost("{id}/installed")]
    public async Task<IActionResult> ItemsInstalled(int id, [FromBody] TransitionBody body)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
        var req = await service.MarkItemsInstalledAsync(id, userId, body?.Comment);
        return Ok(req);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(int id, [FromBody] TransitionBody body)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
        var req = await service.CompleteAsync(id, userId, body?.Comment);
        return Ok(req);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] TransitionBody body)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
        var req = await service.CancelAsync(id, userId, body?.Comment);
        return Ok(req);
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] TransitionBody body)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
        var req = await service.RejectAsync(id, userId, body?.Comment);
        return Ok(req);
    }

    private async Task<RequestDetailsDto?> BuildRequestDetailsAsync(int id)
    {
        return await db.Requests
            .Where(r => r.Id == id)
            .Select(r => new RequestDetailsDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                CreatedByUserId = r.CreatedByUserId,
                Items = r.Transactions
                    .Where(t => t.Type == TransactionType.Pending)
                    .OrderByDescending(t => t.Date)
                    .Select(t => new RequestItemDetailsDto
                    {
                        Id = t.Id,
                        ProductId = t.ProductId,
                        ProductName = t.Product.Name,
                        ProductSku = t.Product.SKU,
                        WarehouseId = t.WarehouseId,
                        WarehouseName = t.Warehouse.Name,
                        Quantity = t.Quantity,
                        LocationId = t.LocationId,
                        LocationName = t.Location != null ? t.Location.Name : null,
                        UnitPrice = t.UnitPrice,
                        TotalPrice = t.TotalPrice,
                        Description = t.Description
                    }).ToList(),
                Transactions = r.Transactions
                    .OrderByDescending(t => t.Date)
                    .Select(t => new Inventory.Shared.DTOs.TransactionRow
                    {
                        Type = t.Type.ToString(),
                        Quantity = t.Quantity,
                        Date = t.Date,
                        Description = t.Description,
                        ProductId = t.ProductId,
                        WarehouseId = t.WarehouseId
                    }).ToList(),
                History = r.History
                    .OrderByDescending(h => h.ChangedAt)
                    .Select(h => new Inventory.Shared.DTOs.HistoryRow
                    {
                        OldStatus = h.OldStatus.ToString(),
                        NewStatus = h.NewStatus.ToString(),
                        ChangedAt = h.ChangedAt,
                        ChangedByUserId = h.ChangedByUserId,
                        Comment = h.Comment
                    }).ToList()
            })
            .FirstOrDefaultAsync();
    }
}
