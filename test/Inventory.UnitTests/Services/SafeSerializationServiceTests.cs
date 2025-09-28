using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Inventory.API.Services;
using Inventory.API.Models;
using System.Text.Json;

namespace Inventory.UnitTests.Services
{
    public class SafeSerializationServiceTests
    {
        private readonly Mock<ILogger<SafeSerializationService>> _mockLogger;
        private readonly SafeSerializationService _service;

        public SafeSerializationServiceTests()
        {
            _mockLogger = new Mock<ILogger<SafeSerializationService>>();
            _service = new SafeSerializationService(_mockLogger.Object);
        }

        [Fact]
        public void SafeSerialize_WithNullObject_ReturnsNull()
        {
            // Act
            var result = _service.SafeSerialize(null);

            // Assert
            Assert.Equal("null", result);
        }

        [Fact]
        public void SafeSerialize_WithSimpleObject_ReturnsJsonString()
        {
            // Arrange
            var obj = new { Name = "Test", Value = 123 };

            // Act
            var result = _service.SafeSerialize(obj);

            // Assert
            Assert.Contains("\"name\"", result.ToLowerInvariant());
            Assert.Contains("\"test\"", result.ToLowerInvariant());
            Assert.Contains("123", result);
        }

        [Fact]
        public void SafeSerialize_WithCircularReference_ReturnsSafeRepresentation()
        {
            // Arrange
            var request = new Request { Id = 1, Title = "Test Request" };
            var history = new RequestHistory { Id = 1, RequestId = 1, Request = request };
            request.History.Add(history);

            // Act
            var result = _service.SafeSerialize(request);

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain("\"history\"", result.ToLowerInvariant()); // Should be excluded due to JsonIgnore
        }

        [Fact]
        public void CreateAuditSafeRepresentation_WithEntity_ExcludesNavigationProperties()
        {
            // Arrange
            var request = new Request 
            { 
                Id = 1, 
                Title = "Test Request",
                Status = RequestStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };
            var history = new RequestHistory { Id = 1, RequestId = 1, Request = request };
            request.History.Add(history);

            // Act
            var result = _service.CreateAuditSafeRepresentation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ContainsKey("Id"));
            Assert.True(result.ContainsKey("Title"));
            Assert.Equal(1, result["Id"]);
            Assert.Equal("Test Request", result["Title"]);
            
            // Navigation properties should be excluded or simplified
            Assert.False(result.ContainsKey("History") || 
                        (result.ContainsKey("History") && result["History"]?.ToString()?.Contains("<") == true));
        }

        [Fact]
        public void CreateAuditSafeRepresentation_WithInventoryTransaction_HandlesRequestNavigation()
        {
            // Arrange
            var request = new Request { Id = 1, Title = "Test Request" };
            var transaction = new InventoryTransaction
            {
                Id = 1,
                ProductId = 1,
                WarehouseId = 1,
                Type = TransactionType.Income,
                Quantity = 10,
                Date = DateTime.UtcNow,
                UserId = "user1",
                RequestId = 1,
                Request = request
            };

            // Act
            var result = _service.CreateAuditSafeRepresentation(transaction);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ContainsKey("Id"));
            Assert.True(result.ContainsKey("ProductId"));
            Assert.True(result.ContainsKey("Quantity"));
            Assert.Equal(1, result["Id"]);
            Assert.Equal(10, result["Quantity"]);
            
            // Request navigation property should be excluded due to JsonIgnore
            Assert.False(result.ContainsKey("Request"));
        }

        [Fact]
        public void SafeSerialize_WithComplexObjectGraph_HandlesGracefully()
        {
            // Arrange
            var complexObject = new
            {
                Level1 = new
                {
                    Level2 = new
                    {
                        Level3 = new
                        {
                            Data = "Deep nested data",
                            Numbers = new[] { 1, 2, 3, 4, 5 }
                        }
                    }
                },
                SimpleProperty = "Simple value"
            };

            // Act
            var result = _service.SafeSerialize(complexObject);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("deep nested data", result.ToLowerInvariant());
            Assert.Contains("simple value", result.ToLowerInvariant());
        }

        [Fact]
        public void SafeSerialize_WithExceptionThrowingProperty_HandlesSafely()
        {
            // Arrange
            var problematicObject = new ProblematicTestClass();

            // Act
            var result = _service.SafeSerialize(problematicObject);

            // Assert
            Assert.NotNull(result);
            // Should not throw exception and should provide some representation
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void CreateAuditSafeRepresentation_WithNullEntity_ReturnsEmptyDictionary()
        {
            // Act
            var result = _service.CreateAuditSafeRepresentation(null!);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void SafeSerialize_WithPrimitiveTypes_ReturnsPrimitiveValues()
        {
            // Test various primitive types
            Assert.Equal("42", _service.SafeSerialize(42));
            Assert.Equal("true", _service.SafeSerialize(true));
            Assert.Equal("\"test string\"", _service.SafeSerialize("test string"));
        }

        [Fact]
        public void SafeSerialize_WithDateTime_HandlesCorrectly()
        {
            // Arrange
            var dateTime = new DateTime(2023, 12, 25, 10, 30, 0, DateTimeKind.Utc);

            // Act
            var result = _service.SafeSerialize(dateTime);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("2023", result);
        }

        /// <summary>
        /// Test class that throws exceptions when accessing certain properties
        /// </summary>
        private class ProblematicTestClass
        {
            public string SafeProperty => "Safe Value";
            
            public string ProblematicProperty => throw new InvalidOperationException("This property always throws");
            
            public int NumericProperty => 42;
        }
    }
}