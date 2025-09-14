using Bunit;
using FluentAssertions;
using Inventory.UI.Pages;
using Inventory.Shared.Interfaces;
using Inventory.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Xunit;
using Microsoft.AspNetCore.Components;

namespace Inventory.ComponentTests.Components;

public class HomeTestAuthenticationStateProvider(ICustomAuthenticationStateProvider customProvider) : AuthenticationStateProvider
{
    private readonly ICustomAuthenticationStateProvider _customProvider = customProvider;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return await _customProvider.GetAuthenticationStateAsync();
    }
}

public class TestNavigationManager : NavigationManager
{
    public TestNavigationManager() : base()
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

public class HomePageTests : TestContext
{
    [Fact]
    public async Task HomePage_UnauthorizedUser_ShouldRedirectToLogin()
    {
        // Arrange
        var mockDashboardService = new Mock<IDashboardService>();
        var mockAuthStateProvider = new Mock<ICustomAuthenticationStateProvider>();
        var navigationManager = new TestNavigationManager();

        Services.AddSingleton(mockDashboardService.Object);
        Services.AddSingleton(mockAuthStateProvider.Object);
        Services.AddSingleton<NavigationManager>(navigationManager);
        
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
            new HomeTestAuthenticationStateProvider(mockAuthStateProvider.Object));

        // Mock unauthenticated user
        var claimsIdentity = new ClaimsIdentity();
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var authState = new AuthenticationState(claimsPrincipal);

        mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => 
            parameters.AddChildContent<Home>());

        // Wait for navigation to complete
        await Task.Delay(600); // Wait for RedirectToLogin delay + navigation

        // Assert
        navigationManager.NavigationCount.Should().Be(1);
        navigationManager.LastNavigatedUrl.Should().Be("/login");
    }

    [Fact]
    public void HomePage_AuthorizedUser_ShouldLoadDashboardStats()
    {
        // Arrange
        var mockDashboardService = new Mock<IDashboardService>();
        var mockAuthStateProvider = new Mock<ICustomAuthenticationStateProvider>();
        var navigationManager = new TestNavigationManager();

        var expectedStats = new DashboardStatsDto
        {
            TotalProducts = 100,
            TotalCategories = 10,
            TotalManufacturers = 5,
            TotalWarehouses = 3,
            LowStockProducts = 15,
            OutOfStockProducts = 5
        };

        mockDashboardService.Setup(x => x.GetDashboardStatsAsync())
            .ReturnsAsync(expectedStats);

        Services.AddSingleton(mockDashboardService.Object);
        Services.AddSingleton(mockAuthStateProvider.Object);
        Services.AddSingleton<NavigationManager>(navigationManager);
        
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
            new HomeTestAuthenticationStateProvider(mockAuthStateProvider.Object));

        // Mock authenticated user
        var claimsIdentity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "admin") },
            "test");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var authState = new AuthenticationState(claimsPrincipal);

        mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => 
            parameters.AddChildContent<Home>());

        // Assert
        navigationManager.NavigationCount.Should().Be(0);
        mockDashboardService.Verify(x => x.GetDashboardStatsAsync(), Times.Once);
        
        // Verify dashboard content is rendered
        component.Markup.Should().Contain("Добро пожаловать в систему управления складом!");
        component.Markup.Should().Contain("admin");
    }

    [Fact]
    public void HomePage_NotAuthorized_ShouldShowRedirectMessage()
    {
        // Arrange
        var mockDashboardService = new Mock<IDashboardService>();
        var mockAuthStateProvider = new Mock<ICustomAuthenticationStateProvider>();
        var navigationManager = new TestNavigationManager();

        Services.AddSingleton(mockDashboardService.Object);
        Services.AddSingleton(mockAuthStateProvider.Object);
        Services.AddSingleton<NavigationManager>(navigationManager);
        
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
            new HomeTestAuthenticationStateProvider(mockAuthStateProvider.Object));

        // Mock unauthenticated user
        var claimsIdentity = new ClaimsIdentity();
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var authState = new AuthenticationState(claimsPrincipal);

        mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => 
            parameters.AddChildContent<Home>());

        // Assert
        component.Markup.Should().Contain("Система управления складом");
        component.Markup.Should().Contain("Перенаправление на страницу авторизации");
        component.Markup.Should().Contain("spinner-border");
    }

    [Fact]
    public void HomePage_DashboardServiceError_ShouldHandleGracefully()
    {
        // Arrange
        var mockDashboardService = new Mock<IDashboardService>();
        var mockAuthStateProvider = new Mock<ICustomAuthenticationStateProvider>();
        var navigationManager = new TestNavigationManager();

        mockDashboardService.Setup(x => x.GetDashboardStatsAsync())
            .ThrowsAsync(new Exception("Service error"));

        Services.AddSingleton(mockDashboardService.Object);
        Services.AddSingleton(mockAuthStateProvider.Object);
        Services.AddSingleton<NavigationManager>(navigationManager);
        
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
            new HomeTestAuthenticationStateProvider(mockAuthStateProvider.Object));

        // Mock authenticated user
        var claimsIdentity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "admin") },
            "test");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var authState = new AuthenticationState(claimsPrincipal);

        mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => 
            parameters.AddChildContent<Home>());

        // Assert
        navigationManager.NavigationCount.Should().Be(0);
        mockDashboardService.Verify(x => x.GetDashboardStatsAsync(), Times.Once);
        
        // Component should still render without crashing
        component.Markup.Should().Contain("Добро пожаловать в систему управления складом!");
    }
}
