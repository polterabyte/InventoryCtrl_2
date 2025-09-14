using Bunit;
using Inventory.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Inventory.ComponentTests;

/// <summary>
/// Base class for component tests with common setup
/// </summary>
public abstract class ComponentTestBase : TestContext
{
    protected ComponentTestBase()
    {
        // Register common services
        Services.AddSingleton(Mock.Of<ILogger<NotificationService>>());
        Services.AddSingleton(Mock.Of<INotificationService>());
    }

    /// <summary>
    /// Helper method to add singleton service to test context
    /// </summary>
    protected void AddSingletonService<TService>(TService service) where TService : class
    {
        Services.AddSingleton(service);
    }
}
