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
            Logger.LogDebug("Making GET request to {Endpoint}", endpoint);
            var response = await HttpClient.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>();
                Logger.LogDebug("GET request successful for {Endpoint}", endpoint);
                return new ApiResponse<T> { Success = true, Data = data };
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

    protected async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            Logger.LogDebug("Making POST request to {Endpoint}", endpoint);
            var response = await HttpClient.PostAsJsonAsync(endpoint, data);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<T>();
                Logger.LogDebug("POST request successful for {Endpoint}", endpoint);
                return new ApiResponse<T> { Success = true, Data = result };
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
            Logger.LogDebug("Making PUT request to {Endpoint}", endpoint);
            var response = await HttpClient.PutAsJsonAsync(endpoint, data);
            
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
            Logger.LogDebug("Making DELETE request to {Endpoint}", endpoint);
            var response = await HttpClient.DeleteAsync(endpoint);
            
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
}
