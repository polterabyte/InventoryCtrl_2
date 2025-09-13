using Microsoft.Extensions.Configuration;

namespace Inventory.Web.Client.Services;

public class PortConfigurationService
{
    private readonly ILogger<PortConfigurationService> _logger;
    private readonly IConfiguration _configuration;
    
    public PortConfigurationService(ILogger<PortConfigurationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }
    
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
        return "http://localhost:5000";
    }
}
