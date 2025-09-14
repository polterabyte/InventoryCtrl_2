using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using FluentAssertions;
using Inventory.Shared.DTOs;
using Inventory.API.Models;
using Xunit;

namespace Inventory.IntegrationTests.Controllers;

public class AuthControllerIntegrationTestsNew : IntegrationTestBase
{
    public AuthControllerIntegrationTestsNew(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SeedTestDataAsync();
        var loginRequest = new LoginRequest
        {
            Username = "testadmin",
            Password = "Admin123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("testadmin");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SeedTestDataAsync();
        var loginRequest = new LoginRequest
        {
            Username = "testadmin",
            Password = "WrongPassword"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid credentials");
    }

    [Fact]
    public async Task Login_WithEmptyCredentials_ShouldReturnBadRequest()
    {
        // Arrange
        await InitializeAsync();
        var loginRequest = new LoginRequest
        {
            Username = "",
            Password = ""
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("validation errors");
    }

    [Fact]
    public async Task Register_WithValidData_ShouldCreateUser()
    {
        // Arrange
        await InitializeAsync();
        var registerRequest = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "NewUser123!",
            ConfirmPassword = "NewUser123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("User created successfully");
    }

    [Fact]
    public async Task Register_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        await InitializeAsync();
        var registerRequest = new RegisterRequest
        {
            Username = "",
            Email = "invalid-email",
            Password = "123",
            ConfirmPassword = "456"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithExistingUsername_ShouldReturnBadRequest()
    {
        // Arrange
        await InitializeAsync();
        await CleanupDatabaseAsync();
        await SeedTestDataAsync();
        var registerRequest = new RegisterRequest
        {
            Username = "testadmin", // Already exists
            Email = "different@example.com",
            Password = "NewUser123!",
            ConfirmPassword = "NewUser123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Username already exists");
    }
}
