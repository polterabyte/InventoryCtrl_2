using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

/// <summary>
/// Interface for client-side request API operations
/// </summary>
public interface IRequestApiService
{
    // Request CRUD operations
    Task<PagedApiResponse<RequestDto>> GetPagedRequestsAsync(int page = 1, int pageSize = 20, string? search = null, string? status = null);
    Task<ApiResponse<RequestDetailsDto>> GetRequestByIdAsync(int requestId);
    Task<ApiResponse<RequestDetailsDto>> CreateRequestAsync(CreateRequestDto createRequest);
    Task<ApiResponse<RequestDetailsDto>> UpdateRequestAsync(int requestId, UpdateRequestDto updateRequest);
    Task<ApiResponse<bool>> DeleteRequestAsync(int requestId);
    
    // Request status transitions
    Task<ApiResponse<RequestDetailsDto>> SubmitRequestAsync(int requestId, string? comment = null);
    Task<ApiResponse<RequestDetailsDto>> ApproveRequestAsync(int requestId, string? comment = null);
    Task<ApiResponse<RequestDetailsDto>> MarkItemsReceivedAsync(int requestId, string? comment = null);
    Task<ApiResponse<RequestDetailsDto>> MarkItemsInstalledAsync(int requestId, string? comment = null);
    Task<ApiResponse<RequestDetailsDto>> CompleteRequestAsync(int requestId, string? comment = null);
    Task<ApiResponse<RequestDetailsDto>> CancelRequestAsync(int requestId, string? comment = null);
    Task<ApiResponse<RequestDetailsDto>> RejectRequestAsync(int requestId, string? comment = null);
    
    // Request item management
    Task<ApiResponse<TransactionRow>> AddRequestItemAsync(int requestId, AddRequestItemDto addItemRequest);
    Task<ApiResponse<bool>> RemoveRequestItemAsync(int requestId, int itemId);
}

/// <summary>
/// DTO for creating a new request
/// </summary>
public class CreateRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating an existing request
/// </summary>
public class UpdateRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// DTO for adding an item to a request
/// </summary>
public class AddRequestItemDto
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int Quantity { get; set; }
    public int? LocationId { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Description { get; set; }
}