using System.Text.Json;
using System.Collections.Generic;

namespace Inventory.API.Services;

public class PortConfigurationService(IConfiguration configuration, ILogger<PortConfigurationService> logger) : IPortConfigurationService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<PortConfigurationService> _logger = logger;
    
    public PortConfigurationService(IConfiguration configuration) : this(configuration, new Logger<PortConfigurationService>(new LoggerFactory()))
    {
    }
    
    public PortConfiguration LoadPortConfiguration()
    {
        var portsConfigPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "ports.json");
        
        if (!File.Exists(portsConfigPath))
        {
            _logger.LogWarning("ports.json not found, using default configuration");
            return PortConfiguration.Default;
        }
        
        try
        {
            var portsConfigJson = File.ReadAllText(portsConfigPath);
            var portsConfig = JsonSerializer.Deserialize<JsonElement>(portsConfigJson);
            
            var apiHttp = portsConfig.GetProperty("api").GetProperty("http").GetInt32();
            var apiHttps = portsConfig.GetProperty("api").GetProperty("https").GetInt32();
            var webHttp = portsConfig.GetProperty("web").GetProperty("http").GetInt32();
            var webHttps = portsConfig.GetProperty("web").GetProperty("https").GetInt32();
            
            return new PortConfiguration
            {
                ApiHttp = apiHttp,
                ApiHttps = apiHttps,
                WebHttp = webHttp,
                WebHttps = webHttps
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load port configuration, using defaults");
            return PortConfiguration.Default;
        }
    }
    
    public string[] GetCorsOrigins()
    {
        var config = LoadPortConfiguration();
        var origins = new List<string>
        {
            $"http://localhost:{config.ApiHttp}",
            $"https://localhost:{config.ApiHttps}",
            $"http://localhost:{config.WebHttp}",
            $"https://localhost:{config.WebHttps}"
        };
        
        // Add additional origins from configuration
        var additionalOrigins = _configuration.GetSection("Cors:AdditionalOrigins").Get<string[]>();
        if (additionalOrigins != null && additionalOrigins.Length > 0)
        {
            origins.AddRange(additionalOrigins);
        }
        
        return origins.ToArray();
    }
}

public class PortConfiguration
{
    public int ApiHttp { get; set; } = 5000;
    public int ApiHttps { get; set; } = 7000;
    public int WebHttp { get; set; } = 5001;
    public int WebHttps { get; set; } = 7001;
    
    public static PortConfiguration Default => new();
}
