using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Inventory.Web.Client;
using System.Security.Claims;

namespace Inventory.Web.Client.Services;

public interface IAuthorizationService
{
    Task<bool> IsAuthenticatedAsync();
    Task<bool> IsInRoleAsync(string role);
    Task<bool> HasPermissionAsync(string permission);
    Task RedirectToLoginAsync();
    Task RedirectToHomeAsync();
    Task RedirectIfNotAuthenticatedAsync();
    Task RedirectIfNotInRoleAsync(string role);
    Task RedirectIfNotAuthorizedAsync(Func<ClaimsPrincipal, bool> authorizationCheck);
}

public class AuthorizationService(
    AuthenticationStateProvider authStateProvider,
    NavigationManager navigationManager) : IAuthorizationService
{
    private readonly AuthenticationStateProvider _authStateProvider = authStateProvider;
    private readonly NavigationManager _navigationManager = navigationManager;

    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.Identity?.IsAuthenticated == true;
    }

    public async Task<bool> IsInRoleAsync(string role)
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.IsInRole(role);
    }

    public async Task<bool> HasPermissionAsync(string permission)
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.HasClaim("Permission", permission);
    }

    public Task RedirectToLoginAsync()
    {
        _navigationManager.NavigateTo("/login");
        return Task.CompletedTask;
    }

    public Task RedirectToHomeAsync()
    {
        _navigationManager.NavigateTo("/");
        return Task.CompletedTask;
    }

    public async Task RedirectIfNotAuthenticatedAsync()
    {
        if (!await IsAuthenticatedAsync())
        {
            await RedirectToLoginAsync();
        }
    }

    public async Task RedirectIfNotInRoleAsync(string role)
    {
        if (!await IsInRoleAsync(role))
        {
            await RedirectToLoginAsync();
        }
    }

    public async Task RedirectIfNotAuthorizedAsync(Func<ClaimsPrincipal, bool> authorizationCheck)
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        if (!authorizationCheck(authState.User))
        {
            await RedirectToLoginAsync();
        }
    }
}
