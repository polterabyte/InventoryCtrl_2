using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Inventory.API.Services;
using Inventory.Shared.Interfaces;
using Inventory.Shared.DTOs;
using ApiUser = Inventory.API.Models.User;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Inventory.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<UserManager<ApiUser>> _mockUserManager;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly Mock<IRefreshTokenService> _mockRefreshTokenService;
        private readonly Mock<IInternalAuditService> _mockAuditService;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(s => s.Value).Returns("a_super_secret_key_that_is_long_enough_for_testing");
            _mockConfiguration.Setup(c => c.GetSection("Jwt:Key")).Returns(mockConfigSection.Object);
            _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
            _mockConfiguration.Setup(c => c["Jwt:ExpireMinutes"]).Returns("15");


            var userStoreMock = new Mock<IUserStore<ApiUser>>();
            _mockUserManager = new Mock<UserManager<ApiUser>>(
                userStoreMock.Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<ApiUser>>().Object,
                new IUserValidator<ApiUser>[0],
                new IPasswordValidator<ApiUser>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<ApiUser>>>().Object
            );

            _mockLogger = new Mock<ILogger<AuthService>>();
            _mockRefreshTokenService = new Mock<IRefreshTokenService>();
            _mockAuditService = new Mock<IInternalAuditService>();
            _mockAuditService.Setup(s => s.LogDetailedChangeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Inventory.API.Enums.ActionType>(),
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()
            )).Returns(Task.CompletedTask);

            _authService = new AuthService(
                _mockUserManager.Object,
                _mockConfiguration.Object,
                _mockRefreshTokenService.Object,
                _mockAuditService.Object,
                Mock.Of<Serilog.ILogger>()
            );
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnSuccess()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "Password123!" };
            var user = new ApiUser { UserName = "testuser", Email = "test@example.com" };

            _mockUserManager.Setup(x => x.FindByNameAsync(request.Username)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
            _mockRefreshTokenService.Setup(s => s.GenerateRefreshToken()).Returns("new-refresh-token");
            _mockRefreshTokenService.Setup(s => s.GenerateAccessTokenAsync(user)).ReturnsAsync("new-access-token");

            // Act
            var result = await _authService.LoginAsync(request, "::1", "test-agent", "test-request-id");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("new-access-token", result.Data.Token);
            Assert.Equal("new-refresh-token", result.Data.RefreshToken);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidUsername_ShouldReturnError()
        {
            // Arrange
            var request = new LoginRequest { Username = "wronguser", Password = "Password123!" };
            _mockUserManager.Setup(x => x.FindByNameAsync(request.Username)).ReturnsAsync((ApiUser?)null);

            // Act
            var result = await _authService.LoginAsync(request, "::1", "test-agent", "test-request-id");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid credentials", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ShouldReturnError()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "WrongPassword!" };
            var user = new ApiUser { UserName = "testuser" };

            _mockUserManager.Setup(x => x.FindByNameAsync(request.Username)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(false);

            // Act
            var result = await _authService.LoginAsync(request, "::1", "test-agent", "test-request-id");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid credentials", result.ErrorMessage);
        }
    }
}
