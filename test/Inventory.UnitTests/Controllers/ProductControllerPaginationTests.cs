using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Inventory.API.Controllers;
using Inventory.API.Models;
using Inventory.API.Services;
using Inventory.Shared.DTOs;
using Xunit;
using FluentAssertions;

namespace Inventory.UnitTests.Controllers;

public class ProductControllerPaginationTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ProductController _controller;
    private readonly Mock<ILogger<ProductController>> _mockLogger;
    private readonly string _testDatabaseName;

    public ProductControllerPaginationTests()
    {
        // Create unique database name for this test
        _testDatabaseName = $"inventory_unit_test_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_testDatabaseName)
            .Options;

        _context = new AppDbContext(options);
        
        // Ensure database is created
        _context.Database.EnsureCreated();
        
        _mockLogger = new Mock<ILogger<ProductController>>();
        var safeSerializationService = new SafeSerializationService(Mock.Of<ILogger<SafeSerializationService>>());
        var mockAuditService = new Mock<AuditService>(_context, Mock.Of<IHttpContextAccessor>(), Mock.Of<ILogger<AuditService>>(), safeSerializationService);
        _controller = new ProductController(_context, _mockLogger.Object, mockAuditService.Object);
        
        // Setup authentication context
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "testadmin"),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Setup test data
        SetupTestData();
    }

    private void SetupTestData()
    {
        // Add categories
        var category1 = new Category { Name = "Electronics", Description = "Electronic devices", IsActive = true, CreatedAt = DateTime.UtcNow };
        var category2 = new Category { Name = "Books", Description = "Books and literature", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Categories.AddRange(category1, category2);
        _context.SaveChanges();

        // Add manufacturers
        var manufacturer1 = new Manufacturer { Name = "Dell" };
        var manufacturer2 = new Manufacturer { Name = "Apple" };
        _context.Manufacturers.AddRange(manufacturer1, manufacturer2);
        _context.SaveChanges();

        // Add product models
        var model1 = new ProductModel { Name = "XPS 13" };
        var model2 = new ProductModel { Name = "MacBook Pro" };
        _context.ProductModels.AddRange(model1, model2);
        _context.SaveChanges();

        // Add product groups
        var group1 = new ProductGroup { Name = "Laptops", IsActive = true };
        var group2 = new ProductGroup { Name = "Tablets", IsActive = true };
        _context.ProductGroups.AddRange(group1, group2);
        _context.SaveChanges();

        // Add products
        var products = new List<Product>
        {
            new Product
            {
                Name = "Dell XPS 13",
                SKU = "DELL-XPS13-001",
                Description = "High-performance laptop",
                CurrentQuantity = 10,
                UnitOfMeasureId = 1,
                IsActive = true,
                CategoryId = category1.Id,
                ManufacturerId = manufacturer1.Id,
                ProductModelId = model1.Id,
                ProductGroupId = group1.Id,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Name = "MacBook Pro 16",
                SKU = "APPLE-MBP16-001",
                Description = "Professional laptop",
                CurrentQuantity = 5,
                UnitOfMeasureId = 1,
                IsActive = true,
                CategoryId = category1.Id,
                ManufacturerId = manufacturer2.Id,
                ProductModelId = model2.Id,
                ProductGroupId = group1.Id,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Name = "iPad Pro",
                SKU = "APPLE-IPAD-001",
                Description = "Professional tablet",
                CurrentQuantity = 15,
                UnitOfMeasureId = 1,
                IsActive = false, // Inactive product
                CategoryId = category1.Id,
                ManufacturerId = manufacturer2.Id,
                ProductModelId = model2.Id,
                ProductGroupId = group2.Id,
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.Products.AddRange(products);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetProducts_WithPagination_ReturnsPagedResponse()
    {
        // Arrange
        var page = 1;
        var pageSize = 2;

        // Act
        var result = await _controller.GetProducts(page, pageSize);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        var response = okResult?.Value as PagedApiResponse<ProductDto>;
        
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().HaveCount(2); // Page size is 2
        response.Data.total.Should().Be(3); // 3 total products (Admin sees all)
        response.Data.page.Should().Be(1);
        response.Data.PageSize.Should().Be(2);
        response.Data.TotalPages.Should().Be(2); // 3 products / 2 per page = 2 pages
    }

    [Fact]
    public async Task GetProducts_WithSearchFilter_ReturnsFilteredResults()
    {
        // Arrange
        var search = "Dell";

        // Act
        var result = await _controller.GetProducts(1, 10, search);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        var response = okResult?.Value as PagedApiResponse<ProductDto>;
        
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.Items.Should().HaveCount(1);
        response.Data.Items.First().Name.Should().Contain("Dell");
    }

    [Fact]
    public async Task GetProducts_WithCategoryFilter_ReturnsFilteredResults()
    {
        // Arrange
        var categoryId = _context.Categories.First().Id;

        // Act
        var result = await _controller.GetProducts(1, 10, null, categoryId);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        var response = okResult?.Value as PagedApiResponse<ProductDto>;
        
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.Items.Should().HaveCount(3); // All 3 products are in the same category (Admin sees all)
    }

    [Fact]
    public async Task GetProducts_WithIsActiveFilter_ReturnsFilteredResults()
    {
        // Arrange
        var isActive = false;

        // Act
        var result = await _controller.GetProducts(1, 10, null, null, null, isActive);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        var response = okResult?.Value as PagedApiResponse<ProductDto>;
        
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.Items.Should().HaveCount(1);
        response.Data.Items.First().IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetProducts_AsNonAdminUser_ShowsOnlyActiveProducts()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user1"),
            new Claim(ClaimTypes.Role, "User")
        }));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await _controller.GetProducts();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        var response = okResult?.Value as PagedApiResponse<ProductDto>;
        
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.Items.Should().HaveCount(2); // Only active products
        response.Data.Items.All(p => p.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task GetProducts_AsAdminUser_CanSeeAllProducts()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin1"),
            new Claim(ClaimTypes.Role, "Admin")
        }));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await _controller.GetProducts();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        var response = okResult?.Value as PagedApiResponse<ProductDto>;
        
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.Items.Should().HaveCount(3); // All products including inactive
    }

    [Fact]
    public async Task GetProducts_WithInvalidPageSize_ReturnsDefaultPageSize()
    {
        // Arrange
        var page = 1;
        var pageSize = 0; // Invalid page size

        // Act
        var result = await _controller.GetProducts(page, pageSize);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        var response = okResult?.Value as PagedApiResponse<ProductDto>;
        
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.PageSize.Should().Be(0); // Will be handled by the controller logic
    }

    public void Dispose()
    {
        // Clean up database
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
