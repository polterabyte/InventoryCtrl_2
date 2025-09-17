using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Inventory.API.Models;

namespace Inventory.UnitTests;

public abstract class TestBase : IDisposable
{
    protected AppDbContext Context { get; private set; }
    private readonly string _testDatabaseName;

    protected TestBase()
    {
        // Create unique database name for this test
        _testDatabaseName = $"inventory_unit_test_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_testDatabaseName)
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    protected Mock<ILogger<T>> CreateMockLogger<T>() where T : class
    {
        return new Mock<ILogger<T>>();
    }

    protected void VerifyLogLevel<T>(Mock<ILogger<T>> loggerMock, LogLevel level, Times times)
        where T : class
    {
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    protected void VerifyLogMessage<T>(Mock<ILogger<T>> loggerMock, string expectedMessage, Times times)
        where T : class
    {
        loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    public void Dispose()
    {
        Context?.Dispose();
    }
}
