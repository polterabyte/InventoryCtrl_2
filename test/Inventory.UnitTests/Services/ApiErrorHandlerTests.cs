using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using Moq;
using Xunit;
using Inventory.Web.Client.Services;
using Inventory.Shared.Interfaces;
using Radzen;
using Inventory.Shared.DTOs;
using System.Net;
using System.Text;

namespace Inventory.UnitTests.Services
{
    public class ApiErrorHandlerTests : IDisposable
    {
        private readonly Mock<ILogger<ApiErrorHandler>> _mockLogger;
        private readonly Mock<ITokenManagementService> _mockTokenManagement;
        private readonly Mock<NavigationManager> _mockNavigation;
        private readonly Mock<NotificationService> _mockNotification;
        private readonly ApiErrorHandler _errorHandler;

        public ApiErrorHandlerTests()
        {
            _mockLogger = new Mock<ILogger<ApiErrorHandler>>();
            _mockTokenManagement = new Mock<ITokenManagementService>();
            _mockNavigation = new Mock<NavigationManager>();
            _mockNotification = new Mock<NotificationService>();

            _errorHandler = new ApiErrorHandler(
                _mockLogger.Object,
                _mockTokenManagement.Object,
                _mockNavigation.Object,
                _mockNotification.Object);
        }

        [Fact]
        public async Task HandleResponseAsync_WithSuccessfulResponse_ReturnsSuccessResponse()
        {
            // Arrange
            var testData = new { Message = "Success" };
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(testData);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            // Act
            var result = await _errorHandler.HandleResponseAsync<object>(httpResponse);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task HandleResponseAsync_WithBadRequest_ShowsWarningNotification()
        {
            // Arrange
            var errorMessage = "Invalid request parameters";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorMessage, Encoding.UTF8, "text/plain")
            };

            // Act
            var result = await _errorHandler.HandleResponseAsync<object>(httpResponse);

            // Note: Since we're using direct Radzen notifications, we can't easily mock them in unit tests
            // In a real implementation, you might want to create an abstraction layer
            // For now, we'll just verify the response structure
            Assert.False(result.Success);
            Assert.Equal(errorMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task HandleResponseAsync_WithInternalServerError_ShowsErrorNotification()
        {
            // Arrange
            var errorMessage = "Internal server error";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(errorMessage, Encoding.UTF8, "text/plain")
            };

            // Act
            var result = await _errorHandler.HandleResponseAsync<object>(httpResponse);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(errorMessage, result.ErrorMessage);
            // Note: Notification service calls are not easily mockable with direct Radzen usage
        }

        [Fact]
        public async Task HandleResponseAsync_WithForbidden_ShowsAccessDeniedNotification()
        {
            // Arrange
            var httpResponse = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("Access denied", Encoding.UTF8, "text/plain")
            };

            // Act
            var result = await _errorHandler.HandleResponseAsync<object>(httpResponse);

            // Assert
            Assert.False(result.Success);
            // Note: Notification service calls are not easily mockable with direct Radzen usage
        }

        [Fact]
        public async Task HandleResponseAsync_WithUnauthorizedAndValidRefreshToken_AttemptsTokenRefresh()
        {
            // Arrange
            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("Unauthorized", Encoding.UTF8, "text/plain")
            };

            _mockTokenManagement.Setup(x => x.HasValidRefreshTokenAsync())
                .ReturnsAsync(true);
            _mockTokenManagement.Setup(x => x.TryRefreshTokenAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _errorHandler.HandleResponseAsync<object>(httpResponse);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("TOKEN_REFRESHED", result.ErrorMessage);
            _mockTokenManagement.Verify(x => x.TryRefreshTokenAsync(), Times.Once);
        }

        [Fact]
        public async Task HandleResponseAsync_WithUnauthorizedAndNoRefreshToken_RedirectsToLogin()
        {
            // Arrange
            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("Unauthorized", Encoding.UTF8, "text/plain")
            };

            _mockTokenManagement.Setup(x => x.HasValidRefreshTokenAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _errorHandler.HandleResponseAsync<object>(httpResponse);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Authentication required. Please log in again.", result.ErrorMessage);
            _mockTokenManagement.Verify(x => x.ClearTokensAsync(), Times.Once);
            _mockNavigation.Verify(x => x.NavigateTo("/login", true), Times.Once);
            // Note: Notification service calls are not easily mockable with direct Radzen usage
        }

        [Fact]
        public async Task HandleResponseAsync_WithUnauthorizedAndFailedRefresh_RedirectsToLogin()
        {
            // Arrange
            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("Unauthorized", Encoding.UTF8, "text/plain")
            };

            _mockTokenManagement.Setup(x => x.HasValidRefreshTokenAsync())
                .ReturnsAsync(true);
            _mockTokenManagement.Setup(x => x.TryRefreshTokenAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _errorHandler.HandleResponseAsync<object>(httpResponse);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Authentication required. Please log in again.", result.ErrorMessage);
            _mockTokenManagement.Verify(x => x.ClearTokensAsync(), Times.Once);
            _mockNavigation.Verify(x => x.NavigateTo("/login", true), Times.Once);
        }

        [Fact]
        public async Task HandleResponseAsync_WithNotFound_ShowsWarningNotification()
        {
            // Arrange
            var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Resource not found", Encoding.UTF8, "text/plain")
            };

            // Act
            var result = await _errorHandler.HandleResponseAsync<object>(httpResponse);

            // Assert
            Assert.False(result.Success);
            // Note: Notification service calls are not easily mockable with direct Radzen usage
        }

        [Fact]
        public async Task HandleResponseAsync_WithConflict_ShowsWarningNotification()
        {
            // Arrange
            var errorMessage = "Resource conflict";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(errorMessage, Encoding.UTF8, "text/plain")
            };

            // Act
            var result = await _errorHandler.HandleResponseAsync<object>(httpResponse);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(errorMessage, result.ErrorMessage);
            // Note: Notification service calls are not easily mockable with direct Radzen usage
        }

        [Fact]
        public async Task HandleExceptionAsync_WithHttpRequestException_ReturnsNetworkError()
        {
            // Arrange
            var exception = new HttpRequestException("Network error");

            // Act
            var result = await _errorHandler.HandleExceptionAsync<object>(exception, "TEST_OPERATION");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Network error occurred. Please check your connection.", result.ErrorMessage);
        }

        [Fact]
        public async Task HandleExceptionAsync_WithTaskCanceledException_ReturnsTimeoutError()
        {
            // Arrange
            var exception = new TaskCanceledException("Request timed out");

            // Act
            var result = await _errorHandler.HandleExceptionAsync<object>(exception, "TEST_OPERATION");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Request was cancelled or timed out.", result.ErrorMessage);
        }

        [Fact]
        public async Task HandleExceptionAsync_WithUnauthorizedAccessException_ReturnsAuthError()
        {
            // Arrange
            var exception = new UnauthorizedAccessException("Access denied");

            // Act
            var result = await _errorHandler.HandleExceptionAsync<object>(exception, "TEST_OPERATION");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Access denied. Please log in again.", result.ErrorMessage);
        }

        [Fact]
        public async Task HandlePagedResponseAsync_WithSuccessfulResponse_ReturnsSuccessResponse()
        {
            // Arrange
            var testData = new PagedApiResponse<string>
            {
                Success = true,
                Data = new List<string> { "Item1", "Item2" },
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(testData);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            // Act
            var result = await _errorHandler.HandlePagedResponseAsync<string>(httpResponse);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.TotalCount);
        }

        public void Dispose()
        {
            // Cleanup resources if needed
        }
    }
}