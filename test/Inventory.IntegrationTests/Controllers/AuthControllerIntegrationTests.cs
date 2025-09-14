using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Xunit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Inventory.API.Services;
using System.Linq;
using System.Text.Json;

namespace Inventory.IntegrationTests.Controllers;

public class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class AuthControllerIntegrationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the production database context and all related services
                var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)).ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Remove Entity Framework services to avoid conflicts
                var efServices = services.Where(d => d.ServiceType.Name.Contains("EntityFramework") || 
                                                     d.ServiceType.Name.Contains("DbContext")).ToList();
                foreach (var service in efServices)
                {
                    services.Remove(service);
                }

                // Add in-memory database instead of production database
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                // Add port configuration service for testing
                services.AddSingleton<IPortConfigurationService, PortConfigurationService>();
            });
        });
    private readonly HttpClient _client = factory.CreateClient();

    private async Task CleanDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Удалить всех пользователей
        var users = await userManager.Users.ToListAsync();
        foreach (var user in users)
        {
            await userManager.DeleteAsync(user);
        }
        
        // Удалить все роли
        var roles = await roleManager.Roles.ToListAsync();
        foreach (var role in roles)
        {
            await roleManager.DeleteAsync(role);
        }
        
        // Очистить базу данных
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }

    private async Task CreateTestUserAsync(string username, string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Создать роль User если не существует
        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
        }
        
        // Создать пользователя
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };
        
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "User");
        }
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        await CleanDatabaseAsync();
        
        var uniqueUsername = $"testuser_{Guid.NewGuid():N}";
        var uniqueEmail = $"test_{Guid.NewGuid():N}@example.com";
        var password = "TestPassword123!";
        
        // Создать пользователя через API
        var registerRequest = new RegisterRequest
        {
            Username = uniqueUsername,
            Email = uniqueEmail,
            Password = password,
            ConfirmPassword = password
        };
        
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var request = new LoginRequest
        {
            Username = uniqueUsername,
            Password = password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.Username.Should().Be(uniqueUsername);
        result.Role.Should().Be("User");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "invalid",
            Password = "wrongpassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyCredentials_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "",
            Password = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithValidData_ShouldCreateUser()
    {
        // Arrange
        await CleanDatabaseAsync();
        
        var uniqueUsername = $"newuser_{Guid.NewGuid():N}";
        var uniqueEmail = $"newuser_{Guid.NewGuid():N}@example.com";
        var password = "NewPassword123!";

        var request = new RegisterRequest
        {
            Username = uniqueUsername,
            Email = uniqueEmail,
            Password = password,
            ConfirmPassword = password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.Should().NotBeNull();
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithExistingUsername_ShouldReturnBadRequest()
    {
        // Arrange
        await CleanDatabaseAsync();
        
        var existingUsername = $"existinguser_{Guid.NewGuid():N}";
        var existingEmail = $"existing_{Guid.NewGuid():N}@example.com";
        var differentEmail = $"different_{Guid.NewGuid():N}@example.com";
        
        // Создать существующего пользователя через API
        var firstRegisterRequest = new RegisterRequest
        {
            Username = existingUsername,
            Email = existingEmail,
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };
        
        var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", firstRegisterRequest);
        firstResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Попытка зарегистрировать пользователя с тем же именем, но другим email
        var secondRegisterRequest = new RegisterRequest
        {
            Username = existingUsername, // То же имя пользователя
            Email = differentEmail,
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", secondRegisterRequest);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}
