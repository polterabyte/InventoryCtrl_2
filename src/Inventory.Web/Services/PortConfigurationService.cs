using System.Text.Json;

namespace Inventory.Web.Services;

public class PortConfigurationService
{
    private readonly ILogger<PortConfigurationService> _logger;
    
    public PortConfigurationService(ILogger<PortConfigurationService> logger)
    {
        _logger = logger;
    }
    
    public string GetApiUrl()
    {
        var portsConfigPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "ports.json");
        
        if (!File.Exists(portsConfigPath))
        {
            _logger.LogWarning("ports.json not found, using default API URL");
            return "https://localhost:7000";
        }
        
        try
        {
            var portsConfigJson = File.ReadAllText(portsConfigPath);
            var portsConfig = JsonSerializer.Deserialize<JsonElement>(portsConfigJson);
            var apiHttpsPort = portsConfig.GetProperty("api").GetProperty("https").GetInt32();
            return $"https://localhost:{apiHttpsPort}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load port configuration, using default API URL");
            return "https://localhost:7000";
        }
    }
}
