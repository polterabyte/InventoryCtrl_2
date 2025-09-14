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
}
