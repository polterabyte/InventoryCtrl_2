using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Inventory.API.Models;
using Inventory.API.Services;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;

namespace Inventory.UnitTests.Requests;

public class RequestServiceTests
{
    private class FakeNotificationService : INotificationService
    {
        public Task<ApiResponse<NotificationDto>> CreateNotificationAsync(CreateNotificationRequest request)
            => Task.FromResult(new ApiResponse<NotificationDto> { Success = true, Data = new NotificationDto { Title = request.Title, Message = request.Message } });

        public Task<ApiResponse<List<NotificationDto>>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20)
            => Task.FromResult(new ApiResponse<List<NotificationDto>> { Success = true, Data = new List<NotificationDto>() });

        public Task<ApiResponse<NotificationDto>> GetNotificationAsync(int notificationId)
            => Task.FromResult(new ApiResponse<NotificationDto> { Success = false });

        public Task<ApiResponse<bool>> MarkAsReadAsync(int notificationId, string userId)
            => Task.FromResult(new ApiResponse<bool> { Success = true, Data = true });

        public Task<ApiResponse<bool>> MarkAllAsReadAsync(string userId)
            => Task.FromResult(new ApiResponse<bool> { Success = true, Data = true });

        public Task<ApiResponse<bool>> ArchiveNotificationAsync(int notificationId, string userId)
            => Task.FromResult(new ApiResponse<bool> { Success = true, Data = true });

        public Task<ApiResponse<bool>> DeleteNotificationAsync(int notificationId, string userId)
            => Task.FromResult(new ApiResponse<bool> { Success = true, Data = true });

        public Task<ApiResponse<NotificationStatsDto>> GetNotificationStatsAsync(string userId)
            => Task.FromResult(new ApiResponse<NotificationStatsDto> { Success = true, Data = new NotificationStatsDto() });

        public Task<ApiResponse<List<NotificationPreferenceDto>>> GetUserPreferencesAsync(string userId)
            => Task.FromResult(new ApiResponse<List<NotificationPreferenceDto>> { Success = true, Data = new List<NotificationPreferenceDto>() });

        public Task<ApiResponse<NotificationPreferenceDto>> UpdatePreferenceAsync(string userId, UpdateNotificationPreferenceRequest request)
            => Task.FromResult(new ApiResponse<NotificationPreferenceDto> { Success = true, Data = new NotificationPreferenceDto() });

        public Task<ApiResponse<bool>> DeletePreferenceAsync(string userId, string eventType)
            => Task.FromResult(new ApiResponse<bool> { Success = true, Data = true });

        public Task<ApiResponse<List<NotificationRuleDto>>> GetNotificationRulesAsync()
            => Task.FromResult(new ApiResponse<List<NotificationRuleDto>> { Success = true, Data = new List<NotificationRuleDto>() });

        public Task<ApiResponse<NotificationRuleDto>> CreateNotificationRuleAsync(CreateNotificationRuleRequest request)
            => Task.FromResult(new ApiResponse<NotificationRuleDto> { Success = true, Data = new NotificationRuleDto() });

        public Task<ApiResponse<NotificationRuleDto>> UpdateNotificationRuleAsync(int ruleId, CreateNotificationRuleRequest request)
            => Task.FromResult(new ApiResponse<NotificationRuleDto> { Success = true, Data = new NotificationRuleDto() });

        public Task<ApiResponse<bool>> DeleteNotificationRuleAsync(int ruleId)
            => Task.FromResult(new ApiResponse<bool> { Success = true, Data = true });

        public Task<ApiResponse<bool>> ToggleNotificationRuleAsync(int ruleId)
            => Task.FromResult(new ApiResponse<bool> { Success = true, Data = true });

        public Task TriggerStockLowNotificationAsync(object product)
            => Task.CompletedTask;

        public Task TriggerStockOutNotificationAsync(object product)
            => Task.CompletedTask;

        public Task TriggerTransactionNotificationAsync(object transaction)
            => Task.CompletedTask;

        public Task TriggerSystemNotificationAsync(string title, string message, string userId, string? actionUrl = null)
            => Task.CompletedTask;

        public Task<ApiResponse<bool>> SendBulkNotificationAsync(List<string> userIds, CreateNotificationRequest request)
            => Task.FromResult(new ApiResponse<bool> { Success = true, Data = true });

        public Task<ApiResponse<bool>> CleanupExpiredNotificationsAsync()
            => Task.FromResult(new ApiResponse<bool> { Success = true, Data = true });
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task SeedProductAndWarehouseAsync(AppDbContext db)
    {
        if (!await db.Products.AnyAsync())
        {
            db.Products.Add(new Product
            {
                Id = 1,
                Name = "Test Product",
                SKU = "TP-001",
                CurrentQuantity = 0,
                UnitOfMeasureId = 1,
                CategoryId = 1,
                ManufacturerId = 1,
                ProductModelId = 1,
                ProductGroupId = 1,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!await db.Warehouses.AnyAsync())
        {
            db.Warehouses.Add(new Warehouse
            {
                Id = 1,
                Name = "Main Warehouse"
            });
        }

        await db.SaveChangesAsync();
    }

    private static CreateRequestDto BuildCreateRequestDto(string title, string? description = null)
    {
        return new CreateRequestDto
        {
            Title = title,
            Description = description,
            Items = new List<RequestItemInputDto>
            {
                new()
                {
                    ProductId = 1,
                    WarehouseId = 1,
                    Quantity = 1
                }
            }
        };
    }

    [Fact]
    public async Task CreateRequest_StartsInDraft_And_WritesHistory()
    {
        using var db = CreateDb();
        await SeedProductAndWarehouseAsync(db);
        var service = new RequestService(db, new FakeNotificationService(), NullLogger<RequestService>.Instance);

        var req = await service.CreateRequestAsync(BuildCreateRequestDto("Test Request", "desc"), "user-1");

        Assert.NotNull(req);
        Assert.Equal(RequestStatus.Draft, req.Status);
        Assert.Equal("user-1", req.CreatedByUserId);

        // History entry persisted
        var hist = await db.RequestHistories.Where(h => h.RequestId == req.Id).ToListAsync();
        Assert.Single(hist);
        Assert.Equal(RequestStatus.Draft, hist[0].NewStatus);
    }

    [Fact]
    public async Task Submit_Then_Approve_ValidTransitions()
    {
        using var db = CreateDb();
        await SeedProductAndWarehouseAsync(db);
        var service = new RequestService(db, new FakeNotificationService(), NullLogger<RequestService>.Instance);
        var req = await service.CreateRequestAsync(BuildCreateRequestDto("R"), "creator");

        req = await service.SubmitAsync(req.Id, "creator");
        Assert.Equal(RequestStatus.Submitted, req.Status);

        req = await service.ApproveAsync(req.Id, "approver");
        Assert.Equal(RequestStatus.Approved, req.Status);
    }

    [Fact]
    public async Task ItemsReceived_Converts_Pending_To_Income()
    {
        using var db = CreateDb();
        // seed product/warehouse to satisfy FK-less in-memory model usage
        await SeedProductAndWarehouseAsync(db);

        var service = new RequestService(db, new FakeNotificationService(), NullLogger<RequestService>.Instance);
        var req = await service.CreateRequestAsync(BuildCreateRequestDto("R"), "creator");
        await service.AddPendingItemAsync(req.Id, 1, 1, 5, "creator");
        req = await service.SubmitAsync(req.Id, "creator");
        req = await service.ApproveAsync(req.Id, "approver");

        req = await service.MarkItemsReceivedAsync(req.Id, "receiver");
        Assert.Equal(RequestStatus.ItemsReceived, req.Status);

        var trxs = await db.InventoryTransactions.Where(t => t.RequestId == req.Id).ToListAsync();
        Assert.Single(trxs);
        Assert.Equal(TransactionType.Income, trxs[0].Type);
    }

    [Fact]
    public async Task InvalidTransition_Throws()
    {
        using var db = CreateDb();
        await SeedProductAndWarehouseAsync(db);
        var service = new RequestService(db, new FakeNotificationService(), NullLogger<RequestService>.Instance);
        var req = await service.CreateRequestAsync(BuildCreateRequestDto("R"), "creator");

        // Approve without submit should fail
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.ApproveAsync(req.Id, "approver");
        });
    }
}

