using Inventory.Shared.DTOs;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Inventory.Shared.Interfaces;
using Radzen;
using System.Text.Json;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Централизованный обработчик ошибок API
/// </summary>
public class ApiErrorHandler : IApiErrorHandler
{
    private readonly ILogger<ApiErrorHandler> _logger;
    private readonly ITokenManagementService _tokenManagementService;
    private readonly NavigationManager _navigationManager;
    private readonly NotificationService _notificationService;

    public ApiErrorHandler(
        ILogger<ApiErrorHandler> logger,
        ITokenManagementService tokenManagementService,
        NavigationManager navigationManager,
        NotificationService notificationService)
    {
        _logger = logger;
        _tokenManagementService = tokenManagementService;
        _navigationManager = navigationManager;
        _notificationService = notificationService;
    }

    public async Task<ApiResponse<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        try
        {
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var data = await response.Content.ReadFromJsonAsync<T>();
                    _logger.LogDebug("API request successful. Status: {StatusCode}", response.StatusCode);

                    if (data == null)
                    {
                        // Если T - это bool, и мы не можем десериализовать, предположим, что это успешная операция DELETE
                        if (typeof(T) == typeof(bool))
                        {
                            return ApiResponse<T>.SuccessResult((T)(object)true);
                        }
                        _logger.LogWarning("Deserialized data is null for type {Type}, but a value was expected.", typeof(T).Name);
                        return ApiResponse<T>.ErrorResult($"Failed to deserialize response for {typeof(T).Name}. Content was empty or null.");
                    }
                    return ApiResponse<T>.SuccessResult(data);
                }
                catch (System.Text.Json.JsonException jsonEx)
                {
                    _logger.LogWarning(jsonEx, "Failed to deserialize response as {Type}. Response content: {Content}", 
                        typeof(T).Name, await response.Content.ReadAsStringAsync());
                    
                    // For DELETE operations, if we can't deserialize, assume success
                    if (typeof(T) == typeof(bool))
                    {
                        _logger.LogInformation("Assuming success for boolean response that couldn't be deserialized");
                        return ApiResponse<T>.SuccessResult((T)(object)true);
                    }
                    
                    return ApiResponse<T>.ErrorResult("Failed to deserialize response data");
                }
            }

            return await HandleErrorResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling API response");
            return ApiResponse<T>.ErrorResult("Failed to process API response");
        }
    }

    public async Task<PagedApiResponse<T>> HandlePagedResponseAsync<T>(HttpResponseMessage response)
    {
        try
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var serializerOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                try
                {
                    var wrappedResponse = JsonSerializer.Deserialize<PagedApiResponse<T>>(responseContent, serializerOptions);
                    if (wrappedResponse != null && (wrappedResponse.Success || wrappedResponse.Data != null))
                    {
                        _logger.LogDebug("API paged request successful (wrapped format). Status: {StatusCode}", response.StatusCode);
                        return wrappedResponse;
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogDebug(jsonEx, "Failed to deserialize wrapped paged response. Falling back to legacy format.");
                }

                try
                {
                    var legacyResponse = JsonSerializer.Deserialize<PagedResponse<T>>(responseContent, serializerOptions);
                    if (legacyResponse != null)
                    {
                        _logger.LogDebug("API paged request successful (legacy format). Status: {StatusCode}", response.StatusCode);
                        return PagedApiResponse<T>.CreateSuccess(legacyResponse);
                    }
                }
                catch (JsonException legacyEx)
                {
                    _logger.LogWarning(legacyEx, "Failed to deserialize legacy paged response format.");
                }

                _logger.LogWarning("Paged response deserialization failed for type {Type}. Content: {Content}", typeof(T).Name, responseContent);
                return PagedApiResponse<T>.CreateFailure("Failed to deserialize paged response");
            }

            return await HandlePagedErrorResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling API paged response");
            return PagedApiResponse<T>.CreateFailure("Failed to process API paged response");
        }
    }

    public Task<ApiResponse<T>> HandleExceptionAsync<T>(Exception exception, string operation)
    {
        _logger.LogError(exception, "Exception in {Operation}", operation);
        
        var errorMessage = GetUserFriendlyErrorMessage(exception);
        
        return Task.FromResult(ApiResponse<T>.ErrorResult(errorMessage));
    }

    public Task<PagedApiResponse<T>> HandlePagedExceptionAsync<T>(Exception exception, string operation)
    {
        _logger.LogError(exception, "Exception in {Operation}", operation);
        
        var errorMessage = GetUserFriendlyErrorMessage(exception);
        
        return Task.FromResult(PagedApiResponse<T>.CreateFailure(errorMessage));
    }

    private async Task<ApiResponse<T>> HandleErrorResponseAsync<T>(HttpResponseMessage response)
    {
        var errorMessage = await GetErrorMessageAsync(response);
        var statusCode = response.StatusCode;
        
        _logger.LogWarning("API request failed. Status: {StatusCode}, Error: {Error}", statusCode, errorMessage);
        
        // Enhanced error handling based on status code
        switch (statusCode)
        {
            case HttpStatusCode.Unauthorized:
                return await HandleUnauthorizedResponseAsync<T>(response);
                
            case HttpStatusCode.InternalServerError:
                // For 500 errors, log but don't redirect to login
                // These are server errors, not authentication issues
                _logger.LogError("Server error occurred: {Error}", errorMessage);
                _notificationService.Notify(new Radzen.NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Server Error",
                    Detail = "A server error occurred. Please try again in a few moments.",
                    Duration = 5000
                });
                break;
                
            case HttpStatusCode.BadRequest:
                _logger.LogWarning("Bad request: {Error}", errorMessage);
                _notificationService.Notify(new Radzen.NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Invalid Request",
                    Detail = errorMessage,
                    Duration = 4000
                });
                break;
                
            case HttpStatusCode.Forbidden:
                _logger.LogWarning("Access forbidden: {Error}", errorMessage);
                _notificationService.Notify(new Radzen.NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Access Denied",
                    Detail = "You don't have permission to perform this action.",
                    Duration = 4000
                });
                break;
                
            case HttpStatusCode.NotFound:
                _logger.LogWarning("Resource not found: {Error}", errorMessage);
                _notificationService.Notify(new Radzen.NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Not Found",
                    Detail = "The requested resource was not found.",
                    Duration = 4000
                });
                break;
                
            case HttpStatusCode.Conflict:
                _logger.LogWarning("Conflict error: {Error}", errorMessage);
                _notificationService.Notify(new Radzen.NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Conflict",
                    Detail = errorMessage,
                    Duration = 4000
                });
                break;
        }
        
        return ApiResponse<T>.ErrorResult(errorMessage);
    }

    private async Task<PagedApiResponse<T>> HandlePagedErrorResponseAsync<T>(HttpResponseMessage response)
    {
        var errorMessage = await GetErrorMessageAsync(response);
        var statusCode = response.StatusCode;
        
        _logger.LogWarning("API paged request failed. Status: {StatusCode}, Error: {Error}", statusCode, errorMessage);
        
        return PagedApiResponse<T>.CreateFailure(errorMessage);
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

    /// <summary>
    /// Handles 401 Unauthorized errors - token refresh is handled by JwtHttpInterceptor
    /// </summary>
    private async Task<ApiResponse<T>> HandleUnauthorizedResponseAsync<T>(HttpResponseMessage response)
    {
        _logger.LogInformation("Handling 401 Unauthorized response - token refresh should have been handled by interceptor");

        // Since the interceptor handles token refresh and retry, if we get here it means refresh failed
        // Just redirect to login
        await RedirectToLoginAsync("Session expired. Please log in again.");
        return CreateAuthFailureResponse<T>();
    }

    /// <summary>
    /// Redirects to login page with cleanup
    /// </summary>
    private async Task RedirectToLoginAsync(string message)
    {
        // Clear tokens
        await _tokenManagementService.ClearTokensAsync();
        
        // Show notification
        _notificationService.Notify(new Radzen.NotificationMessage
        {
            Severity = NotificationSeverity.Error,
            Summary = "Authentication Required",
            Detail = message,
            Duration = 5000
        });
        
        // Redirect to login page
        _navigationManager.NavigateTo("/login", true);
    }

    /// <summary>
    /// Creates a standardized authentication failure response
    /// </summary>
    private ApiResponse<T> CreateAuthFailureResponse<T>()
    {
        return ApiResponse<T>.ErrorResult("Authentication required. Please log in again.");
    }
}
