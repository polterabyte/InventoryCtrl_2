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
    private readonly IUrlBuilderService _urlBuilderService;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        HttpClient httpClient,
        IAuthenticationService authService,
        IJSRuntime jsRuntime,
        IUrlBuilderService urlBuilderService,
        ILogger<UserManagementService> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _jsRuntime = jsRuntime;
        _urlBuilderService = urlBuilderService;
        _logger = logger;
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
        return await _urlBuilderService.BuildFullUrlAsync(endpoint);
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
            var fullUrl = await GetFullApiUrlAsync($"/user{queryString}");
            
            _logger.LogDebug("Making request to: {FullUrl}", fullUrl);
            var response = await client.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Received response content: {Content}", content);
                
                try
                {
                    return JsonSerializer.Deserialize<PagedApiResponse<UserDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize JSON response. Content: {Content}", content);
                    return null;
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error getting users: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return null;
        }
    }

    public async Task<ApiResponse<UserDto>?> GetUserAsync(string id)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var fullUrl = await GetFullApiUrlAsync($"/user/{id}");
            var response = await client.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<UserDto>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Error getting user: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user");
            return null;
        }
    }

    public async Task<ApiResponse<UserDto>?> CreateUserAsync(CreateUserDto user)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var fullUrl = await GetFullApiUrlAsync("/user");
            var json = JsonSerializer.Serialize(user);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Creating user at URL: {FullUrl}", fullUrl);
            
            var response = await client.PostAsync(fullUrl, content);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<UserDto>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error creating user: {StatusCode}, Response: {ResponseContent}", response.StatusCode, responseContent);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
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

            var fullUrl = await GetFullApiUrlAsync($"/user/{id}");
            var response = await client.PutAsync(fullUrl, content);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<UserDto>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error updating user: {StatusCode}", response.StatusCode);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
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
            var fullUrl = await GetFullApiUrlAsync($"/user/{id}");
            var response = await client.DeleteAsync(fullUrl);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error deleting user: {StatusCode}", response.StatusCode);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
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

            var fullUrl = await GetFullApiUrlAsync($"/user/{id}/change-password");
            var response = await client.PostAsync(fullUrl, content);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error changing password: {StatusCode}", response.StatusCode);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
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
            var fullUrl = await GetFullApiUrlAsync($"/user/export{queryString}");
            var response = await client.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                var fileName = $"users-export-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv";
                
                // Use JavaScript to download the file
                await _jsRuntime.InvokeVoidAsync("downloadFileFromBytes", fileName, content);
                return true;
            }

            _logger.LogError("Error exporting users: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users");
            return false;
        }
    }
}