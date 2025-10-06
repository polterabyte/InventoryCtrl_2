using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Linq;
using Xunit;
using Inventory.API.Models;
using Inventory.API.Controllers;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;

namespace Inventory.IntegrationTests.RequestCreation
{
    /// <summary>
    /// Integration tests for the request creation flow to verify that circular reference issues are resolved
    /// </summary>
    public class RequestCreationFlowTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public RequestCreationFlowTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateRequest_WithValidData_ShouldSucceedWithoutCircularReferenceError()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Ensure database is created and seeded
            await context.Database.EnsureCreatedAsync();
            
            // Create a test user if not exists
            var testUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "testuser");
            if (testUser == null)
            {
                testUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "testuser",
                    Email = "test@example.com",
                    FirstName = "Test",
                    LastName = "User"
                };
                context.Users.Add(testUser);
                await context.SaveChangesAsync();
            }

            // Get a JWT token for authentication (simplified for test)
            var authToken = GenerateTestJwtToken(testUser.Id, testUser.UserName);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            var (product, warehouse) = await EnsureTestProductAndWarehouse(context);

            var requestBody = new CreateRequestDto
            {
                Title = "Test Integration Request",
                Description = "Testing circular reference fix",
                Items = new List<RequestItemInputDto>
                {
                    new()
                    {
                        ProductId = product.Id,
                        WarehouseId = warehouse.Id,
                        Quantity = 1
                    }
                }
            };
            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/requests", content);

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Request failed with status: {response.StatusCode}. Content: {await response.Content.ReadAsStringAsync()}");

            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(responseContent));

            // Verify the response can be deserialized (no circular reference issues)
            var request = JsonSerializer.Deserialize<RequestDetailsDto>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(request);
            Assert.Equal("Test Integration Request", request.Title);
            Assert.Equal("Testing circular reference fix", request.Description);
            Assert.Equal(RequestStatus.Draft.ToString(), request.Status);
        }

        [Fact]
        public async Task CreateRequest_ShouldNotCauseAuditSerializationErrors()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            await context.Database.EnsureCreatedAsync();
            
            var testUser = await EnsureTestUserExists(context);
            var authToken = GenerateTestJwtToken(testUser.Id, testUser.UserName);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            var (product, warehouse) = await EnsureTestProductAndWarehouse(context);

            var requestBody = new CreateRequestDto
            {
                Title = "Audit Test Request",
                Description = "Testing audit logging without circular references",
                Items = new List<RequestItemInputDto>
                {
                    new()
                    {
                        ProductId = product.Id,
                        WarehouseId = warehouse.Id,
                        Quantity = 1
                    }
                }
            };
            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/requests", content);

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Request failed with status: {response.StatusCode}. Content: {await response.Content.ReadAsStringAsync()}");
            
            // Verify that audit logs were created without errors
            var auditLogs = await context.AuditLogs
                .Where(a => a.Action.Contains("REQUEST") || a.Action.Contains("CREATE"))
                .OrderByDescending(a => a.Timestamp)
                .Take(5)
                .ToListAsync();
            
            Assert.NotEmpty(auditLogs);
            
            // Verify that audit logs don't contain serialization error indicators
            foreach (var log in auditLogs)
            {
                Assert.DoesNotContain("_serializationError", log.Changes ?? "");
                Assert.DoesNotContain("circular reference", log.ErrorMessage ?? "");
                Assert.DoesNotContain("JsonException", log.ErrorMessage ?? "");
            }
        }

        [Fact]
        public async Task CreateRequestAndAddItems_ShouldHandleComplexObjectGraphs()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            await context.Database.EnsureCreatedAsync();
            
            var testUser = await EnsureTestUserExists(context);
            var (product, warehouse) = await EnsureTestProductAndWarehouse(context);
            
            var authToken = GenerateTestJwtToken(testUser.Id, testUser.UserName);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            // Create request
            var requestBody = new CreateRequestDto
            {
                Title = "Complex Graph Test",
                Description = "Testing complex object relationships",
                Items = new List<RequestItemInputDto>
                {
                    new()
                    {
                        ProductId = product.Id,
                        WarehouseId = warehouse.Id,
                        Quantity = 2
                    }
                }
            };
            var requestJson = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var createResponse = await _client.PostAsync("/api/requests", requestContent);
            Assert.True(createResponse.IsSuccessStatusCode);

            var createdRequest = JsonSerializer.Deserialize<RequestDetailsDto>(await createResponse.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(createdRequest);

            // Add item to request
            var addItemBody = new AddRequestItemDto
            {
                ProductId = product.Id,
                WarehouseId = warehouse.Id,
                Quantity = 5,
                UnitPrice = 10.50m,
                Description = "Test item"
            };
            var itemJson = JsonSerializer.Serialize(addItemBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var itemContent = new StringContent(itemJson, Encoding.UTF8, "application/json");

            // Act
            var addItemResponse = await _client.PostAsync($"/api/requests/{createdRequest.Id}/items", itemContent);

            // Assert
            Assert.True(addItemResponse.IsSuccessStatusCode, $"Add item failed with status: {addItemResponse.StatusCode}. Content: {await addItemResponse.Content.ReadAsStringAsync()}");

            // Verify the updated request is returned without circular reference issues
            var updatedRequest = JsonSerializer.Deserialize<RequestDetailsDto>(await addItemResponse.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(updatedRequest);
            var addedItem = updatedRequest!.Items.FirstOrDefault(i => i.ProductId == product.Id && i.WarehouseId == warehouse.Id && i.Quantity == 5);
            Assert.NotNull(addedItem);
        }

        private async Task<User> EnsureTestUserExists(AppDbContext context)
        {
            var testUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "testuser");
            if (testUser == null)
            {
                testUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "testuser",
                    Email = "test@example.com",
                    FirstName = "Test",
                    LastName = "User"
                };
                context.Users.Add(testUser);
                await context.SaveChangesAsync();
            }
            return testUser;
        }

        private async Task<(Product product, Warehouse warehouse)> EnsureTestProductAndWarehouse(AppDbContext context)
        {
            var category = await context.Categories.FirstOrDefaultAsync() ?? new Category { Name = "Test Category" };
            if (category.Id == 0)
            {
                context.Categories.Add(category);
                await context.SaveChangesAsync();
            }

            var manufacturer = await context.Manufacturers.FirstOrDefaultAsync() ?? new Manufacturer { Name = "Test Manufacturer" };
            if (manufacturer.Id == 0)
            {
                context.Manufacturers.Add(manufacturer);
                await context.SaveChangesAsync();
            }

            var uom = await context.UnitOfMeasures.FirstOrDefaultAsync() ?? new UnitOfMeasure { Name = "Piece", Symbol = "pcs" };
            if (uom.Id == 0)
            {
                context.UnitOfMeasures.Add(uom);
                await context.SaveChangesAsync();
            }

            var product = await context.Products.FirstOrDefaultAsync() ?? new Product
            {
                Name = "Test Product",
                CategoryId = category.Id,
                ManufacturerId = manufacturer.Id,
                UnitOfMeasureId = uom.Id,
                SKU = "TEST-001"
            };
            if (product.Id == 0)
            {
                context.Products.Add(product);
                await context.SaveChangesAsync();
            }

            var warehouse = await context.Warehouses.FirstOrDefaultAsync() ?? new Warehouse { Name = "Test Warehouse" };
            if (warehouse.Id == 0)
            {
                context.Warehouses.Add(warehouse);
                await context.SaveChangesAsync();
            }

            return (product, warehouse);
        }

        private string GenerateTestJwtToken(string userId, string userName)
        {
            // For testing purposes, return a simple token structure
            // In a real scenario, you would generate a proper JWT token
            var tokenPayload = new
            {
                sub = userId,
                name = userName,
                exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            };
            
            // This is a simplified token generation for testing
            // In production, use proper JWT generation
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(tokenPayload)));
        }
    }
}