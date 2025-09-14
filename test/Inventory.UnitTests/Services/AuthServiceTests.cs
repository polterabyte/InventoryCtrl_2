using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Inventory.API.Controllers;
using Inventory.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Inventory.API.Models;
using Microsoft.Extensions.Configuration;
using Xunit;
using Inventory.API.Services;

namespace Inventory.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly Mock<IPortConfigurationService> _portServiceMock;
    private readonly AuthController _authController;

    public AuthServiceTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _portServiceMock = new Mock<IPortConfigurationService>();

        _authController = new AuthController(
            _userManagerMock.Object,
            _configMock.Object,
            _loggerMock.Object,
            _portServiceMock.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "admin",
            Password = "Admin123!"
        };

        var user = new User
        {
            UserName = "admin",
            Role = "Admin"
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(request.Username))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);
        _userManagerMock.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(new List<System.Security.Claims.Claim>());
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin" });

        _configMock.Setup(x => x["Jwt:Key"]).Returns("YourSuperSecretKeyThatIsAtLeast32CharactersLong");
        _configMock.Setup(x => x["Jwt:Issuer"]).Returns("InventoryControl");
        _configMock.Setup(x => x["Jwt:Audience"]).Returns("InventoryControl");

        // Act
        var result = await _authController.Login(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.OkObjectResult>();
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

        _userManagerMock.Setup(x => x.FindByNameAsync(request.Username))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authController.Login(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.UnauthorizedObjectResult>();
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
        var result = await _authController.Login(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>();
    }
}
