using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Inventory.API.Controllers;
using Inventory.API.Services;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using System.Collections.Generic;
using System.Threading;

namespace Inventory.UnitTests.Controllers
{
    public class RequestsControllerErrorHandlingTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<IRequestService> _mockService;
        private readonly Mock<ILogger<RequestsController>> _mockLogger;
        private readonly RequestsController _controller;

        public RequestsControllerErrorHandlingTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new AppDbContext(options);
            _mockService = new Mock<IRequestService>();
            _mockLogger = new Mock<ILogger<RequestsController>>();
            
            _controller = new RequestsController(_dbContext, _mockService.Object, _mockLogger.Object);
            
            // Setup user context
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id")
            }));
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            };
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        [Fact]
        public async Task Create_WithValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var requestBody = new CreateRequestDto
            {
                Title = "Test Request",
                Description = "Test Description",
                Items = new List<RequestItemInputDto>
                {
                    new()
                    {
                        ProductId = 1,
                        WarehouseId = 1,
                        Quantity = 1
                    }
                }
            };

            var expectedRequest = new Request
            {
                Id = 1,
                Title = "Test Request",
                Description = "Test Description",
                Status = RequestStatus.Draft,
                CreatedByUserId = "test-user-id",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Requests.Add(expectedRequest);
            await _dbContext.SaveChangesAsync();

            _mockService.Setup(x => x.CreateRequestAsync(
                    It.IsAny<CreateRequestDto>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedRequest);

            // Act
            var result = await _controller.Create(requestBody);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal("GetById", createdResult.ActionName);
            Assert.Equal(expectedRequest.Id, createdResult.RouteValues!["id"]);
            Assert.NotNull(createdResult.Value);

            _mockService.Verify(x => x.CreateRequestAsync(
                It.Is<CreateRequestDto>(dto => dto.Title == "Test Request" && dto.Description == "Test Description"),
                "test-user-id",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Create_WithArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var requestBody = new CreateRequestDto
            {
                Title = string.Empty,
                Description = string.Empty,
                Items = new List<RequestItemInputDto>
                {
                    new()
                    {
                        ProductId = 1,
                        WarehouseId = 1,
                        Quantity = 1
                    }
                }
            }; // Invalid title
            var exceptionMessage = "Title cannot be empty";
            
            _mockService.Setup(x => x.CreateRequestAsync(
                    It.IsAny<CreateRequestDto>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException(exceptionMessage));

            // Act
            var result = await _controller.Create(requestBody);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            Assert.NotNull(response);
            
            // Check that the response contains error information
            var responseType = response.GetType();
            var successProperty = responseType.GetProperty("success");
            var errorMessageProperty = responseType.GetProperty("errorMessage");
            
            Assert.False((bool)successProperty?.GetValue(response)!);
            Assert.Equal(exceptionMessage, errorMessageProperty?.GetValue(response));
        }

        [Fact]
        public async Task Create_WithInvalidOperationException_ReturnsConflict()
        {
            // Arrange
            var requestBody = new CreateRequestDto
            {
                Title = "Test Request",
                Description = "Test Description",
                Items = new List<RequestItemInputDto>
                {
                    new()
                    {
                        ProductId = 1,
                        WarehouseId = 1,
                        Quantity = 1
                    }
                }
            };
            var exceptionMessage = "Cannot create request at this time";
            
            _mockService.Setup(x => x.CreateRequestAsync(
                    It.IsAny<CreateRequestDto>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(exceptionMessage));

            // Act
            var result = await _controller.Create(requestBody);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            var response = conflictResult.Value;
            Assert.NotNull(response);
            
            var responseType = response.GetType();
            var successProperty = responseType.GetProperty("success");
            var errorMessageProperty = responseType.GetProperty("errorMessage");
            
            Assert.False((bool)successProperty?.GetValue(response)!);
            Assert.Equal(exceptionMessage, errorMessageProperty?.GetValue(response));
        }

        [Fact]
        public async Task Create_WithUnexpectedException_ReturnsInternalServerError()
        {
            // Arrange
            var requestBody = new CreateRequestDto
            {
                Title = "Test Request",
                Description = "Test Description",
                Items = new List<RequestItemInputDto>
                {
                    new()
                    {
                        ProductId = 1,
                        WarehouseId = 1,
                        Quantity = 1
                    }
                }
            };
            var exceptionMessage = "Database connection failed";
            
            _mockService.Setup(x => x.CreateRequestAsync(
                    It.IsAny<CreateRequestDto>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.Create(requestBody);

            // Assert
            var serverErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverErrorResult.StatusCode);
            
            var response = serverErrorResult.Value;
            Assert.NotNull(response);
            
            var responseType = response.GetType();
            var successProperty = responseType.GetProperty("success");
            var errorMessageProperty = responseType.GetProperty("errorMessage");
            
            Assert.False((bool)successProperty?.GetValue(response)!);
            Assert.Equal("An error occurred while creating the request. Please try again.", 
                errorMessageProperty?.GetValue(response));
        }

        [Fact]
        public async Task AddItem_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var requestId = 1;
            var addItemBody = new AddRequestItemDto
            {
                ProductId = 1,
                WarehouseId = 1,
                Quantity = 10,
                LocationId = 1,
                Description = "Test item"
            };
            var expectedTransaction = new InventoryTransaction
            {
                Id = 1,
                ProductId = 1,
                WarehouseId = 1,
                Quantity = 10,
                RequestId = requestId
            };

            _mockService.Setup(x => x.AddPendingItemAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTransaction);

            // Act
            var result = await _controller.AddItem(requestId, addItemBody);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);
        }

        [Fact]
        public async Task AddItem_WithInvalidOperationException_ReturnsBadRequest()
        {
            // Arrange
            var requestId = 1;
            var addItemBody = new AddRequestItemDto
            {
                ProductId = 1,
                WarehouseId = 1,
                Quantity = 10,
                LocationId = 1,
                Description = "Test item"
            };
            var exceptionMessage = "Request not found";
            
            _mockService.Setup(x => x.AddPendingItemAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(exceptionMessage));

            // Act
            var result = await _controller.AddItem(requestId, addItemBody);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            Assert.NotNull(response);
            
            var responseType = response.GetType();
            var errorMessageProperty = responseType.GetProperty("errorMessage");
            Assert.Equal(exceptionMessage, errorMessageProperty?.GetValue(response));
        }

        [Fact]
        public async Task Submit_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var requestId = 1;
            var transitionBody = new RequestsController.TransitionBody("Submitting for approval");
            var expectedRequest = new Request
            {
                Id = requestId,
                Title = "Test Request",
                Status = RequestStatus.Submitted
            };

            _mockService.Setup(x => x.SubmitAsync(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedRequest);

            // Act
            var result = await _controller.Submit(requestId, transitionBody);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedRequest, okResult.Value);
        }

        [Fact]
        public async Task Submit_WithInvalidOperationException_ReturnsBadRequest()
        {
            // Arrange
            var requestId = 1;
            var transitionBody = new RequestsController.TransitionBody("Test comment");
            var exceptionMessage = "Invalid status transition from Draft";
            
            _mockService.Setup(x => x.SubmitAsync(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(exceptionMessage));

            // Act
            var result = await _controller.Submit(requestId, transitionBody);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            Assert.NotNull(response);
            
            var responseType = response.GetType();
            var errorMessageProperty = responseType.GetProperty("errorMessage");
            Assert.Equal(exceptionMessage, errorMessageProperty?.GetValue(response));
        }

        [Fact]
        public async Task Submit_WithUnexpectedException_ReturnsInternalServerError()
        {
            // Arrange
            var requestId = 1;
            var transitionBody = new RequestsController.TransitionBody("Test comment");
            
            _mockService.Setup(x => x.SubmitAsync(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Submit(requestId, transitionBody);

            // Assert
            var serverErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverErrorResult.StatusCode);
            
            var response = serverErrorResult.Value;
            Assert.NotNull(response);
            
            var responseType = response.GetType();
            var errorMessageProperty = responseType.GetProperty("errorMessage");
            Assert.Equal("An error occurred while submitting the request. Please try again.", 
                errorMessageProperty?.GetValue(response));
        }
    }
}