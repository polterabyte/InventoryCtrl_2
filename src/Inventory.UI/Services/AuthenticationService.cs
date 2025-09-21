using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Blazored.LocalStorage;
using Inventory.Shared.Interfaces;

namespace Inventory.UI.Services;

public interface IAuthenticationService
{
    Task<bool> IsAuthenticatedAsync();
    Task<bool> IsTokenValidAsync();
    Task<string?> GetTokenAsync();
    Task ClearAuthenticationAsync();
    Task<string?> GetReturnUrlAsync();
    Task SetReturnUrlAsync(string url);
    Task ClearReturnUrlAsync();
}

public class AuthenticationService : IAuthenticationService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _navigationManager;

    public AuthenticationService(
        AuthenticationStateProvider authStateProvider,
        ILocalStorageService localStorage,
        NavigationManager navigationManager)
    {
        _authStateProvider = authStateProvider;
        _localStorage = localStorage;
        _navigationManager = navigationManager;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            return authState.User.Identity?.IsAuthenticated == true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsTokenValidAsync()
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token))
                return false;

            // Проверяем формат JWT токена
            if (token.Split('.').Length != 3)
                return false;

            // Проверяем, не истек ли токен
            var payload = token.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs != null && keyValuePairs.TryGetValue("exp", out var expObj))
            {
                if (long.TryParse(expObj.ToString(), out var exp))
                {
                    var expDateTime = DateTimeOffset.FromUnixTimeSeconds(exp);
                    if (expDateTime <= DateTimeOffset.UtcNow)
                    {
                        // Токен истек, очищаем его
                        await ClearAuthenticationAsync();
                        return false;
                    }
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _localStorage.GetItemAsStringAsync("authToken");
        }
        catch
        {
            return null;
        }
    }

    public async Task ClearAuthenticationAsync()
    {
        try
        {
            await _localStorage.RemoveItemAsync("authToken");
            
            if (_authStateProvider is ICustomAuthenticationStateProvider customProvider)
            {
                await customProvider.MarkUserAsLoggedOutAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing authentication: {ex.Message}");
        }
    }

    public async Task<string?> GetReturnUrlAsync()
    {
        try
        {
            return await _localStorage.GetItemAsStringAsync("returnUrl");
        }
        catch
        {
            return null;
        }
    }

    public async Task SetReturnUrlAsync(string url)
    {
        try
        {
            if (!string.IsNullOrEmpty(url) && url != "login")
            {
                await _localStorage.SetItemAsStringAsync("returnUrl", url);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting return URL: {ex.Message}");
        }
    }

    public async Task ClearReturnUrlAsync()
    {
        try
        {
            await _localStorage.RemoveItemAsync("returnUrl");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing return URL: {ex.Message}");
        }
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
