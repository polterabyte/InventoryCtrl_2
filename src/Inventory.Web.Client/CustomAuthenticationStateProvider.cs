using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using Inventory.Shared.Interfaces;
using Inventory.Web.Client.Services;

namespace Inventory.Web.Client
{
    public class CustomAuthenticationStateProvider(
        HttpClient httpClient, 
        ILocalStorageService localStorage,
        ITokenManagementService tokenManagementService) 
        : AuthenticationStateProvider, ICustomAuthenticationStateProvider
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILocalStorageService _localStorage = localStorage;
        private readonly ITokenManagementService _tokenManagementService = tokenManagementService;

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Получаем токен через TokenManagementService
                var token = await _tokenManagementService.GetStoredTokenAsync();
                var identity = new ClaimsIdentity();
                _httpClient.DefaultRequestHeaders.Authorization = null;

                if (!string.IsNullOrEmpty(token))
                {
                    // Проверяем, не истекает ли токен в ближайшее время
                    if (await _tokenManagementService.IsTokenExpiringSoonAsync())
                    {
                        // Пытаемся обновить токен
                        var refreshSuccess = await _tokenManagementService.TryRefreshTokenAsync();
                        if (refreshSuccess)
                        {
                            // Получаем обновленный токен
                            token = await _tokenManagementService.GetStoredTokenAsync();
                        }
                        else
                        {
                            // Если не удалось обновить, очищаем токены
                            await _tokenManagementService.ClearTokensAsync();
                            token = null;
                        }
                    }

                    if (!string.IsNullOrEmpty(token))
                    {
                        identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                }

                var user = new ClaimsPrincipal(identity);
                return new AuthenticationState(user);
            }
            catch (Exception ex)
            {
                // В случае ошибки возвращаем неаутентифицированное состояние
                Console.WriteLine($"Error in GetAuthenticationStateAsync: {ex.Message}");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public async Task MarkUserAsAuthenticatedAsync(string token)
        {
            // Сохраняем токен через TokenManagementService
            await _tokenManagementService.SaveTokensAsync(token, "");
            
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
            var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
            NotifyAuthenticationStateChanged(authState);
        }

        public async Task MarkUserAsLoggedOutAsync()
        {
            // Очищаем токены через TokenManagementService
            await _tokenManagementService.ClearTokensAsync();
            
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(anonymousUser));
            NotifyAuthenticationStateChanged(authState);
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs != null)
            {
                keyValuePairs.TryGetValue(ClaimTypes.Role, out object? roles);

                if (roles != null)
                {
                    if (roles.ToString()?.Trim().StartsWith("[") == true)
                    {
                        var parsedRoles = JsonSerializer.Deserialize<string[]>(roles.ToString()!);

                        if (parsedRoles != null)
                        {
                            foreach (var parsedRole in parsedRoles)
                            {
                                claims.Add(new Claim(ClaimTypes.Role, parsedRole));
                            }
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimTypes.Role, roles.ToString() ?? string.Empty));
                    }

                    keyValuePairs.Remove(ClaimTypes.Role);
                }

                claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty)));
            }
            return claims;
        }
        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}

