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

        public Task<ApiResponse<bool>> ArchiveNotificationAsync(int notificationId)
            => Task.FromResult(new ApiResponse<bool> { Success = true, Data = true });

        public Task<ApiResponse<bool>> MarkAsReadAsync(int notificationId)
            => Task.FromResult(new ApiResponse<bool> { Success = true, Data = true });

        public Task<ApiResponse<bool>> SendBulkNotificationAsync(List<string> userIds, CreateNotificationRequest request)
            => Task.FromResult(new ApiResponse<bool> { Success = true, Data = true });
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateRequest_StartsInDraft_And_WritesHistory()
    {
        using var db = CreateDb();
        var service = new RequestService(db, new FakeNotificationService(), NullLogger<RequestService>.Instance);

        var req = await service.CreateRequestAsync("Test Request", "user-1", "desc");

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
        var service = new RequestService(db, new FakeNotificationService(), NullLogger<RequestService>.Instance);
        var req = await service.CreateRequestAsync("R", "creator");

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
        db.Products.Add(new Product { Id = 1, Name = "P1", SKU = "SKU1", Quantity = 0, UnitOfMeasureId = 1, CategoryId = 1, ManufacturerId = 1, ProductModelId = 1, ProductGroupId = 1, MinStock = 0, MaxStock = 100, CreatedAt = DateTime.UtcNow });
        db.Warehouses.Add(new Warehouse { Id = 1, Name = "Main" });
        await db.SaveChangesAsync();

        var service = new RequestService(db, new FakeNotificationService(), NullLogger<RequestService>.Instance);
        var req = await service.CreateRequestAsync("R", "creator");
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
        var service = new RequestService(db, new FakeNotificationService(), NullLogger<RequestService>.Instance);
        var req = await service.CreateRequestAsync("R", "creator");

        // Approve without submit should fail
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.ApproveAsync(req.Id, "approver");
        });
    }
}

