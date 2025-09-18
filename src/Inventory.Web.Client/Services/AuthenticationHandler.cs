using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;

namespace Inventory.Web.Client.Services;

public class AuthenticationHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;

    public AuthenticationHandler(ILocalStorageService localStorage, AuthenticationStateProvider authStateProvider)
    {
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            // Get token from local storage
            var token = await _localStorage.GetItemAsStringAsync("authToken");
            
            if (!string.IsNullOrEmpty(token))
            {
                // Add Authorization header
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (Exception)
        {
            // If we can't get the token, continue without it
            // The API will return 401 if authentication is required
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
