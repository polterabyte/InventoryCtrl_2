using System.Net.Http.Json;
using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;

namespace Inventory.Shared.Services;

public abstract class BaseApiService
{
    protected readonly HttpClient HttpClient;
    protected readonly string BaseUrl;

    protected BaseApiService(HttpClient httpClient, string baseUrl)
    {
        HttpClient = httpClient;
        BaseUrl = baseUrl;
    }

    protected async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            var response = await HttpClient.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>();
                return new ApiResponse<T> { Success = true, Data = data };
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                return new ApiResponse<T> { Success = false, ErrorMessage = errorMessage };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse<T> { Success = false, ErrorMessage = ex.Message };
        }
    }

    protected async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            var response = await HttpClient.PostAsJsonAsync(endpoint, data);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<T>();
                return new ApiResponse<T> { Success = true, Data = result };
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                return new ApiResponse<T> { Success = false, ErrorMessage = errorMessage };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse<T> { Success = false, ErrorMessage = ex.Message };
        }
    }

    protected async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            var response = await HttpClient.PutAsJsonAsync(endpoint, data);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<T>();
                return new ApiResponse<T> { Success = true, Data = result };
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                return new ApiResponse<T> { Success = false, ErrorMessage = errorMessage };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse<T> { Success = false, ErrorMessage = ex.Message };
        }
    }

    protected async Task<ApiResponse<bool>> DeleteAsync(string endpoint)
    {
        try
        {
            var response = await HttpClient.DeleteAsync(endpoint);
            return new ApiResponse<bool> { Success = response.IsSuccessStatusCode, Data = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { Success = false, ErrorMessage = ex.Message };
        }
    }
}
