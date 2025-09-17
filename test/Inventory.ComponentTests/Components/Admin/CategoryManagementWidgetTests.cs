using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using Inventory.UI.Components.Admin;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Inventory.Shared.Services;
using Xunit;
using FluentAssertions;

namespace Inventory.ComponentTests.Components.Admin;

public class CategoryManagementWidgetTests : ComponentTestBase
{
    [Fact]
    public void CategoryManagementWidget_RendersCorrectly()
    {
        // Arrange
        var mockCategoryService = new Mock<ICategoryService>();
        var mockNotificationService = new Mock<INotificationService>();
        var mockJSRuntime = new Mock<IJSRuntime>();

        Services.AddSingleton(mockCategoryService.Object);
        Services.AddSingleton(mockNotificationService.Object);
        Services.AddSingleton(mockJSRuntime.Object);

        // Act
        var component = RenderComponent<CategoryManagementWidget>();

        // Assert
        component.Should().NotBeNull();
        component.Markup.Should().Contain("Categories Management");
        component.Markup.Should().Contain("Add New Category");
    }

    [Fact]
    public void CategoryManagementWidget_ShowsCreateModal_WhenAddButtonClicked()
    {
        // Arrange
        var mockCategoryService = new Mock<ICategoryService>();
        var mockNotificationService = new Mock<INotificationService>();
        var mockJSRuntime = new Mock<IJSRuntime>();

        Services.AddSingleton(mockCategoryService.Object);
        Services.AddSingleton(mockNotificationService.Object);
        Services.AddSingleton(mockJSRuntime.Object);

        var component = RenderComponent<CategoryManagementWidget>();

        // Act
        var addButton = component.Find("button:contains('Add New Category')");
        addButton.Click();

        // Assert
        component.Markup.Should().Contain("Create Category");
        component.Markup.Should().Contain("modal");
    }

    [Fact]
    public void CategoryManagementWidget_ShowsEditModal_WhenEditButtonClicked()
    {
        // Arrange
        var mockCategoryService = new Mock<ICategoryService>();
        var mockNotificationService = new Mock<INotificationService>();
        var mockJSRuntime = new Mock<IJSRuntime>();

        var categories = new List<CategoryDto>
        {
            new() { Id = 1, Name = "Test Category", Description = "Test Description", IsActive = true }
        };

        mockCategoryService.Setup(s => s.GetAllCategoriesAsync())
                          .ReturnsAsync(categories);

        Services.AddSingleton(mockCategoryService.Object);
        Services.AddSingleton(mockNotificationService.Object);
        Services.AddSingleton(mockJSRuntime.Object);

        var component = RenderComponent<CategoryManagementWidget>();

        // Act
        var editButton = component.Find("button[title='Edit']");
        editButton.Click();

        // Assert
        component.Markup.Should().Contain("Edit Category");
        component.Markup.Should().Contain("modal");
    }

    [Fact]
    public void CategoryManagementWidget_ShowsDeleteConfirmation_WhenDeleteButtonClicked()
    {
        // Arrange
        var mockCategoryService = new Mock<ICategoryService>();
        var mockNotificationService = new Mock<INotificationService>();
        var mockJSRuntime = new Mock<IJSRuntime>();

        var categories = new List<CategoryDto>
        {
            new() { Id = 1, Name = "Test Category", Description = "Test Description", IsActive = true }
        };

        mockCategoryService.Setup(s => s.GetAllCategoriesAsync())
                          .ReturnsAsync(categories);

        mockJSRuntime.Setup(js => js.InvokeAsync<bool>("confirm", It.IsAny<string>()))
                    .ReturnsAsync(true);

        Services.AddSingleton(mockCategoryService.Object);
        Services.AddSingleton(mockNotificationService.Object);
        Services.AddSingleton(mockJSRuntime.Object);

        var component = RenderComponent<CategoryManagementWidget>();

        // Act
        var deleteButton = component.Find("button[title='Delete']");
        deleteButton.Click();

        // Assert
        mockJSRuntime.Verify(js => js.InvokeAsync<bool>("confirm", "Are you sure you want to delete 'Test Category'?"), Times.Once);
    }

    [Fact]
    public void CategoryManagementWidget_DisplaysCategories_WhenDataLoaded()
    {
        // Arrange
        var mockCategoryService = new Mock<ICategoryService>();
        var mockNotificationService = new Mock<INotificationService>();
        var mockJSRuntime = new Mock<IJSRuntime>();

        var categories = new List<CategoryDto>
        {
            new() { Id = 1, Name = "Test Category 1", Description = "Test Description 1", IsActive = true },
            new() { Id = 2, Name = "Test Category 2", Description = "Test Description 2", IsActive = false }
        };

        mockCategoryService.Setup(s => s.GetAllCategoriesAsync())
                          .ReturnsAsync(categories);

        Services.AddSingleton(mockCategoryService.Object);
        Services.AddSingleton(mockNotificationService.Object);
        Services.AddSingleton(mockJSRuntime.Object);

        // Act
        var component = RenderComponent<CategoryManagementWidget>();

        // Assert
        component.Markup.Should().Contain("Test Category 1");
        component.Markup.Should().Contain("Test Category 2");
        component.Markup.Should().Contain("Active");
        component.Markup.Should().Contain("Inactive");
    }

    [Fact]
    public void CategoryManagementWidget_ShowsLoadingState_WhenDataIsLoading()
    {
        // Arrange
        var mockCategoryService = new Mock<ICategoryService>();
        var mockNotificationService = new Mock<INotificationService>();
        var mockJSRuntime = new Mock<IJSRuntime>();

        // Setup a task that will never complete to simulate loading
        var tcs = new TaskCompletionSource<List<CategoryDto>>();
        mockCategoryService.Setup(s => s.GetAllCategoriesAsync())
                          .Returns(tcs.Task);

        Services.AddSingleton(mockCategoryService.Object);
        Services.AddSingleton(mockNotificationService.Object);
        Services.AddSingleton(mockJSRuntime.Object);

        // Act
        var component = RenderComponent<CategoryManagementWidget>();

        // Assert
        component.Markup.Should().Contain("Loading...");
        component.Markup.Should().Contain("spinner-border");
    }

    [Fact]
    public void CategoryManagementWidget_ShowsNoDataMessage_WhenNoCategories()
    {
        // Arrange
        var mockCategoryService = new Mock<ICategoryService>();
        var mockNotificationService = new Mock<INotificationService>();
        var mockJSRuntime = new Mock<IJSRuntime>();

        var categories = new List<CategoryDto>();

        mockCategoryService.Setup(s => s.GetAllCategoriesAsync())
                          .ReturnsAsync(categories);

        Services.AddSingleton(mockCategoryService.Object);
        Services.AddSingleton(mockNotificationService.Object);
        Services.AddSingleton(mockJSRuntime.Object);

        // Act
        var component = RenderComponent<CategoryManagementWidget>();

        // Assert
        component.Markup.Should().Contain("No categories found");
    }
}
