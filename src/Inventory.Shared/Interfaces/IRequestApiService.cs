using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    Task<ApiResponse<RequestDetailsDto>> AddRequestItemAsync(int requestId, AddRequestItemDto addItemRequest);
    Task<ApiResponse<bool>> RemoveRequestItemAsync(int requestId, int itemId);
}

/// <summary>
/// DTO for creating a new request
/// </summary>
public class CreateRequestDto
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [MinLength(1, ErrorMessage = "At least one request item must be provided")]
    public ICollection<RequestItemInputDto> Items { get; set; } = new List<RequestItemInputDto>();
}

/// <summary>
/// DTO for updating an existing request
/// </summary>
public class UpdateRequestDto
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [MinLength(1, ErrorMessage = "At least one request item must be provided")]
    public ICollection<RequestItemInputDto> Items { get; set; } = new List<RequestItemInputDto>();
}

/// <summary>
/// DTO describing an item included in a request when creating or updating it
/// </summary>
public class RequestItemInputDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public int WarehouseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero")]
    public int Quantity { get; set; }

    public int? LocationId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// DTO for adding an item to a request
/// </summary>
public class AddRequestItemDto : RequestItemInputDto
{
}