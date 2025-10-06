using System.Net.Http;
using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Web API service for request management operations
/// </summary>
public class WebRequestApiService : WebBaseApiService, IRequestApiService
{
    public WebRequestApiService(
        HttpClient httpClient,
        IUrlBuilderService urlBuilderService,
        IResilientApiService resilientApiService,
        IApiErrorHandler errorHandler,
        IRequestValidator requestValidator,
        ILogger<WebRequestApiService> logger)
        : base(httpClient, urlBuilderService, resilientApiService, errorHandler, requestValidator, logger)
    {
    }

    #region Request CRUD Operations

    public async Task<PagedApiResponse<RequestDto>> GetPagedRequestsAsync(int page = 1, int pageSize = 20, string? search = null, string? status = null)
    {
        try
        {
            var queryParams = new List<string>();
            
            if (page > 1) queryParams.Add($"page={page}");
            if (pageSize != 20) queryParams.Add($"pageSize={pageSize}");
            if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
            if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={Uri.EscapeDataString(status)}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var endpoint = $"{ApiEndpoints.Requests}{queryString}";

            Logger.LogDebug("Getting paged requests with endpoint: {Endpoint}", endpoint);
            return await GetPagedAsync<RequestDto>(endpoint);
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandlePagedExceptionAsync<RequestDto>(ex, "GetPagedRequests");
        }
    }

    public async Task<ApiResponse<RequestDetailsDto>> GetRequestByIdAsync(int requestId)
    {
        try
        {
            var endpoint = ApiEndpoints.RequestById.Replace("{id}", requestId.ToString());
            Logger.LogDebug("Getting request by ID: {RequestId}", requestId);

            return await GetAsync<RequestDetailsDto>(endpoint);
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<RequestDetailsDto>(ex, $"GetRequestById - {requestId}");
        }
    }

    public async Task<ApiResponse<RequestDetailsDto>> CreateRequestAsync(CreateRequestDto createRequest)
    {
        try
        {
            Logger.LogDebug("Creating new request with title: {Title}", createRequest.Title);
            return await PostAsync<RequestDetailsDto>(ApiEndpoints.Requests, createRequest);
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<RequestDetailsDto>(ex, "CreateRequest");
        }
    }

    public async Task<ApiResponse<RequestDetailsDto>> UpdateRequestAsync(int requestId, UpdateRequestDto updateRequest)
    {
        try
        {
            var endpoint = ApiEndpoints.RequestById.Replace("{id}", requestId.ToString());
            Logger.LogDebug("Updating request: {RequestId}", requestId);
            return await PutAsync<RequestDetailsDto>(endpoint, updateRequest);
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<RequestDetailsDto>(ex, $"UpdateRequest - {requestId}");
        }
    }

    public async Task<ApiResponse<bool>> DeleteRequestAsync(int requestId)
    {
        try
        {
            var endpoint = ApiEndpoints.RequestById.Replace("{id}", requestId.ToString());
            Logger.LogDebug("Deleting request: {RequestId}", requestId);
            return await DeleteAsync(endpoint);
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<bool>(ex, $"DeleteRequest - {requestId}");
        }
    }

    #endregion

    #region Request Status Transitions

    public async Task<ApiResponse<RequestDetailsDto>> SubmitRequestAsync(int requestId, string? comment = null)
    {
        try
        {
            var endpoint = ApiEndpoints.RequestSubmit.Replace("{id}", requestId.ToString());
            var body = new { Comment = comment };
            Logger.LogDebug("Submitting request: {RequestId}", requestId);
            return await PostAsync<RequestDetailsDto>(endpoint, body);
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<RequestDetailsDto>(ex, $"SubmitRequest - {requestId}");
        }
    }

    public async Task<ApiResponse<RequestDetailsDto>> ApproveRequestAsync(int requestId, string? comment = null)
    {
        try
        {
            var endpoint = ApiEndpoints.RequestApprove.Replace("{id}", requestId.ToString());
            var body = new { Comment = comment };
            Logger.LogDebug("Approving request: {RequestId}", requestId);
            return await PostAsync<RequestDetailsDto>(endpoint, body);
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<RequestDetailsDto>(ex, $"ApproveRequest - {requestId}");
        }
    }

    public async Task<ApiResponse<RequestDetailsDto>> MarkItemsReceivedAsync(int requestId, string? comment = null)
    {
        try
        {
            var endpoint = ApiEndpoints.RequestReceived.Replace("{id}", requestId.ToString());
            var body = new { Comment = comment };
            Logger.LogDebug("Marking items received for request: {RequestId}", requestId);
            return await PostAsync<RequestDetailsDto>(endpoint, body);
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<RequestDetailsDto>(ex, $"MarkItemsReceived - {requestId}");
        }
    }

    public async Task<ApiResponse<RequestDetailsDto>> MarkItemsInstalledAsync(int requestId, string? comment = null)
    {
        try
        {
            var endpoint = ApiEndpoints.RequestInstalled.Replace("{id}", requestId.ToString());
            var body = new { Comment = comment };
            Logger.LogDebug("Marking items installed for request: {RequestId}", requestId);
            return await PostAsync<RequestDetailsDto>(endpoint, body);
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<RequestDetailsDto>(ex, $"MarkItemsInstalled - {requestId}");
        }
    }

    public async Task<ApiResponse<RequestDetailsDto>> CompleteRequestAsync(int requestId, string? comment = null)
    {
        try
        {
            var endpoint = ApiEndpoints.RequestComplete.Replace("{id}", requestId.ToString());
            var body = new { Comment = comment };
            Logger.LogDebug("Completing request: {RequestId}", requestId);
            return await PostAsync<RequestDetailsDto>(endpoint, body);
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<RequestDetailsDto>(ex, $"CompleteRequest - {requestId}");
        }
    }

    public async Task<ApiResponse<RequestDetailsDto>> CancelRequestAsync(int requestId, string? comment = null)
    {
        try
        {
            var endpoint = ApiEndpoints.RequestCancel.Replace("{id}", requestId.ToString());
            var body = new { Comment = comment };
            Logger.LogDebug("Cancelling request: {RequestId}", requestId);
            return await PostAsync<RequestDetailsDto>(endpoint, body);
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<RequestDetailsDto>(ex, $"CancelRequest - {requestId}");
        }
    }

    public async Task<ApiResponse<RequestDetailsDto>> RejectRequestAsync(int requestId, string? comment = null)
    {
        try
        {
            var endpoint = ApiEndpoints.RequestReject.Replace("{id}", requestId.ToString());
            var body = new { Comment = comment };
            Logger.LogDebug("Rejecting request: {RequestId}", requestId);
            return await PostAsync<RequestDetailsDto>(endpoint, body);
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<RequestDetailsDto>(ex, $"RejectRequest - {requestId}");
        }
    }

    #endregion

    #region Request Item Management

    public async Task<ApiResponse<RequestDetailsDto>> AddRequestItemAsync(int requestId, AddRequestItemDto addItemRequest)
    {
        try
        {
            var endpoint = ApiEndpoints.RequestItems.Replace("{id}", requestId.ToString());
            Logger.LogDebug("Adding item to request: {RequestId}, ProductId: {ProductId}", requestId, addItemRequest.ProductId);
            return await PostAsync<RequestDetailsDto>(endpoint, addItemRequest);
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<RequestDetailsDto>(ex, $"AddRequestItem - {requestId}");
        }
    }

    public async Task<ApiResponse<bool>> RemoveRequestItemAsync(int requestId, int itemId)
    {
        try
        {
            var endpoint = $"{ApiEndpoints.RequestItems.Replace("{id}", requestId.ToString())}/{itemId}";
            Logger.LogDebug("Removing item from request: {RequestId}, ItemId: {ItemId}", requestId, itemId);
            return await DeleteAsync(endpoint);
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<bool>(ex, $"RemoveRequestItem - {requestId}/{itemId}");
        }
    }

    #endregion
}