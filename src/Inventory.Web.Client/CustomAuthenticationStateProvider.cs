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
                var identity = new ClaimsIdentity();
                _httpClient.DefaultRequestHeaders.Authorization = null;

                // Просто получаем токен, без логики обновления
                var token = await _tokenManagementService.GetStoredTokenAsync();

                if (!string.IsNullOrEmpty(token))
                {
                    // Проверяем валидность токена (например, не истек ли он полностью)
                    var expiration = GetTokenExpirationTime(token);
                    if (expiration.HasValue && expiration.Value > DateTimeOffset.UtcNow)
                    {
                        identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                    else
                    {
                        // Токен истек, очищаем его
                        await _tokenManagementService.ClearTokensAsync();
                    }
                }

                var user = new ClaimsPrincipal(identity);
                return new AuthenticationState(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAuthenticationStateAsync: {ex.Message}");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public async Task MarkUserAsAuthenticatedAsync(string accessToken, string refreshToken)
        {
            await _tokenManagementService.SaveTokensAsync(accessToken, refreshToken);
            
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(accessToken), "jwt"));
            var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
            NotifyAuthenticationStateChanged(authState);
        }
        
        public async Task MarkUserAsLoggedOutAsync()
        {
            await _tokenManagementService.ClearTokensAsync();
            
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(anonymousUser));
            NotifyAuthenticationStateChanged(authState);
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            if (string.IsNullOrEmpty(jwt)) return claims;

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
                            claims.AddRange(parsedRoles.Select(parsedRole => new Claim(ClaimTypes.Role, parsedRole)));
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
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }

        private DateTimeOffset? GetTokenExpirationTime(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3) return null;

                var payload = parts[1];
                var jsonBytes = ParseBase64WithoutPadding(payload);
                var payloadJson = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

                if (payloadJson?.TryGetValue("exp", out var expValue) == true && long.TryParse(expValue.ToString(), out var expUnix))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(expUnix);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}

