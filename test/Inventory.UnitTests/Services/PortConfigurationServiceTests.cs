using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Inventory.API.Services;
using Xunit;
using FluentAssertions;

namespace Inventory.UnitTests.Services;

public class PortConfigurationServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<PortConfigurationService>> _mockLogger;
    private readonly PortConfigurationService _service;

    public PortConfigurationServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<PortConfigurationService>>();
        _service = new PortConfigurationService(_mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public void LoadPortConfiguration_WithValidPortsJson_ShouldReturnConfiguration()
    {
        // Arrange
        var portsJsonPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "ports.json");
        var portsJsonContent = """
        {
            "api": {
                "http": 5000,
                "https": 7000
            },
            "web": {
                "http": 5001,
                "https": 7001
            }
        }
        """;

        // Create temporary ports.json file
        var tempDir = Path.GetTempPath();
        var tempPortsPath = Path.Combine(tempDir, "ports.json");
        File.WriteAllText(tempPortsPath, portsJsonContent);

        try
        {
            // Create service with custom path
            var service = new PortConfigurationService(_mockConfiguration.Object, _mockLogger.Object);

            // Act
            var result = service.LoadPortConfiguration();

            // Assert
            result.Should().NotBeNull();
            result.ApiHttp.Should().Be(5000);
            result.ApiHttps.Should().Be(7000);
            result.WebHttp.Should().Be(5001);
            result.WebHttps.Should().Be(7001);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPortsPath))
            {
                File.Delete(tempPortsPath);
            }
        }
    }

    [Fact]
    public void LoadPortConfiguration_WithMissingPortsJson_ShouldReturnDefaultConfiguration()
    {
        // Arrange
        var service = new PortConfigurationService(_mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = service.LoadPortConfiguration();

        // Assert
        result.Should().NotBeNull();
        result.ApiHttp.Should().Be(5000);
        result.ApiHttps.Should().Be(7000);
        result.WebHttp.Should().Be(5001);
        result.WebHttps.Should().Be(7001);
    }

    [Fact]
    public void LoadPortConfiguration_WithInvalidJson_ShouldReturnDefaultConfiguration()
    {
        // Arrange
        var portsJsonPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "ports.json");
        var invalidJsonContent = "invalid json content";

        // Create temporary invalid ports.json file
        var tempDir = Path.GetTempPath();
        var tempPortsPath = Path.Combine(tempDir, "ports.json");
        File.WriteAllText(tempPortsPath, invalidJsonContent);

        try
        {
            // Create service with custom path
            var service = new PortConfigurationService(_mockConfiguration.Object, _mockLogger.Object);

            // Act
            var result = service.LoadPortConfiguration();

            // Assert
            result.Should().NotBeNull();
            result.ApiHttp.Should().Be(5000);
            result.ApiHttps.Should().Be(7000);
            result.WebHttp.Should().Be(5001);
            result.WebHttps.Should().Be(7001);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPortsPath))
            {
                File.Delete(tempPortsPath);
            }
        }
    }

    [Fact]
    public void GetCorsOrigins_WithDefaultConfiguration_ShouldReturnDefaultOrigins()
    {
        // Arrange
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(x => x.Value).Returns((string?)null);
        _mockConfiguration.Setup(x => x.GetSection("Cors:AdditionalOrigins"))
            .Returns(mockSection.Object);

        // Act
        var result = _service.GetCorsOrigins();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        result.Should().Contain("http://localhost:5000");
        result.Should().Contain("https://localhost:7000");
        result.Should().Contain("http://localhost:5001");
        result.Should().Contain("https://localhost:7001");
    }

    [Fact]
    public void GetCorsOrigins_WithAdditionalOrigins_ShouldIncludeAdditionalOrigins()
    {
        // Arrange
        var additionalOrigins = new[] { "https://example.com", "http://test.com" };
        
        // Создаем реальную конфигурацию с дополнительными origins
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AdditionalOrigins:0"] = "https://example.com",
                ["Cors:AdditionalOrigins:1"] = "http://test.com"
            })
            .Build();
        
        var service = new PortConfigurationService(configuration);

        // Act
        var result = service.GetCorsOrigins();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(6);
        result.Should().Contain("http://localhost:5000");
        result.Should().Contain("https://localhost:7000");
        result.Should().Contain("http://localhost:5001");
        result.Should().Contain("https://localhost:7001");
        result.Should().Contain("https://example.com");
        result.Should().Contain("http://test.com");
    }

    [Fact]
    public void PortConfiguration_Default_ShouldHaveCorrectDefaultValues()
    {
        // Act
        var config = PortConfiguration.Default;

        // Assert
        config.Should().NotBeNull();
        config.ApiHttp.Should().Be(5000);
        config.ApiHttps.Should().Be(7000);
        config.WebHttp.Should().Be(5001);
        config.WebHttps.Should().Be(7001);
    }
}
