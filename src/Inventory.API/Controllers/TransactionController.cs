using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.API.Services;
using Inventory.Shared.DTOs;
using Serilog;
using System.Security.Claims;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IUserWarehouseService _userWarehouseService;
    private readonly ILogger<TransactionController> _logger;

    public TransactionController(
        AppDbContext context,
        IUserWarehouseService userWarehouseService,
        ILogger<TransactionController> logger)
    {
        _context = context;
        _userWarehouseService = userWarehouseService;
        _logger = logger;
    }
    [HttpGet]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? productId = null,
        [FromQuery] int? warehouseId = null,
        [FromQuery] string? type = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // Get current user information
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new PagedApiResponse<InventoryTransactionDto>
                {
                    Success = false,
                    ErrorMessage = "User not authenticated"
                });
            }

            // Get accessible warehouse IDs for the current user
            var accessibleWarehouseIds = await _userWarehouseService.GetAccessibleWarehouseIdsAsync(currentUserId, currentUserRole ?? "User");

            var query = _context.InventoryTransactions
                .Include(t => t.Product)
                .Include(t => t.Warehouse)
                .Include(t => t.User)
                .Include(t => t.Location)
                .Where(t => accessibleWarehouseIds.Contains(t.WarehouseId)) // Apply warehouse access control
                .AsQueryable();

            // Apply filters
            if (productId.HasValue)
            {
                query = query.Where(t => t.ProductId == productId.Value);
            }

            if (warehouseId.HasValue)
            {
                query = query.Where(t => t.WarehouseId == warehouseId.Value);
            }

            if (!string.IsNullOrEmpty(type))
            {
                if (Enum.TryParse<TransactionType>(type, out var transactionType))
                {
                    query = query.Where(t => t.Type == transactionType);
                }
            }

            if (startDate.HasValue)
            {
                query = query.Where(t => t.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.Date <= endDate.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var transactions = await query
                .OrderByDescending(t => t.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new InventoryTransactionDto
                {
                    Id = t.Id,
                    ProductId = t.ProductId,
                    ProductName = t.Product.Name,
                    ProductSku = t.Product.SKU,
                    WarehouseId = t.WarehouseId,
                    WarehouseName = t.Warehouse.Name,
                    UserId = t.UserId,
                    UserName = t.User.UserName ?? "Unknown",
                    LocationId = t.LocationId,
                    LocationName = t.Location != null ? t.Location.Name : null,
                    Type = t.Type.ToString(),
                    Quantity = t.Quantity,
                    Date = t.Date,
                    Description = t.Description
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<InventoryTransactionDto>
            {
                Items = transactions,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return Ok(new PagedApiResponse<InventoryTransactionDto>
            {
                Success = true,
                Data = pagedResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions");
            return StatusCode(500, new PagedApiResponse<InventoryTransactionDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve transactions"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransaction(int id)
    {
        try
        {
            // Get current user information
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new ApiResponse<InventoryTransactionDto>
                {
                    Success = false,
                    ErrorMessage = "User not authenticated"
                });
            }

            var transaction = await _context.InventoryTransactions
                .Include(t => t.Product)
                .Include(t => t.Warehouse)
                .Include(t => t.User)
                .Include(t => t.Location)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                return NotFound(new ApiResponse<InventoryTransactionDto>
                {
                    Success = false,
                    ErrorMessage = "Transaction not found"
                });
            }

            // Check if user has access to this warehouse
            var hasAccess = await _userWarehouseService.CheckWarehouseAccessAsync(currentUserId, transaction.WarehouseId);
            if (!hasAccess.HasAccess)
            {
                return Forbid();
            }

            var transactionDto = new InventoryTransactionDto
            {
                Id = transaction.Id,
                ProductId = transaction.ProductId,
                ProductName = transaction.Product.Name,
                ProductSku = transaction.Product.SKU,
                WarehouseId = transaction.WarehouseId,
                WarehouseName = transaction.Warehouse.Name,
                UserId = transaction.UserId,
                UserName = transaction.User.UserName ?? "Unknown",
                LocationId = transaction.LocationId,
                LocationName = transaction.Location?.Name,
                Type = transaction.Type.ToString(),
                Quantity = transaction.Quantity,
                Date = transaction.Date,
                Description = transaction.Description
            };

            return Ok(new ApiResponse<InventoryTransactionDto>
            {
                Success = true,
                Data = transactionDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction {TransactionId}", id);
            return StatusCode(500, new ApiResponse<InventoryTransactionDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve transaction"
            });
        }
    }

    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetTransactionsByProduct(
        int productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            // Get current user information
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new PagedApiResponse<InventoryTransactionDto>
                {
                    Success = false,
                    ErrorMessage = "User not authenticated"
                });
            }

            // Get accessible warehouse IDs for the current user
            var accessibleWarehouseIds = await _userWarehouseService.GetAccessibleWarehouseIdsAsync(currentUserId, currentUserRole ?? "User");

            var query = _context.InventoryTransactions
                .Include(t => t.Product)
                .Include(t => t.Warehouse)
                .Include(t => t.User)
                .Include(t => t.Location)
                .Where(t => t.ProductId == productId && accessibleWarehouseIds.Contains(t.WarehouseId)); // Apply warehouse access control

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var transactions = await query
                .OrderByDescending(t => t.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new InventoryTransactionDto
                {
                    Id = t.Id,
                    ProductId = t.ProductId,
                    ProductName = t.Product.Name,
                    ProductSku = t.Product.SKU,
                    WarehouseId = t.WarehouseId,
                    WarehouseName = t.Warehouse.Name,
                    UserId = t.UserId,
                    UserName = t.User.UserName ?? "Unknown",
                    LocationId = t.LocationId,
                    LocationName = t.Location != null ? t.Location.Name : null,
                    Type = t.Type.ToString(),
                    Quantity = t.Quantity,
                    Date = t.Date,
                    Description = t.Description
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<InventoryTransactionDto>
            {
                Items = transactions,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return Ok(new PagedApiResponse<InventoryTransactionDto>
            {
                Success = true,
                Data = pagedResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions for product {ProductId}", productId);
            return StatusCode(500, new PagedApiResponse<InventoryTransactionDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve transactions"
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateInventoryTransactionDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<InventoryTransactionDto>
                {
                    Success = false,
                    ErrorMessage = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            // Verify product exists
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return BadRequest(new ApiResponse<InventoryTransactionDto>
                {
                    Success = false,
                    ErrorMessage = "Product not found"
                });
            }

            // Verify warehouse exists
            var warehouse = await _context.Warehouses.FindAsync(request.WarehouseId);
            if (warehouse == null)
            {
                return BadRequest(new ApiResponse<InventoryTransactionDto>
                {
                    Success = false,
                    ErrorMessage = "Warehouse not found"
                });
            }

            // Verify location exists (if specified)
            if (request.LocationId.HasValue)
            {
                var location = await _context.Locations.FindAsync(request.LocationId.Value);
                if (location == null)
                {
                    return BadRequest(new ApiResponse<InventoryTransactionDto>
                    {
                        Success = false,
                        ErrorMessage = "Location not found"
                    });
                }
            }

            // Validate stock availability for Outcome transactions using ProductOnHandView
            if (request.Type == "Outcome")
            {
                var currentQuantity = await _context.ProductOnHand
                    .Where(v => v.ProductId == request.ProductId)
                    .Select(v => v.OnHandQty)
                    .FirstOrDefaultAsync();
                    
                if (currentQuantity < request.Quantity)
                {
                    return BadRequest(new ApiResponse<InventoryTransactionDto>
                    {
                        Success = false,
                        ErrorMessage = $"Insufficient stock for this transaction. Available: {currentQuantity}, Requested: {request.Quantity}"
                    });
                }
            }

            // We no longer modify Product.Quantity directly - it's computed from transactions
            product.UpdatedAt = DateTime.UtcNow;

            var transaction = new InventoryTransaction
            {
                ProductId = request.ProductId,
                WarehouseId = request.WarehouseId,
                UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system",
                LocationId = request.LocationId,
                Type = Enum.Parse<TransactionType>(request.Type),
                Quantity = request.Quantity,
                Date = request.Date ?? DateTime.UtcNow,
                Description = request.Description
            };

            _context.InventoryTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Transaction created: {TransactionType} for product {ProductId}, quantity {Quantity}", 
                request.Type, request.ProductId, request.Quantity);

            // Return the created transaction with related data
            var createdTransaction = await _context.InventoryTransactions
                .Include(t => t.Product)
                .Include(t => t.Warehouse)
                .Include(t => t.User)
                .Include(t => t.Location)
                .FirstAsync(t => t.Id == transaction.Id);

            var transactionDto = new InventoryTransactionDto
            {
                Id = createdTransaction.Id,
                ProductId = createdTransaction.ProductId,
                ProductName = createdTransaction.Product.Name,
                ProductSku = createdTransaction.Product.SKU,
                WarehouseId = createdTransaction.WarehouseId,
                WarehouseName = createdTransaction.Warehouse.Name,
                UserId = createdTransaction.UserId,
                UserName = createdTransaction.User.UserName ?? "Unknown",
                LocationId = createdTransaction.LocationId,
                LocationName = createdTransaction.Location?.Name,
                Type = createdTransaction.Type.ToString(),
                Quantity = createdTransaction.Quantity,
                Date = createdTransaction.Date,
                Description = createdTransaction.Description
            };

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, new ApiResponse<InventoryTransactionDto>
            {
                Success = true,
                Data = transactionDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction");
            return StatusCode(500, new ApiResponse<InventoryTransactionDto>
            {
                Success = false,
                ErrorMessage = "Failed to create transaction"
            });
        }
    }
}


