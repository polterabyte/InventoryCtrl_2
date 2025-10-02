using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Inventory.Web.Client.Services;
using Inventory.Shared.DTOs;
using Inventory.Shared.Constants;
using Inventory.Shared.Interfaces;
using System.Threading.Tasks;

namespace Inventory.UnitTests.Services
{
    public class AutoTokenRefreshServiceTests
    {
        private readonly Mock<ILogger<AutoTokenRefreshService>> _mockLogger;
        private readonly Mock<ITokenManagementService> _mockTokenManagement;
        private readonly AutoTokenRefreshService _service;

        public AutoTokenRefreshServiceTests()
        {
            _mockLogger = new Mock<ILogger<AutoTokenRefreshService>>();
            _mockTokenManagement = new Mock<ITokenManagementService>();
            _service = new AutoTokenRefreshService(_mockLogger.Object, _mockTokenManagement.Object);
        }

        [Fact]
        public async Task ExecuteWithAutoRefreshAsync_WhenSuccessful_ReturnsOriginalResponse()
        {
            // Arrange
            var expectedResponse = new ApiResponse<string>
            {
                Success = true,
                Data = "test data"
            };

            // Act
            var result = await _service.ExecuteWithAutoRefreshAsync(() => Task.FromResult(expectedResponse));

            // Assert
            Assert.True(result.Success);
            Assert.Equal(expectedResponse.Data, result.Data);
            _mockTokenManagement.Verify(x => x.TryRefreshTokenAsync(), Times.Never);
        }

        [Fact]
        public async Task ExecuteWithAutoRefreshAsync_WhenTokenRefreshNeeded_RefreshesAndRetries()
        {
            // Arrange
            var failedResponse = new ApiResponse<string>
            {
                Success = false,
                ErrorMessage = ApiResponseCodes.TokenRefreshed
            };

            var successResponse = new ApiResponse<string>
            {
                Success = true,
                Data = "test data"
            };

            var callCount = 0;
            Func<Task<ApiResponse<string>>> operation = () =>
            {
                callCount++;
                return Task.FromResult(callCount == 1 ? failedResponse : successResponse);
            };

            _mockTokenManagement.Setup(x => x.TryRefreshTokenAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.ExecuteWithAutoRefreshAsync(operation);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(successResponse.Data, result.Data);
            Assert.Equal(2, callCount);
            _mockTokenManagement.Verify(x => x.TryRefreshTokenAsync(), Times.Once);
        }

        [Fact]
        public async Task ExecuteWithAutoRefreshAsync_WhenTokenRefreshFails_ReturnsOriginalFailedResponse()
        {
            // Arrange
            var failedResponse = new ApiResponse<string>
            {
                Success = false,
                ErrorMessage = ApiResponseCodes.TokenRefreshed
            };

            _mockTokenManagement.Setup(x => x.TryRefreshTokenAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _service.ExecuteWithAutoRefreshAsync(() => Task.FromResult(failedResponse));

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ApiResponseCodes.TokenRefreshed, result.ErrorMessage);
            _mockTokenManagement.Verify(x => x.TryRefreshTokenAsync(), Times.Once);
        }

        [Fact]
        public async Task ExecutePagedWithAutoRefreshAsync_WhenSuccessful_ReturnsOriginalResponse()
        {
            // Arrange
            var expectedResponse = new PagedApiResponse<string>
            {
                Success = true,
                Data = new PagedResponse<string>
                {
                    Items = new List<string> { "test data" },
                    total = 1,
                    page = 1,
                    PageSize = 10
                }
            };

            // Act
            var result = await _service.ExecutePagedWithAutoRefreshAsync(() => Task.FromResult(expectedResponse));

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.NotNull(expectedResponse.Data);
            Assert.Equal(expectedResponse.Data.Items, result.Data.Items);
            Assert.Equal(expectedResponse.Data.total, result.Data.total);
            _mockTokenManagement.Verify(x => x.TryRefreshTokenAsync(), Times.Never);
        }

        [Fact]
        public async Task ExecutePagedWithAutoRefreshAsync_WhenTokenRefreshNeeded_RefreshesAndRetries()
        {
            // Arrange
            var failedResponse = new PagedApiResponse<string>
            {
                Success = false,
                ErrorMessage = ApiResponseCodes.TokenRefreshed
            };

            var successResponse = new PagedApiResponse<string>
            {
                Success = true,
                Data = new PagedResponse<string>
                {
                    Items = new List<string> { "test data" },
                    total = 1,
                    page = 1,
                    PageSize = 10
                }
            };

            var callCount = 0;
            Func<Task<PagedApiResponse<string>>> operation = () =>
            {
                callCount++;
                return Task.FromResult(callCount == 1 ? failedResponse : successResponse);
            };

            _mockTokenManagement.Setup(x => x.TryRefreshTokenAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.ExecutePagedWithAutoRefreshAsync(operation);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.NotNull(successResponse.Data);
            Assert.Equal(successResponse.Data.Items, result.Data.Items);
            Assert.Equal(successResponse.Data.total, result.Data.total);
            Assert.Equal(2, callCount);
            _mockTokenManagement.Verify(x => x.TryRefreshTokenAsync(), Times.Once);
        }

        [Fact]
        public async Task ExecutePagedWithAutoRefreshAsync_WhenTokenRefreshFails_ReturnsOriginalFailedResponse()
        {
            // Arrange
            var failedResponse = new PagedApiResponse<string>
            {
                Success = false,
                ErrorMessage = ApiResponseCodes.TokenRefreshed
            };

            _mockTokenManagement.Setup(x => x.TryRefreshTokenAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _service.ExecutePagedWithAutoRefreshAsync(() => Task.FromResult(failedResponse));

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ApiResponseCodes.TokenRefreshed, result.ErrorMessage);
            _mockTokenManagement.Verify(x => x.TryRefreshTokenAsync(), Times.Once);
        }
    }
}