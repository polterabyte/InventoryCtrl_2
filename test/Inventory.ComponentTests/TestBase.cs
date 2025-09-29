using Bunit;
using Inventory.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Radzen;

namespace Inventory.ComponentTests;

/// <summary>
/// Base class for component tests with common setup
/// </summary>
public abstract class ComponentTestBase : TestContext
{
    protected ComponentTestBase()
    {
        // Register common services
        Services.AddSingleton(Mock.Of<ILogger<ErrorHandlingService>>());
        Services.AddSingleton(Mock.Of<NotificationService>());
        // Services.AddSingleton(Mock.Of<INotificationService>()); // Commented out - interface not found
    }

    /// <summary>
    /// Helper method to add singleton service to test context
    /// </summary>
    protected void AddSingletonService<TService>(TService service) where TService : class
    {
        Services.AddSingleton(service);
    }
}
