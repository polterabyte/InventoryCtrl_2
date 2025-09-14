using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Inventory.API.Controllers;
using Inventory.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Inventory.API.Models;
using Microsoft.Extensions.Configuration;
using Inventory.API.Services;
using Xunit;

namespace Inventory.UnitTests.Services;

public class SimpleAuthServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;

    public SimpleAuthServiceTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AuthController>>();

        // Mock configuration
        _configMock.Setup(x => x["Jwt:Key"]).Returns("YourSuperSecretKeyThatIsAtLeast32CharactersLong");
        _configMock.Setup(x => x["Jwt:Issuer"]).Returns("InventoryControl");
        _configMock.Setup(x => x["Jwt:Audience"]).Returns("InventoryControl");
    }

    [Fact]
    public void AuthController_ShouldBeCreated()
    {
        // Arrange
        var mockPortService = new Mock<IPortConfigurationService>();

        // Act
        var authController = new AuthController(
            _userManagerMock.Object,
            _configMock.Object,
            _loggerMock.Object,
            mockPortService.Object);

        // Assert
        authController.Should().NotBeNull();
    }

    [Fact]
    public void LoginRequest_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "testpass"
        };

        // Assert
        request.Username.Should().Be("testuser");
        request.Password.Should().Be("testpass");
    }

    [Fact]
    public void User_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.UserName.Should().BeNull();
        user.Email.Should().BeNull();
        user.Role.Should().BeEmpty();
        user.Transactions.Should().NotBeNull();
        user.Transactions.Should().BeEmpty();
    }
}
