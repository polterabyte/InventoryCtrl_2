using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.JSInterop;
using System.Text;
using System.Text.Json;
using Inventory.UI.Services;

namespace Inventory.Web.Client.Services;

public class UserManagementService : IUserManagementService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authService;
    private readonly IJSRuntime _jsRuntime;
    private readonly IApiUrlService _apiUrlService;

    public UserManagementService(
        HttpClient httpClient,
        IAuthenticationService authService,
        IJSRuntime jsRuntime,
        IApiUrlService apiUrlService)
    {
        _httpClient = httpClient;
        _authService = authService;
        _jsRuntime = jsRuntime;
        _apiUrlService = apiUrlService;
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        return _httpClient;
    }

    private async Task<string> GetFullApiUrlAsync(string endpoint)
    {
        var apiBaseUrl = await _apiUrlService.GetApiBaseUrlAsync();
        return $"{apiBaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
    }

    public async Task<PagedApiResponse<UserDto>?> GetUsersAsync(int page = 1, int pageSize = 10, string? search = null, string? role = null)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var queryParams = new List<string>();
            
            if (page > 0) queryParams.Add($"page={page}");
            if (pageSize > 0) queryParams.Add($"pageSize={pageSize}");
            if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
            if (!string.IsNullOrEmpty(role)) queryParams.Add($"role={Uri.EscapeDataString(role)}");

            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var fullUrl = await GetFullApiUrlAsync($"user{queryString}");
            var response = await client.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PagedApiResponse<UserDto>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            await _jsRuntime.InvokeVoidAsync("console.error", $"Error getting users: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", "Error getting users:", ex.Message);
            return null;
        }
    }

    public async Task<ApiResponse<UserDto>?> GetUserAsync(string id)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var fullUrl = await GetFullApiUrlAsync($"user/{id}");
            var response = await client.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<UserDto>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            await _jsRuntime.InvokeVoidAsync("console.error", $"Error getting user: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", "Error getting user:", ex.Message);
            return null;
        }
    }

    public async Task<ApiResponse<UserDto>?> CreateUserAsync(CreateUserDto user)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var fullUrl = await GetFullApiUrlAsync("user");
            var json = JsonSerializer.Serialize(user);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _jsRuntime.InvokeVoidAsync("console.log", $"Creating user at URL: {fullUrl}");
            
            var response = await client.PostAsync(fullUrl, content);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<UserDto>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (!response.IsSuccessStatusCode)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Error creating user: {response.StatusCode}, Response: {responseContent}");
            }

            return result;
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", "Error creating user:", ex.Message);
            return new ApiResponse<UserDto>
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ApiResponse<UserDto>?> UpdateUserAsync(string id, UpdateUserDto user)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var json = JsonSerializer.Serialize(user);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var fullUrl = await GetFullApiUrlAsync($"user/{id}");
            var response = await client.PutAsync(fullUrl, content);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<UserDto>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (!response.IsSuccessStatusCode)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Error updating user: {response.StatusCode}");
            }

            return result;
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", "Error updating user:", ex.Message);
            return new ApiResponse<UserDto>
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ApiResponse<object>?> DeleteUserAsync(string id)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var fullUrl = await GetFullApiUrlAsync($"user/{id}");
            var response = await client.DeleteAsync(fullUrl);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (!response.IsSuccessStatusCode)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Error deleting user: {response.StatusCode}");
            }

            return result;
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", "Error deleting user:", ex.Message);
            return new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ApiResponse<object>?> ChangePasswordAsync(string id, ChangePasswordDto passwordDto)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var json = JsonSerializer.Serialize(passwordDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var fullUrl = await GetFullApiUrlAsync($"user/{id}/change-password");
            var response = await client.PostAsync(fullUrl, content);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (!response.IsSuccessStatusCode)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Error changing password: {response.StatusCode}");
            }

            return result;
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", "Error changing password:", ex.Message);
            return new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> ExportUsersAsync(string? search = null, string? role = null)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
            if (!string.IsNullOrEmpty(role)) queryParams.Add($"role={Uri.EscapeDataString(role)}");

            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var fullUrl = await GetFullApiUrlAsync($"user/export{queryString}");
            var response = await client.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                var fileName = $"users-export-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv";
                
                // Use JavaScript to download the file
                await _jsRuntime.InvokeVoidAsync("downloadFileFromBytes", fileName, content);
                return true;
            }

            await _jsRuntime.InvokeVoidAsync("console.error", $"Error exporting users: {response.StatusCode}");
            return false;
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", "Error exporting users:", ex.Message);
            return false;
        }
    }
}