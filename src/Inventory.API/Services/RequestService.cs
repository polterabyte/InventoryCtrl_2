using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.Shared.Interfaces;
using Inventory.Shared.DTOs;
using Inventory.Shared.Models;

namespace Inventory.API.Services;

public interface IRequestService
{
    Task<Request> CreateRequestAsync(CreateRequestDto createRequest, string createdByUserId, CancellationToken ct = default);
    Task<Request> UpdateRequestAsync(int requestId, UpdateRequestDto updateRequest, string userId, CancellationToken ct = default);
    Task<Inventory.API.Models.InventoryTransaction> AddPendingItemAsync(int requestId, int productId, int warehouseId, int quantity, string userId, int? locationId = null, decimal? unitPrice = null, string? description = null, CancellationToken ct = default);

    Task<Request> SubmitAsync(int requestId, string userId, string? comment = null, CancellationToken ct = default);
    Task<Request> ApproveAsync(int requestId, string approverUserId, string? comment = null, CancellationToken ct = default);
    Task<Request> MarkItemsReceivedAsync(int requestId, string userId, string? comment = null, CancellationToken ct = default);
    Task<Request> MarkItemsInstalledAsync(int requestId, string userId, string? comment = null, CancellationToken ct = default);
    Task<Request> CompleteAsync(int requestId, string userId, string? comment = null, CancellationToken ct = default);
    Task<Request> CancelAsync(int requestId, string userId, string? comment = null, CancellationToken ct = default);
    Task<Request> RejectAsync(int requestId, string userId, string? comment = null, CancellationToken ct = default);
}

public class RequestService(AppDbContext db, INotificationService notificationService, ILogger<RequestService> logger) : IRequestService
{
    public async Task<Request> CreateRequestAsync(CreateRequestDto createRequest, string createdByUserId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(createRequest);

        if (string.IsNullOrWhiteSpace(createRequest.Title))
        {
            throw new ArgumentException("Request title is required", nameof(createRequest.Title));
        }

        if (createRequest.Items == null || createRequest.Items.Count == 0)
        {
            throw new ArgumentException("At least one item must be provided when creating a request", nameof(createRequest.Items));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var request = new Request
        {
            Title = createRequest.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(createRequest.Description) ? null : createRequest.Description.Trim(),
            Status = RequestStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        db.Requests.Add(request);
        await db.SaveChangesAsync(ct);

        foreach (var item in createRequest.Items)
        {
            await AddPendingItemInternalAsync(
                request.Id,
                item.ProductId,
                item.WarehouseId,
                item.Quantity,
                createdByUserId,
                item.LocationId,
                item.UnitPrice,
                item.Description,
                ct);
        }

        await AddHistoryAsync(request, null, RequestStatus.Draft, createdByUserId, "Request created", ct);

        await transaction.CommitAsync(ct);

        return request;
    }

    public async Task<Request> UpdateRequestAsync(int requestId, UpdateRequestDto updateRequest, string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(updateRequest);

        if (string.IsNullOrWhiteSpace(updateRequest.Title))
        {
            throw new ArgumentException("Request title is required", nameof(updateRequest.Title));
        }

        if (updateRequest.Items == null || updateRequest.Items.Count == 0)
        {
            throw new ArgumentException("At least one item must be provided when updating a request", nameof(updateRequest.Items));
        }

        var request = await db.Requests
            .Include(r => r.Transactions.Where(t => t.Type == TransactionType.Pending))
            .FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException("Request not found");

        EnsureStatus(request, [RequestStatus.Draft]);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        request.Title = updateRequest.Title.Trim();
        request.Description = string.IsNullOrWhiteSpace(updateRequest.Description) ? null : updateRequest.Description.Trim();
        request.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var existingPending = await db.InventoryTransactions
            .Where(t => t.RequestId == requestId && t.Type == TransactionType.Pending)
            .ToListAsync(ct);

        if (existingPending.Count > 0)
        {
            db.InventoryTransactions.RemoveRange(existingPending);
            await db.SaveChangesAsync(ct);
        }

        foreach (var item in updateRequest.Items)
        {
            await AddPendingItemInternalAsync(
                requestId,
                item.ProductId,
                item.WarehouseId,
                item.Quantity,
                userId,
                item.LocationId,
                item.UnitPrice,
                item.Description,
                ct);
        }

        await transaction.CommitAsync(ct);

        return request;
    }

    public async Task<Inventory.API.Models.InventoryTransaction> AddPendingItemAsync(int requestId, int productId, int warehouseId, int quantity, string userId, int? locationId = null, decimal? unitPrice = null, string? description = null, CancellationToken ct = default)
    {
        return await AddPendingItemInternalAsync(requestId, productId, warehouseId, quantity, userId, locationId, unitPrice, description, ct);
    }

    private async Task<Inventory.API.Models.InventoryTransaction> AddPendingItemInternalAsync(
        int requestId,
        int productId,
        int warehouseId,
        int quantity,
        string userId,
        int? locationId,
        decimal? unitPrice,
        string? description,
        CancellationToken ct)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        }

        var request = await db.Requests.FirstOrDefaultAsync(r => r.Id == requestId, ct) ?? throw new InvalidOperationException("Request not found");
        EnsureStatus(request, [RequestStatus.Draft, RequestStatus.Submitted, RequestStatus.Approved, RequestStatus.InProgress]);

        // Ensure related entities exist
        var productExists = await db.Products.AnyAsync(p => p.Id == productId, ct);
        if (!productExists)
        {
            throw new ArgumentException($"Product with id {productId} not found", nameof(productId));
        }

        var warehouseExists = await db.Warehouses.AnyAsync(w => w.Id == warehouseId, ct);
        if (!warehouseExists)
        {
            throw new ArgumentException($"Warehouse with id {warehouseId} not found", nameof(warehouseId));
        }

        if (locationId.HasValue)
        {
            var locationExists = await db.Locations.AnyAsync(l => l.Id == locationId.Value, ct);
            if (!locationExists)
            {
                throw new ArgumentException($"Location with id {locationId.Value} not found", nameof(locationId));
            }
        }

        var trx = new Inventory.API.Models.InventoryTransaction
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Type = TransactionType.Pending,
            Quantity = quantity,
            Date = DateTime.UtcNow,
            UserId = userId,
            LocationId = locationId,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            RequestId = requestId,
            UnitPrice = unitPrice,
            TotalPrice = unitPrice.HasValue ? unitPrice.Value * quantity : null
        };
        db.InventoryTransactions.Add(trx);
        await db.SaveChangesAsync(ct);
        return trx;
    }

    public async Task<Request> SubmitAsync(int requestId, string userId, string? comment = null, CancellationToken ct = default)
    {
        var req = await db.Requests.FirstOrDefaultAsync(r => r.Id == requestId, ct) ?? throw new InvalidOperationException("Request not found");
        EnsureStatus(req, [RequestStatus.Draft]);
        await ChangeStatusAsync(req, RequestStatus.Submitted, userId, comment, ct);
        return req;
    }

    public async Task<Request> ApproveAsync(int requestId, string approverUserId, string? comment = null, CancellationToken ct = default)
    {
        var req = await db.Requests.FirstOrDefaultAsync(r => r.Id == requestId, ct) ?? throw new InvalidOperationException("Request not found");
        EnsureStatus(req, [RequestStatus.Submitted]);
        req.ApprovedByUserId = approverUserId;
        await ChangeStatusAsync(req, RequestStatus.Approved, approverUserId, comment, ct);
        return req;
    }

    public async Task<Request> MarkItemsReceivedAsync(int requestId, string userId, string? comment = null, CancellationToken ct = default)
    {
        var req = await db.Requests
            .Include(r => r.Transactions)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct) ?? throw new InvalidOperationException("Request not found");
        EnsureStatus(req, [RequestStatus.Approved, RequestStatus.InProgress]);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Convert PENDING -> INCOME for all items of this request
        var pendings = await db.InventoryTransactions
            .Where(t => t.RequestId == requestId && t.Type == TransactionType.Pending)
            .ToListAsync(ct);
        foreach (var t in pendings)
        {
            t.Type = TransactionType.Income;
            t.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);

        await ChangeStatusAsync(req, RequestStatus.ItemsReceived, userId, comment ?? "Items received", ct);
        await tx.CommitAsync(ct);
        return req;
    }

    public async Task<Request> MarkItemsInstalledAsync(int requestId, string userId, string? comment = null, CancellationToken ct = default)
    {
        var req = await db.Requests.FirstOrDefaultAsync(r => r.Id == requestId, ct) ?? throw new InvalidOperationException("Request not found");
        EnsureStatus(req, [RequestStatus.ItemsReceived, RequestStatus.InProgress]);
        await ChangeStatusAsync(req, RequestStatus.ItemsInstalled, userId, comment ?? "Items installed", ct);
        return req;
    }

    public async Task<Request> CompleteAsync(int requestId, string userId, string? comment = null, CancellationToken ct = default)
    {
        var req = await db.Requests.FirstOrDefaultAsync(r => r.Id == requestId, ct) ?? throw new InvalidOperationException("Request not found");
        EnsureStatus(req, [RequestStatus.ItemsInstalled, RequestStatus.ItemsReceived, RequestStatus.InProgress]);
        await ChangeStatusAsync(req, RequestStatus.Completed, userId, comment, ct);
        return req;
    }

    public async Task<Request> CancelAsync(int requestId, string userId, string? comment = null, CancellationToken ct = default)
    {
        var req = await db.Requests.FirstOrDefaultAsync(r => r.Id == requestId, ct) ?? throw new InvalidOperationException("Request not found");
        EnsureStatus(req, [RequestStatus.Draft, RequestStatus.Submitted, RequestStatus.Approved, RequestStatus.InProgress]);
        await ChangeStatusAsync(req, RequestStatus.Cancelled, userId, comment, ct);
        return req;
    }

    public async Task<Request> RejectAsync(int requestId, string userId, string? comment = null, CancellationToken ct = default)
    {
        var req = await db.Requests.FirstOrDefaultAsync(r => r.Id == requestId, ct) ?? throw new InvalidOperationException("Request not found");
        EnsureStatus(req, [RequestStatus.Submitted]);
        await ChangeStatusAsync(req, RequestStatus.Rejected, userId, comment, ct);
        return req;
    }

    private static void EnsureStatus(Request req, IReadOnlyCollection<RequestStatus> allowed)
    {
        if (!allowed.Contains(req.Status))
            throw new InvalidOperationException($"Invalid status transition from {req.Status}");
    }

    private async Task AddHistoryAsync(Request req, RequestStatus? oldStatus, RequestStatus newStatus, string userId, string? comment, CancellationToken ct)
    {
        var h = new RequestHistory
        {
            RequestId = req.Id,
            OldStatus = oldStatus ?? newStatus,
            NewStatus = newStatus,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = userId,
            Comment = comment
        };
        db.RequestHistories.Add(h);
        await db.SaveChangesAsync(ct);
    }

    private async Task ChangeStatusAsync(Request req, RequestStatus newStatus, string userId, string? comment, CancellationToken ct)
    {
        var old = req.Status;
        req.Status = newStatus;
        req.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await AddHistoryAsync(req, old, newStatus, userId, comment, ct);

        // Emit notification to the request creator (and optionally approver)
        try
        {
            var title = $"Request #{req.Id} status changed: {old} в†’ {newStatus}";
            var message = string.IsNullOrWhiteSpace(comment)
                ? $"Request '{req.Title}' changed to {newStatus} by {userId}."
                : $"Request '{req.Title}' changed to {newStatus} by {userId}. {comment}";

            var create = new CreateNotificationRequest
            {
                Title = title,
                Message = message,
                Type = "INFO",
                Category = "REQUEST",
                UserId = req.CreatedByUserId
            };
            await notificationService.CreateNotificationAsync(create);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to emit request status notification for Request {RequestId}", req.Id);
        }
    }
}


