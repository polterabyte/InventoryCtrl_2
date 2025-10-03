using Inventory.Shared.Interfaces;
using Moq;
using Inventory.UI.Components.Admin;
using Inventory.Shared.DTOs;
using Xunit;
using FluentAssertions;
using Radzen;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Inventory.ComponentTests.Components.Admin
{
    public class CategoryManagementWidgetTests : TestContext
    {
        private readonly Mock<IClientCategoryService> _mockCategoryService;
        private readonly Mock<NotificationService> _mockUiNotificationService;
        private readonly Mock<IJSRuntime> _mockJsRuntime;
        private readonly Mock<DialogService> _mockDialogService;

        public CategoryManagementWidgetTests()
        {
            _mockCategoryService = new Mock<IClientCategoryService>();
            _mockUiNotificationService = new Mock<NotificationService>();
            _mockJsRuntime = new Mock<IJSRuntime>();
            _mockDialogService = new Mock<DialogService>();

            Services.AddSingleton(_mockCategoryService.Object);
            Services.AddSingleton(_mockUiNotificationService.Object);
            Services.AddSingleton(_mockJsRuntime.Object);
            Services.AddSingleton(_mockDialogService.Object);
            Services.AddScoped(sp => new HttpClient());

            // Mock the NotificationService to prevent NullReferenceException
            Services.AddSingleton<NotificationService>(new NotificationService());
        }

        [Fact]
        public void RendersCorrectly_And_LoadsDataOnInitialization()
        {
            // Arrange
            var categories = new List<CategoryDto>
            {
                new() { Id = 1, Name = "Root 1", IsActive = true, CreatedAt = System.DateTime.Now },
                new() { Id = 2, Name = "Child 1", ParentCategoryId = 1, IsActive = true, CreatedAt = System.DateTime.Now }
            };
            _mockCategoryService.Setup(s => s.GetAllCategoriesAsync()).ReturnsAsync(categories);
            _mockCategoryService.Setup(s => s.GetRootCategoriesAsync()).ReturnsAsync(new List<CategoryDto> { categories[0] });

            // Act
            var cut = RenderComponent<CategoryManagementWidget>();

            // Assert
            cut.WaitForAssertion(() => cut.FindAll("tbody tr").Count.Should().Be(2));
            cut.Find("h5").TextContent.Should().Be("Categories Management");
            cut.Markup.Should().Contain("Root 1");
            cut.Markup.Should().Contain("Child 1");
        }

        [Fact]
        public async Task AddButton_OpensCreateDialog()
        {
            // Arrange
            _mockCategoryService.Setup(s => s.GetRootCategoriesAsync()).ReturnsAsync(new List<CategoryDto>());
            var cut = RenderComponent<CategoryManagementWidget>();

            // Act
            cut.Find("button.rz-button.rz-button-primary").Click();
            await Task.Delay(100); // Allow dialog to open

            // Assert
            _mockDialogService.Verify(d => d.OpenAsync(
                "Create Category",
                It.IsAny<RenderFragment<DialogService>>(),
                It.Is<DialogOptions>(o => o.Width == "500px")),
                Times.Once);
        }

        [Fact]
        public async Task RefreshButton_ReloadsData()
        {
            // Arrange
            var component = RenderComponent<CategoryManagementWidget>();
            _mockCategoryService.Invocations.Clear(); // Clear initial load invocation

            // Act
            var refreshButton = component.Find("button.btn-secondary");
            await component.InvokeAsync(() => refreshButton.Click());

            // Assert
            _mockCategoryService.Verify(s => s.GetAllCategoriesAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteButton_ShowsConfirmation_AndDeletesItem()
        {
            // Arrange
            var categoryToDelete = new CategoryDto { Id = 1, Name = "ToDelete", IsActive = true, CreatedAt = System.DateTime.Now };
            _mockCategoryService.Setup(s => s.GetAllCategoriesAsync()).ReturnsAsync(new List<CategoryDto> { categoryToDelete });
            _mockCategoryService.Setup(s => s.GetRootCategoriesAsync()).ReturnsAsync(new List<CategoryDto>());
            _mockJsRuntime.Setup(js => js.InvokeAsync<bool>("confirm", It.IsAny<object[]>())).ReturnsAsync(true);
            _mockCategoryService.Setup(s => s.DeleteCategoryAsync(categoryToDelete.Id)).ReturnsAsync(true);

            var cut = RenderComponent<CategoryManagementWidget>();
            cut.WaitForState(() => cut.FindAll("tbody tr").Count > 0);

            // Act
            cut.Find("button.rz-button-danger").Click();
            await Task.Delay(100); // allow confirm dialog and service call

            // Assert
            _mockJsRuntime.Verify(js => js.InvokeAsync<bool>("confirm", It.Is<object[]>(o => o[0].ToString()!.Contains("ToDelete"))), Times.Once);
            _mockCategoryService.Verify(s => s.DeleteCategoryAsync(categoryToDelete.Id), Times.Once);
            _mockUiNotificationService.Verify(n => n.Notify(It.Is<NotificationMessage>(m => m.Severity == NotificationSeverity.Success)), Times.Once);
        }
    }
}
