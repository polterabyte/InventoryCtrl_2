using Microsoft.Extensions.Logging;
using Moq;

namespace Inventory.UnitTests;

public abstract class TestBase
{
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
}
