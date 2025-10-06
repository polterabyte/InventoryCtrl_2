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
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Interfaces;

namespace Inventory.UnitTests.Services;

public class SimpleAuthServiceTests
{
    private readonly Mock<AuthService> _authServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;

    public SimpleAuthServiceTests()
    {
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _authServiceMock = new Mock<AuthService>(
            new Mock<UserManager<User>>(Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!).Object,
            new Mock<IConfiguration>().Object,
            _refreshTokenServiceMock.Object,
            new Mock<IInternalAuditService>().Object,
            new Mock<Serilog.ILogger>().Object
            );
        _loggerMock = new Mock<ILogger<AuthController>>();
    }

    [Fact]
    public void AuthController_ShouldBeCreated()
    {
        // Arrange
        var authController = new AuthController(
            _authServiceMock.Object,
            _loggerMock.Object);

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
        user.FirstName.Should().BeEmpty();
        user.LastName.Should().BeEmpty();
        user.Transactions.Should().NotBeNull();
        user.Transactions.Should().BeEmpty();
    }
}
