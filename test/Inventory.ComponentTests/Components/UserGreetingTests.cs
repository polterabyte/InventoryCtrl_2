using Bunit;
using FluentAssertions;
using Inventory.UI.Components;
using Inventory.Shared.Interfaces;
using Inventory.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Xunit;

namespace Inventory.ComponentTests.Components;

public class TestAuthenticationStateProvider(ICustomAuthenticationStateProvider customProvider) : AuthenticationStateProvider
{
    private readonly ICustomAuthenticationStateProvider _customProvider = customProvider;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return await _customProvider.GetAuthenticationStateAsync();
    }
}

public class UserGreetingTests : TestContext
{
    [Fact]
    public void UserGreeting_ShouldRenderGreetingForAuthenticatedUser()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        var mockAuthStateProvider = new Mock<ICustomAuthenticationStateProvider>();
        var mockLocalStorage = new Mock<Blazored.LocalStorage.ILocalStorageService>();

        Services.AddSingleton(mockAuthService.Object);
        Services.AddSingleton(mockAuthStateProvider.Object);
        Services.AddSingleton(mockLocalStorage.Object);
        
        // Add authorization services
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();
        var mockAuthzService = new Mock<IAuthorizationService>();
        mockAuthzService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .Returns<ClaimsPrincipal, object, IEnumerable<IAuthorizationRequirement>>((user, resource, requirements) =>
                Task.FromResult(user.Identity?.IsAuthenticated == true ? AuthorizationResult.Success() : AuthorizationResult.Failed()));
        mockAuthzService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>()))
            .Returns<ClaimsPrincipal, object, string>((user, resource, policy) =>
                Task.FromResult(user.Identity?.IsAuthenticated == true ? AuthorizationResult.Success() : AuthorizationResult.Failed()));
        Services.AddSingleton<IAuthorizationService>(mockAuthzService.Object);
        Services.AddSingleton<AuthenticationStateProvider>(provider => 
            new TestAuthenticationStateProvider(mockAuthStateProvider.Object));

        // Mock authenticated user
        var claimsIdentity = new System.Security.Claims.ClaimsIdentity(
            new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "admin") },
            "test");
        var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);
        var authState = new AuthenticationState(claimsPrincipal);

        mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => 
            parameters.AddChildContent<UserGreeting>());

        // Assert
        component.Markup.Should().Contain("Привет");
        component.Markup.Should().Contain("admin");
    }

    [Fact]
    public void UserGreeting_ShouldNotRenderForUnauthenticatedUser()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        var mockAuthStateProvider = new Mock<ICustomAuthenticationStateProvider>();
        var mockLocalStorage = new Mock<Blazored.LocalStorage.ILocalStorageService>();

        Services.AddSingleton(mockAuthService.Object);
        Services.AddSingleton(mockAuthStateProvider.Object);
        Services.AddSingleton(mockLocalStorage.Object);
        
        // Add authorization services
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();
        var mockAuthzService = new Mock<IAuthorizationService>();
        mockAuthzService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .Returns<ClaimsPrincipal, object, IEnumerable<IAuthorizationRequirement>>((user, resource, requirements) =>
                Task.FromResult(user.Identity?.IsAuthenticated == true ? AuthorizationResult.Success() : AuthorizationResult.Failed()));
        mockAuthzService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>()))
            .Returns<ClaimsPrincipal, object, string>((user, resource, policy) =>
                Task.FromResult(user.Identity?.IsAuthenticated == true ? AuthorizationResult.Success() : AuthorizationResult.Failed()));
        Services.AddSingleton<IAuthorizationService>(mockAuthzService.Object);
        Services.AddSingleton<AuthenticationStateProvider>(provider => 
            new TestAuthenticationStateProvider(mockAuthStateProvider.Object));

        // Mock unauthenticated user
        var claimsIdentity = new System.Security.Claims.ClaimsIdentity();
        var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);
        var authState = new AuthenticationState(claimsPrincipal);

        mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => 
            parameters.AddChildContent<UserGreeting>());

        // Assert
        component.Markup.Should().NotContain("Привет");
    }

    [Fact]
    public void UserGreeting_ShouldHaveLogoutButton()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        var mockAuthStateProvider = new Mock<ICustomAuthenticationStateProvider>();
        var mockLocalStorage = new Mock<Blazored.LocalStorage.ILocalStorageService>();

        Services.AddSingleton(mockAuthService.Object);
        Services.AddSingleton(mockAuthStateProvider.Object);
        Services.AddSingleton(mockLocalStorage.Object);
        
        // Add authorization services
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();
        var mockAuthzService = new Mock<IAuthorizationService>();
        mockAuthzService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .Returns<ClaimsPrincipal, object, IEnumerable<IAuthorizationRequirement>>((user, resource, requirements) =>
                Task.FromResult(user.Identity?.IsAuthenticated == true ? AuthorizationResult.Success() : AuthorizationResult.Failed()));
        mockAuthzService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>()))
            .Returns<ClaimsPrincipal, object, string>((user, resource, policy) =>
                Task.FromResult(user.Identity?.IsAuthenticated == true ? AuthorizationResult.Success() : AuthorizationResult.Failed()));
        Services.AddSingleton<IAuthorizationService>(mockAuthzService.Object);
        Services.AddSingleton<AuthenticationStateProvider>(provider => 
            new TestAuthenticationStateProvider(mockAuthStateProvider.Object));

        // Mock authenticated user
        var claimsIdentity = new System.Security.Claims.ClaimsIdentity(
            new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "admin") },
            "test");
        var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);
        var authState = new AuthenticationState(claimsPrincipal);

        mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => 
            parameters.AddChildContent<UserGreeting>());

        // Assert
        var logoutButton = component.Find("button");
        logoutButton.Should().NotBeNull();
        logoutButton.TextContent.Should().Contain("Выход");
    }
}
