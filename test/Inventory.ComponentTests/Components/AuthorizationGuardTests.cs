using Bunit;
using FluentAssertions;
using Inventory.UI.Components;
using Inventory.Shared.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Inventory.ComponentTests.Components;

public class AuthGuardTestAuthStateProvider(ICustomAuthenticationStateProvider customProvider) : AuthenticationStateProvider
{
    private readonly ICustomAuthenticationStateProvider _customProvider = customProvider;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return await _customProvider.GetAuthenticationStateAsync();
    }
}

public class AuthGuardTestNavigationManager : NavigationManager
{
    public AuthGuardTestNavigationManager() : base()
    {
        Initialize("http://localhost:5000/", "http://localhost:5000/");
    }

    public string? LastNavigatedUrl { get; private set; }
    public int NavigationCount { get; private set; }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        LastNavigatedUrl = uri;
        NavigationCount++;
    }
}

public class AuthorizationGuardTests : TestContext
{
    [Fact]
    public void AuthorizationGuard_AuthenticatedUser_ShouldRenderChildContent()
    {
        // Arrange
        var mockAuthStateProvider = new Mock<ICustomAuthenticationStateProvider>();
        var navigationManager = new AuthGuardTestNavigationManager();

        // Mock authenticated user
        var claimsIdentity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "admin") },
            "test");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var authState = new AuthenticationState(claimsPrincipal);

        mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        Services.AddSingleton(mockAuthStateProvider.Object);
        Services.AddSingleton<NavigationManager>(navigationManager);
        Services.AddSingleton<AuthenticationStateProvider>(provider => 
            new AuthGuardTestAuthStateProvider(mockAuthStateProvider.Object));

        // Act
        var component = RenderComponent<AuthorizationGuard>(parameters => 
            parameters.AddChildContent("<div>Authorized content</div>"));

        // Assert
        component.Markup.Should().Contain("Authorized content");
    }

    [Fact]
    public void AuthorizationGuard_UnauthenticatedUser_ShouldRedirectToLogin()
    {
        // Arrange
        var mockAuthStateProvider = new Mock<ICustomAuthenticationStateProvider>();
        var navigationManager = new AuthGuardTestNavigationManager();

        // Mock unauthenticated user
        var claimsIdentity = new ClaimsIdentity();
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var authState = new AuthenticationState(claimsPrincipal);

        mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        Services.AddSingleton(mockAuthStateProvider.Object);
        Services.AddSingleton<NavigationManager>(navigationManager);
        Services.AddSingleton<AuthenticationStateProvider>(provider => 
            new AuthGuardTestAuthStateProvider(mockAuthStateProvider.Object));

        // Act
        var component = RenderComponent<AuthorizationGuard>(parameters => 
            parameters.AddChildContent("<div>Authorized content</div>"));

        // Assert
        navigationManager.NavigationCount.Should().Be(1);
        navigationManager.LastNavigatedUrl.Should().Be("/login");
    }

    [Fact]
    public void AuthorizationGuard_WithRequiredRole_ShouldCheckRole()
    {
        // Arrange
        var mockAuthStateProvider = new Mock<ICustomAuthenticationStateProvider>();
        var navigationManager = new AuthGuardTestNavigationManager();

        // Mock authenticated user with Admin role
        var claimsIdentity = new ClaimsIdentity(
            new[] { 
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            },
            "test");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var authState = new AuthenticationState(claimsPrincipal);

        mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        Services.AddSingleton(mockAuthStateProvider.Object);
        Services.AddSingleton<NavigationManager>(navigationManager);
        Services.AddSingleton<AuthenticationStateProvider>(provider => 
            new AuthGuardTestAuthStateProvider(mockAuthStateProvider.Object));

        // Act
        var component = RenderComponent<AuthorizationGuard>(parameters => 
        {
            parameters.Add(p => p.RequiredRole, "Admin");
            parameters.AddChildContent("<div>Admin content</div>");
        });

        // Assert
        component.Markup.Should().Contain("Admin content");
    }

    [Fact]
    public void AuthorizationGuard_WithoutRequiredRole_ShouldShowAccessDenied()
    {
        // Arrange
        var mockAuthStateProvider = new Mock<ICustomAuthenticationStateProvider>();
        var navigationManager = new AuthGuardTestNavigationManager();

        // Mock authenticated user without Admin role
        var claimsIdentity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "user") },
            "test");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var authState = new AuthenticationState(claimsPrincipal);

        mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        Services.AddSingleton(mockAuthStateProvider.Object);
        Services.AddSingleton<NavigationManager>(navigationManager);
        Services.AddSingleton<AuthenticationStateProvider>(provider => 
            new AuthGuardTestAuthStateProvider(mockAuthStateProvider.Object));

        // Act
        var component = RenderComponent<AuthorizationGuard>(parameters => 
        {
            parameters.Add(p => p.RequiredRole, "Admin");
            parameters.AddChildContent("<div>Admin content</div>");
        });

        // Assert
        component.Markup.Should().Contain("Access Denied");
        component.Markup.Should().NotContain("Admin content");
    }
}
