using Microsoft.Extensions.Configuration;

namespace Inventory.Web.Client.Services;

public class PortConfigurationService(ILogger<PortConfigurationService> logger, IConfiguration configuration)
{
    private readonly ILogger<PortConfigurationService> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    
    public string GetApiUrl()
    {
        try
        {
            var apiUrl = _configuration["ApiSettings:BaseUrl"];
            if (!string.IsNullOrEmpty(apiUrl))
            {
                return apiUrl;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read API URL from configuration, using default");
        }
        
        // Fallback to default
        return "https://localhost:7000";
    }
}
