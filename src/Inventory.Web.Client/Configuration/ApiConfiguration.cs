namespace Inventory.Web.Client.Configuration;

public class ApiConfiguration
{
    public const string SectionName = "ApiSettings";
    
    public string BaseUrl { get; set; } = "/api";
    public Dictionary<string, EnvironmentConfig> Environments { get; set; } = new();
    public FallbackConfig Fallback { get; set; } = new();
}

public class EnvironmentConfig
{
    public string ApiUrl { get; set; } = string.Empty;
    public string SignalRUrl { get; set; } = string.Empty;
    public string HealthCheckUrl { get; set; } = string.Empty;
}

public class FallbackConfig
{
    public bool UseDynamicDetection { get; set; } = true;
    public PortConfig DefaultPorts { get; set; } = new();
    public RetryConfig RetrySettings { get; set; } = new();
}

public class PortConfig
{
    public int Http { get; set; } = 5000;
    public int Https { get; set; } = 7000;
}

public class RetryConfig
{
    public int MaxRetries { get; set; } = 3;
    public int BaseDelayMs { get; set; } = 1000;
    public int MaxDelayMs { get; set; } = 10000;
}
