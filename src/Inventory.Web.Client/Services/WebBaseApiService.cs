using System.Net.Http.Json;
using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Inventory.Web.Client.Services;

public abstract class WebBaseApiService(
    HttpClient httpClient, 
    IUrlBuilderService urlBuilderService, 
    IResilientApiService resilientApiService,
    IApiErrorHandler errorHandler,
    IRequestValidator requestValidator,
    IAutoTokenRefreshService autoTokenRefreshService,
    ILogger logger)
{
    protected readonly HttpClient HttpClient = httpClient;
    protected readonly IUrlBuilderService UrlBuilderService = urlBuilderService;
    protected readonly IResilientApiService ResilientApiService = resilientApiService;
    protected readonly IApiErrorHandler ErrorHandler = errorHandler;
    protected readonly IRequestValidator RequestValidator = requestValidator;
    protected readonly IAutoTokenRefreshService AutoTokenRefreshService = autoTokenRefreshService;
    protected readonly ILogger Logger = logger;

    public async Task<string> GetApiUrlAsync()
    {
        return await UrlBuilderService.BuildApiUrlAsync(string.Empty);
    }

    /// <summary>
    /// Р’Р°Р»РёРґРёСЂРѕРІР°С‚СЊ РѕР±СЉРµРєС‚ Р·Р°РїСЂРѕСЃР° РїРµСЂРµРґ РѕС‚РїСЂР°РІРєРѕР№
    /// </summary>
    protected async Task<ValidationResult> ValidateRequestAsync<T>(T request)
    {
        if (request == null)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    new() { PropertyName = "Request", Message = "Request object cannot be null" }
                }
            };
        }

        try
        {
            Logger.LogDebug("Validating request of type {Type}", typeof(T).Name);
            var result = await RequestValidator.ValidateAsync(request);
            
            if (!result.IsValid)
            {
                Logger.LogWarning("Request validation failed for {Type}. Errors: {Errors}", 
                    typeof(T).Name, result.Summary);
            }
            else
            {
                Logger.LogDebug("Request validation successful for {Type}", typeof(T).Name);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during request validation for {Type}", typeof(T).Name);
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    new() { PropertyName = "Validation", Message = "Validation error occurred", ErrorCode = "VALIDATION_ERROR" }
                }
            };
        }
    }

    /// <summary>
    /// Р’С‹РїРѕР»РЅРёС‚СЊ HTTP Р·Р°РїСЂРѕСЃ СЃ РІР°Р»РёРґР°С†РёРµР№
    /// </summary>
    protected async Task<ApiResponse<T>> ExecuteWithValidationAsync<T>(HttpMethod method, string endpoint, object? data = null)
    {
        // Р’Р°Р»РёРґРёСЂСѓРµРј РґР°РЅРЅС‹Рµ РµСЃР»Рё РѕРЅРё РµСЃС‚СЊ
        if (data != null)
        {
            var validationResult = await ValidateRequestAsync(data);
            if (!validationResult.IsValid)
            {
                Logger.LogWarning("Request validation failed, skipping API call. Errors: {Errors}", validationResult.Summary);
                return ApiResponse<T>.CreateValidationFailure(validationResult.Errors.Select(e => e.Message).ToList());
            }
        }

        // Р’С‹РїРѕР»РЅСЏРµРј HTTP Р·Р°РїСЂРѕСЃ
        return await ExecuteHttpRequestAsync<ApiResponse<T>>(method, endpoint, data);
    }

    /// <summary>
    /// РћР±С‰РёР№ РјРµС‚РѕРґ РґР»СЏ РІС‹РїРѕР»РЅРµРЅРёСЏ HTTP Р·Р°РїСЂРѕСЃРѕРІ СЃ СѓСЃС‚СЂР°РЅРµРЅРёРµРј РґСѓР±Р»РёСЂРѕРІР°РЅРёСЏ РєРѕРґР°
    /// </summary>
    public async Task<T> ExecuteHttpRequestAsync<T>(
        HttpMethod method, 
        string endpoint, 
        object? data = null,
        Func<HttpResponseMessage, Task<T>>? customResponseHandler = null)
    {
        return await ResilientApiService.ExecuteWithRetryAsync(async () =>
        {
            var fullUrl = await BuildFullUrlAsync(endpoint);
            var request = new HttpRequestMessage(method, fullUrl);
            
            // Р”РѕР±Р°РІР»СЏРµРј РєРѕРЅС‚РµРЅС‚ РґР»СЏ POST/PUT Р·Р°РїСЂРѕСЃРѕРІ
            if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put))
            {
                request.Content = JsonContent.Create(data);
            }
            
            Logger.LogDebug("Making {Method} request to {FullUrl}", method, fullUrl);
            
            try
            {
                var response = await HttpClient.SendAsync(request);
                Logger.LogDebug("Received response with status {StatusCode} for {Method} {FullUrl}", 
                    response.StatusCode, method, fullUrl);
                
                // РСЃРїРѕР»СЊР·СѓРµРј РєР°СЃС‚РѕРјРЅС‹Р№ РѕР±СЂР°Р±РѕС‚С‡РёРє РёР»Рё СЃС‚Р°РЅРґР°СЂС‚РЅС‹Р№
                return customResponseHandler != null 
                    ? await customResponseHandler(response)
                    : await HandleStandardResponseAsync<T>(response);
            }
            catch (TokenRefreshedException)
            {
                // РўРѕРєРµРЅ Р±С‹Р» РѕР±РЅРѕРІР»РµРЅ, РїРѕРІС‚РѕСЂСЏРµРј Р·Р°РїСЂРѕСЃ
                Logger.LogInformation("Token was refreshed, retrying {Method} request to {FullUrl}", method, fullUrl);
                
                // РЎРѕР·РґР°РµРј РЅРѕРІС‹Р№ Р·Р°РїСЂРѕСЃ СЃ РѕР±РЅРѕРІР»РµРЅРЅС‹Рј С‚РѕРєРµРЅРѕРј
                var retryRequest = new HttpRequestMessage(method, fullUrl);
                if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put))
                {
                    retryRequest.Content = JsonContent.Create(data);
                }
                
                var retryResponse = await HttpClient.SendAsync(retryRequest);
                Logger.LogDebug("Retry response with status {StatusCode} for {Method} {FullUrl}", 
                    retryResponse.StatusCode, method, fullUrl);
                
                return customResponseHandler != null 
                    ? await customResponseHandler(retryResponse)
                    : await HandleStandardResponseAsync<T>(retryResponse);
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError(ex, "HTTP request failed for {Method} {FullUrl}: {Message}", 
                    method, fullUrl, ex.Message);
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogError(ex, "HTTP request timeout for {Method} {FullUrl}", method, fullUrl);
                throw new HttpRequestException($"Request timeout for {method} {fullUrl}", ex);
            }
        }, $"{method} {endpoint}");
    }

    /// <summary>
    /// РџРѕСЃС‚СЂРѕРµРЅРёРµ РїРѕР»РЅРѕРіРѕ URL СЃ РІР°Р»РёРґР°С†РёРµР№ Рё РёСЃРїСЂР°РІР»РµРЅРёРµРј
    /// </summary>
    private async Task<string> BuildFullUrlAsync(string endpoint)
    {
        return await UrlBuilderService.BuildFullUrlAsync(endpoint);
    }

    /// <summary>
    /// РЎС‚Р°РЅРґР°СЂС‚РЅР°СЏ РѕР±СЂР°Р±РѕС‚РєР° HTTP РѕС‚РІРµС‚РѕРІ
    /// </summary>
    private async Task<T> HandleStandardResponseAsync<T>(HttpResponseMessage response)
    {
        try
        {
            var apiResponse = await ErrorHandler.HandleResponseAsync<T>(response);
            
            if (!apiResponse.Success)
            {
                // РџСЂРѕРІРµСЂСЏРµРј, РЅСѓР¶РЅРѕ Р»Рё РїРѕРІС‚РѕСЂРёС‚СЊ Р·Р°РїСЂРѕСЃ РїРѕСЃР»Рµ РѕР±РЅРѕРІР»РµРЅРёСЏ С‚РѕРєРµРЅР°
                if (apiResponse.ErrorMessage == "TOKEN_REFRESHED")
                {
                    Logger.LogInformation("Token was refreshed, retrying original request");
                    throw new TokenRefreshedException("Token was refreshed, retry required");
                }
                
                Logger.LogError("API request failed: {ErrorMessage}", apiResponse.ErrorMessage);
                throw new HttpRequestException($"API request failed: {apiResponse.ErrorMessage}");
            }
            
            return apiResponse.Data ?? throw new InvalidOperationException("Response data is null");
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP request failed with status {StatusCode}", response.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error handling response");
            throw new HttpRequestException($"Unexpected error: {ex.Message}", ex);
        }
    }

    protected async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            return await AutoTokenRefreshService.ExecuteWithAutoRefreshAsync(async () =>
                await ExecuteHttpRequestAsync<ApiResponse<T>>(HttpMethod.Get, endpoint));
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<T>(ex, $"GET {endpoint}");
        }
    }

    protected async Task<PagedApiResponse<T>> GetPagedAsync<T>(string endpoint)
    {
        try
        {
            return await AutoTokenRefreshService.ExecutePagedWithAutoRefreshAsync(async () =>
                await ExecuteHttpRequestAsync<PagedApiResponse<T>>(HttpMethod.Get, endpoint));
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandlePagedExceptionAsync<T>(ex, $"GET PAGED {endpoint}");
        }
    }

    protected async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            return await ExecuteWithValidationAsync<T>(HttpMethod.Post, endpoint, data);
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<T>(ex, $"POST {endpoint}");
        }
    }

    protected async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            // Р’Р°Р»РёРґРёСЂСѓРµРј РґР°РЅРЅС‹Рµ РїРµСЂРµРґ РѕС‚РїСЂР°РІРєРѕР№
            var validationResult = await ValidateRequestAsync(data);
            if (!validationResult.IsValid)
            {
                Logger.LogWarning("PUT request validation failed, skipping API call. Errors: {Errors}", validationResult.Summary);
                return ApiResponse<T>.CreateValidationFailure(validationResult.Errors.Select(e => e.Message).ToList());
            }

            // Р”Р»СЏ PUT Р·Р°РїСЂРѕСЃРѕРІ РЅСѓР¶РµРЅ СЃРїРµС†РёР°Р»СЊРЅС‹Р№ РѕР±СЂР°Р±РѕС‚С‡РёРє РѕС‚РІРµС‚Р°
            return await ExecuteHttpRequestAsync<ApiResponse<T>>(HttpMethod.Put, endpoint, data, 
                async response =>
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<T>();
                        Logger.LogDebug("PUT request successful for {StatusCode}", response.StatusCode);
                        return ApiResponse<T>.CreateSuccess(result!);
                    }
                    else
                    {
                        return await ErrorHandler.HandleResponseAsync<T>(response);
                    }
                });
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<T>(ex, $"PUT {endpoint}");
        }
    }

    protected async Task<ApiResponse<bool>> DeleteAsync(string endpoint)
    {
        try
        {
            var fullUrl = await BuildFullUrlAsync(endpoint);
            var request = new HttpRequestMessage(HttpMethod.Delete, fullUrl);
            
            Logger.LogDebug("Making DELETE request to {FullUrl}", fullUrl);
            
            var response = await HttpClient.SendAsync(request);
            Logger.LogDebug("Received response with status {StatusCode} for DELETE {FullUrl}", 
                response.StatusCode, fullUrl);
            
            if (response.IsSuccessStatusCode)
            {
                Logger.LogDebug("DELETE request successful for {StatusCode}", response.StatusCode);
                return ApiResponse<bool>.CreateSuccess(true);
            }
            else
            {
                var errorResponse = await ErrorHandler.HandleResponseAsync<bool>(response);
                return ApiResponse<bool>.CreateFailure(errorResponse.ErrorMessage ?? "Delete operation failed");
            }
        }
        catch (Exception ex)
        {
            return await ErrorHandler.HandleExceptionAsync<bool>(ex, $"DELETE {endpoint}");
        }
    }
}


