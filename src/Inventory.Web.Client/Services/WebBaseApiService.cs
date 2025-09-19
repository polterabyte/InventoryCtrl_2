using System.Net.Http.Json;
using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Inventory.Web.Client.Services;

public abstract class WebBaseApiService(HttpClient httpClient, IJSRuntime jsRuntime, ILogger logger)
{
    protected readonly HttpClient HttpClient = httpClient;
    protected readonly IJSRuntime JSRuntime = jsRuntime;
    protected readonly ILogger Logger = logger;

    protected async Task<string> GetApiUrlAsync()
    {
        try
        {
            var apiUrl = await JSRuntime.InvokeAsync<string>("getApiBaseUrl");
            Logger.LogDebug("Using API URL: {ApiUrl}", apiUrl);
            return apiUrl;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get API URL from JavaScript, using fallback");
            // Fallback to relative path
            return "/api";
        }
    }

    protected async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            var apiUrl = await GetApiUrlAsync();
            var fullUrl = $"{apiUrl}{endpoint}";
            
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
            var apiUrl = await GetApiUrlAsync();
            var fullUrl = $"{apiUrl}{endpoint}";
            
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
            var apiUrl = await GetApiUrlAsync();
            var fullUrl = $"{apiUrl}{endpoint}";
            
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
            var apiUrl = await GetApiUrlAsync();
            var fullUrl = $"{apiUrl}{endpoint}";
            
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
            var apiUrl = await GetApiUrlAsync();
            var fullUrl = $"{apiUrl}{endpoint}";
            
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
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception occurred during DELETE request to {Endpoint}", endpoint);
            return new ApiResponse<bool> { Success = false, ErrorMessage = ex.Message };
        }
    }
}
