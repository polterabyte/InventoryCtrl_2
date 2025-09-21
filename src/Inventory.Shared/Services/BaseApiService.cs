using System.Net.Http.Json;
using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Inventory.Shared.Services;

public abstract class BaseApiService(HttpClient httpClient, string baseUrl, ILogger logger)
{
    protected readonly HttpClient HttpClient = httpClient;
    protected readonly string BaseUrl = baseUrl;
    protected readonly ILogger Logger = logger;

    protected async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            var fullUrl = GetFullUrl(endpoint);
            Logger.LogDebug("Making GET request to {FullUrl}", fullUrl);
            var response = await HttpClient.GetAsync(fullUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
                Logger.LogDebug("GET request successful for {Endpoint}", endpoint);
                return apiResponse ?? new ApiResponse<T> { Success = false, ErrorMessage = "Failed to deserialize response" };
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("GET request failed for {Endpoint}. Status: {StatusCode}, Error: {Error}", 
                    endpoint, response.StatusCode, errorMessage);
                return new ApiResponse<T> { Success = false, ErrorMessage = errorMessage };
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception occurred during GET request to {Endpoint}", endpoint);
            return new ApiResponse<T> { Success = false, ErrorMessage = ex.Message };
        }
    }

    protected async Task<PagedApiResponse<T>> GetPagedAsync<T>(string endpoint)
    {
        try
        {
            var fullUrl = GetFullUrl(endpoint);
            Logger.LogDebug("Making GET request to {FullUrl}", fullUrl);
            var response = await HttpClient.GetAsync(fullUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<PagedApiResponse<T>>();
                Logger.LogDebug("GET request successful for {Endpoint}", endpoint);
                return apiResponse ?? new PagedApiResponse<T> { Success = false, ErrorMessage = "Failed to deserialize response" };
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("GET request failed for {Endpoint}. Status: {StatusCode}, Error: {Error}", 
                    endpoint, response.StatusCode, errorMessage);
                return new PagedApiResponse<T> { Success = false, ErrorMessage = errorMessage };
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception occurred during GET request to {Endpoint}", endpoint);
            return new PagedApiResponse<T> { Success = false, ErrorMessage = ex.Message };
        }
    }

    protected async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            var fullUrl = GetFullUrl(endpoint);
            Logger.LogDebug("Making POST request to {FullUrl}", fullUrl);
            var response = await HttpClient.PostAsJsonAsync(fullUrl, data);
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
                Logger.LogDebug("POST request successful for {Endpoint}", endpoint);
                return apiResponse ?? new ApiResponse<T> { Success = false, ErrorMessage = "Failed to deserialize response" };
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("POST request failed for {Endpoint}. Status: {StatusCode}, Error: {Error}", 
                    endpoint, response.StatusCode, errorMessage);
                return new ApiResponse<T> { Success = false, ErrorMessage = errorMessage };
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception occurred during POST request to {Endpoint}", endpoint);
            return new ApiResponse<T> { Success = false, ErrorMessage = ex.Message };
        }
    }

    protected async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            var fullUrl = GetFullUrl(endpoint);
            Logger.LogDebug("Making PUT request to {FullUrl}", fullUrl);
            var response = await HttpClient.PutAsJsonAsync(fullUrl, data);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<T>();
                Logger.LogDebug("PUT request successful for {Endpoint}", endpoint);
                return new ApiResponse<T> { Success = true, Data = result };
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("PUT request failed for {Endpoint}. Status: {StatusCode}, Error: {Error}", 
                    endpoint, response.StatusCode, errorMessage);
                return new ApiResponse<T> { Success = false, ErrorMessage = errorMessage };
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception occurred during PUT request to {Endpoint}", endpoint);
            return new ApiResponse<T> { Success = false, ErrorMessage = ex.Message };
        }
    }

    protected async Task<ApiResponse<bool>> DeleteAsync(string endpoint)
    {
        try
        {
            var fullUrl = GetFullUrl(endpoint);
            Logger.LogDebug("Making DELETE request to {FullUrl}", fullUrl);
            var response = await HttpClient.DeleteAsync(fullUrl);
            
            if (response.IsSuccessStatusCode)
            {
                Logger.LogDebug("DELETE request successful for {Endpoint}", endpoint);
            }
            else
            {
                Logger.LogWarning("DELETE request failed for {Endpoint}. Status: {StatusCode}", 
                    endpoint, response.StatusCode);
            }
            
            return new ApiResponse<bool> { Success = response.IsSuccessStatusCode, Data = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception occurred during DELETE request to {Endpoint}", endpoint);
            return new ApiResponse<bool> { Success = false, ErrorMessage = ex.Message };
        }
    }

    private string GetFullUrl(string endpoint)
    {
        // Если endpoint уже абсолютный URL, возвращаем как есть
        if (Uri.IsWellFormedUriString(endpoint, UriKind.Absolute))
        {
            return endpoint;
        }

        // Если BaseUrl пустой, возвращаем endpoint как есть (он должен быть полным URL)
        if (string.IsNullOrEmpty(BaseUrl))
        {
            return endpoint;
        }

        // Если endpoint начинается с /, добавляем к BaseUrl
        if (endpoint.StartsWith("/"))
        {
            return $"{BaseUrl.TrimEnd('/')}{endpoint}";
        }

        // Если endpoint не начинается с /, добавляем его
        return $"{BaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
    }
}
