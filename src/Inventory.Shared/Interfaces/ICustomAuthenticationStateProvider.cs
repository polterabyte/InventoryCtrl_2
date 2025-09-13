using Microsoft.AspNetCore.Components.Authorization;

namespace Inventory.Shared.Interfaces;

public interface ICustomAuthenticationStateProvider
{
    Task<AuthenticationState> GetAuthenticationStateAsync();
    Task MarkUserAsAuthenticatedAsync(string token);
    Task MarkUserAsLoggedOutAsync();
}
