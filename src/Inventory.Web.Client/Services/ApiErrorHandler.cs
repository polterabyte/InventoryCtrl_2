using Inventory.Shared.DTOs;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Централизованный обработчик ошибок API
/// </summary>
public class ApiErrorHandler : IApiErrorHandler
{
    private readonly ILogger<ApiErrorHandler> _logger;

    public ApiErrorHandler(ILogger<ApiErrorHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ApiResponse<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        try
        {
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>();
                _logger.LogDebug("API request successful. Status: {StatusCode}", response.StatusCode);
                return new ApiResponse<T> { Success = true, Data = data };
            }

            return await HandleErrorResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling API response");
            return new ApiResponse<T> 
            { 
                Success = false, 
                ErrorMessage = "Failed to process API response"
            };
        }
    }

    public async Task<PagedApiResponse<T>> HandlePagedResponseAsync<T>(HttpResponseMessage response)
    {
        try
        {
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<PagedApiResponse<T>>();
                _logger.LogDebug("API paged request successful. Status: {StatusCode}", response.StatusCode);
                return data ?? new PagedApiResponse<T> { Success = false, ErrorMessage = "Failed to deserialize paged response" };
            }

            return await HandlePagedErrorResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling API paged response");
            return new PagedApiResponse<T> 
            { 
                Success = false, 
                ErrorMessage = "Failed to process API paged response"
            };
        }
    }

    public Task<ApiResponse<T>> HandleExceptionAsync<T>(Exception exception, string operation)
    {
        _logger.LogError(exception, "Exception in {Operation}", operation);
        
        var errorMessage = GetUserFriendlyErrorMessage(exception);
        
        return Task.FromResult(new ApiResponse<T> 
        { 
            Success = false, 
            ErrorMessage = errorMessage
        });
    }

    public Task<PagedApiResponse<T>> HandlePagedExceptionAsync<T>(Exception exception, string operation)
    {
        _logger.LogError(exception, "Exception in {Operation}", operation);
        
        var errorMessage = GetUserFriendlyErrorMessage(exception);
        
        return Task.FromResult(new PagedApiResponse<T> 
        { 
            Success = false, 
            ErrorMessage = errorMessage
        });
    }

    private async Task<ApiResponse<T>> HandleErrorResponseAsync<T>(HttpResponseMessage response)
    {
        var errorMessage = await GetErrorMessageAsync(response);
        var statusCode = response.StatusCode;
        
        _logger.LogWarning("API request failed. Status: {StatusCode}, Error: {Error}", statusCode, errorMessage);
        
        return new ApiResponse<T> 
        { 
            Success = false, 
            ErrorMessage = errorMessage
        };
    }

    private async Task<PagedApiResponse<T>> HandlePagedErrorResponseAsync<T>(HttpResponseMessage response)
    {
        var errorMessage = await GetErrorMessageAsync(response);
        var statusCode = response.StatusCode;
        
        _logger.LogWarning("API paged request failed. Status: {StatusCode}, Error: {Error}", statusCode, errorMessage);
        
        return new PagedApiResponse<T> 
        { 
            Success = false, 
            ErrorMessage = errorMessage
        };
    }

    private async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            
            // Попытка десериализовать как стандартный API ответ
            if (!string.IsNullOrEmpty(content))
            {
                try
                {
                    var apiError = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<object>>(content);
                    if (!string.IsNullOrEmpty(apiError?.ErrorMessage))
                    {
                        return apiError.ErrorMessage;
                    }
                }
                catch
                {
                    // Если не удалось десериализовать как ApiResponse, возвращаем как есть
                }
            }
            
            return content ?? GetDefaultErrorMessage(response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read error content from response");
            return GetDefaultErrorMessage(response.StatusCode);
        }
    }

    private string GetDefaultErrorMessage(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "Invalid request parameters",
            HttpStatusCode.Unauthorized => "Authentication required",
            HttpStatusCode.Forbidden => "Access denied",
            HttpStatusCode.NotFound => "Resource not found",
            HttpStatusCode.Conflict => "Resource conflict",
            HttpStatusCode.InternalServerError => "Internal server error",
            HttpStatusCode.ServiceUnavailable => "Service temporarily unavailable",
            HttpStatusCode.RequestTimeout => "Request timeout",
            _ => $"Request failed with status {statusCode}"
        };
    }

    private string GetUserFriendlyErrorMessage(Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx => "Network error occurred. Please check your connection.",
            TaskCanceledException => "Request was cancelled or timed out.",
            UnauthorizedAccessException => "Access denied. Please log in again.",
            ArgumentException => "Invalid request parameters.",
            InvalidOperationException => "Operation cannot be completed at this time.",
            _ => "An unexpected error occurred. Please try again later."
        };
    }
}
