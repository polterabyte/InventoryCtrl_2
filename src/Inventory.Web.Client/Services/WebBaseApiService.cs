using System.Net.Http.Json;
using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Inventory.Web.Client.Services;

public abstract class WebBaseApiService(
    HttpClient httpClient, 
    IApiUrlService apiUrlService, 
    IResilientApiService resilientApiService,
    ILogger logger,
    IJSRuntime jsRuntime)
{
    protected readonly HttpClient HttpClient = httpClient;
    protected readonly IApiUrlService ApiUrlService = apiUrlService;
    protected readonly IResilientApiService ResilientApiService = resilientApiService;
    protected readonly ILogger Logger = logger;
    protected readonly IJSRuntime JSRuntime = jsRuntime;

    protected async Task<string> GetApiUrlAsync()
    {
        var apiUrl = await ApiUrlService.GetApiBaseUrlAsync();
        Logger.LogDebug("Using API URL: {ApiUrl}", apiUrl);
        return apiUrl;
    }

    protected async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        return await ResilientApiService.GetWithRetryAsync(async () =>
        {
            var apiUrl = await GetApiUrlAsync();
            var fullUrl = $"{apiUrl.TrimEnd('/')}{endpoint}";
            
            // Валидация и исправление URL
            if (!Uri.IsWellFormedUriString(fullUrl, UriKind.Absolute))
            {
                // Если URL относительный, делаем его абсолютным
                if (fullUrl.StartsWith("/"))
                {
                    // В staging окружении используем текущий origin
                    try
                    {
                        var origin = await JSRuntime.InvokeAsync<string>("eval", "window.location.origin");
                        fullUrl = $"{origin.TrimEnd('/')}{fullUrl}";
                    }
                    catch
                    {
                        // Fallback для staging
                        fullUrl = $"http://staging.warehouse.cuby{fullUrl}";
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Invalid URL format: {fullUrl}");
                }
            }
            
            Logger.LogDebug("Making GET request to {FullUrl}", fullUrl);
            var response = await HttpClient.GetAsync(fullUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
                Logger.LogDebug("GET request successful for {FullUrl}", fullUrl);
                return apiResponse ?? new ApiResponse<T> { Success = false, ErrorMessage = "Failed to deserialize response" };
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("GET request failed for {FullUrl}. Status: {StatusCode}, Error: {Error}", 
                    fullUrl, response.StatusCode, errorMessage);
                return new ApiResponse<T> { Success = false, ErrorMessage = errorMessage };
            }
        }, endpoint);
    }

    protected async Task<PagedApiResponse<T>> GetPagedAsync<T>(string endpoint)
    {
        return await ResilientApiService.GetWithRetryAsync(async () =>
        {
            var apiUrl = await GetApiUrlAsync();
            var fullUrl = $"{apiUrl.TrimEnd('/')}{endpoint}";
            
            // Валидация и исправление URL
            if (!Uri.IsWellFormedUriString(fullUrl, UriKind.Absolute))
            {
                // Если URL относительный, делаем его абсолютным
                if (fullUrl.StartsWith("/"))
                {
                    // В staging окружении используем текущий origin
                    try
                    {
                        var origin = await JSRuntime.InvokeAsync<string>("eval", "window.location.origin");
                        fullUrl = $"{origin.TrimEnd('/')}{fullUrl}";
                    }
                    catch
                    {
                        // Fallback для staging
                        fullUrl = $"http://staging.warehouse.cuby{fullUrl}";
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Invalid URL format: {fullUrl}");
                }
            }
            
            Logger.LogDebug("Making GET request to {FullUrl}", fullUrl);
            var response = await HttpClient.GetAsync(fullUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<PagedApiResponse<T>>();
                Logger.LogDebug("GET request successful for {FullUrl}", fullUrl);
                return apiResponse ?? new PagedApiResponse<T> { Success = false, ErrorMessage = "Failed to deserialize response" };
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("GET request failed for {FullUrl}. Status: {StatusCode}, Error: {Error}", 
                    fullUrl, response.StatusCode, errorMessage);
                return new PagedApiResponse<T> { Success = false, ErrorMessage = errorMessage };
            }
        }, endpoint);
    }

    protected async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
    {
        return await ResilientApiService.PostWithRetryAsync(async () =>
        {
            var apiUrl = await GetApiUrlAsync();
            var fullUrl = $"{apiUrl.TrimEnd('/')}{endpoint}";
            
            // Валидация и исправление URL
            if (!Uri.IsWellFormedUriString(fullUrl, UriKind.Absolute))
            {
                // Если URL относительный, делаем его абсолютным
                if (fullUrl.StartsWith("/"))
                {
                    // В staging окружении используем текущий origin
                    try
                    {
                        var origin = await JSRuntime.InvokeAsync<string>("eval", "window.location.origin");
                        fullUrl = $"{origin.TrimEnd('/')}{fullUrl}";
                    }
                    catch
                    {
                        // Fallback для staging
                        fullUrl = $"http://staging.warehouse.cuby{fullUrl}";
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Invalid URL format: {fullUrl}");
                }
            }
            
            Logger.LogDebug("Making POST request to {FullUrl}", fullUrl);
            var response = await HttpClient.PostAsJsonAsync(fullUrl, data);
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
                Logger.LogDebug("POST request successful for {FullUrl}", fullUrl);
                return apiResponse ?? new ApiResponse<T> { Success = false, ErrorMessage = "Failed to deserialize response" };
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("POST request failed for {FullUrl}. Status: {StatusCode}, Error: {Error}", 
                    fullUrl, response.StatusCode, errorMessage);
                return new ApiResponse<T> { Success = false, ErrorMessage = errorMessage };
            }
        }, endpoint);
    }

    protected async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data)
    {
        return await ResilientApiService.PutWithRetryAsync(async () =>
        {
            var apiUrl = await GetApiUrlAsync();
            var fullUrl = $"{apiUrl.TrimEnd('/')}{endpoint}";
            
            // Валидация и исправление URL
            if (!Uri.IsWellFormedUriString(fullUrl, UriKind.Absolute))
            {
                // Если URL относительный, делаем его абсолютным
                if (fullUrl.StartsWith("/"))
                {
                    // В staging окружении используем текущий origin
                    try
                    {
                        var origin = await JSRuntime.InvokeAsync<string>("eval", "window.location.origin");
                        fullUrl = $"{origin.TrimEnd('/')}{fullUrl}";
                    }
                    catch
                    {
                        // Fallback для staging
                        fullUrl = $"http://staging.warehouse.cuby{fullUrl}";
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Invalid URL format: {fullUrl}");
                }
            }
            
            Logger.LogDebug("Making PUT request to {FullUrl}", fullUrl);
            var response = await HttpClient.PutAsJsonAsync(fullUrl, data);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<T>();
                Logger.LogDebug("PUT request successful for {FullUrl}", fullUrl);
                return new ApiResponse<T> { Success = true, Data = result };
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("PUT request failed for {FullUrl}. Status: {StatusCode}, Error: {Error}", 
                    fullUrl, response.StatusCode, errorMessage);
                return new ApiResponse<T> { Success = false, ErrorMessage = errorMessage };
            }
        }, endpoint);
    }

    protected async Task<ApiResponse<bool>> DeleteAsync(string endpoint)
    {
        return await ResilientApiService.ExecuteWithRetryAsync(async () =>
        {
            var apiUrl = await GetApiUrlAsync();
            var fullUrl = $"{apiUrl.TrimEnd('/')}{endpoint}";
            
            // Валидация и исправление URL
            if (!Uri.IsWellFormedUriString(fullUrl, UriKind.Absolute))
            {
                // Если URL относительный, делаем его абсолютным
                if (fullUrl.StartsWith("/"))
                {
                    // В staging окружении используем текущий origin
                    try
                    {
                        var origin = await JSRuntime.InvokeAsync<string>("eval", "window.location.origin");
                        fullUrl = $"{origin.TrimEnd('/')}{fullUrl}";
                    }
                    catch
                    {
                        // Fallback для staging
                        fullUrl = $"http://staging.warehouse.cuby{fullUrl}";
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Invalid URL format: {fullUrl}");
                }
            }
            
            Logger.LogDebug("Making DELETE request to {FullUrl}", fullUrl);
            var response = await HttpClient.DeleteAsync(fullUrl);
            
            if (response.IsSuccessStatusCode)
            {
                Logger.LogDebug("DELETE request successful for {FullUrl}", fullUrl);
            }
            else
            {
                Logger.LogWarning("DELETE request failed for {FullUrl}. Status: {StatusCode}", 
                    fullUrl, response.StatusCode);
            }
            
            return new ApiResponse<bool> { Success = response.IsSuccessStatusCode, Data = response.IsSuccessStatusCode };
        }, $"DELETE {endpoint}");
    }
}
