using Inventory.API.Services;

namespace Inventory.API.Configuration;

public class ApplicationConfigurationBuilder
{
    private readonly WebApplicationBuilder _builder;
    private PortConfiguration? _portConfig;
    
    public ApplicationConfigurationBuilder(WebApplicationBuilder builder)
    {
        _builder = builder;
    }
    
    public ApplicationConfigurationBuilder LoadPortConfiguration()
    {
        var portService = new PortConfigurationService(_builder.Configuration, 
            _builder.Services.BuildServiceProvider().GetRequiredService<ILogger<PortConfigurationService>>());
        _portConfig = portService.LoadPortConfiguration();
        
        // Store in configuration for other services
        _builder.Configuration["Ports:Api:Http"] = _portConfig.ApiHttp.ToString();
        _builder.Configuration["Ports:Api:Https"] = _portConfig.ApiHttps.ToString();
        _builder.Configuration["Ports:Web:Http"] = _portConfig.WebHttp.ToString();
        _builder.Configuration["Ports:Web:Https"] = _portConfig.WebHttps.ToString();
        
        return this;
    }
    
    public ApplicationConfigurationBuilder ConfigureCors()
    {
        if (_portConfig == null)
            throw new InvalidOperationException("Port configuration must be loaded first");
            
        var corsOrigins = new[]
        {
            $"http://localhost:{_portConfig.ApiHttp}",
            $"https://localhost:{_portConfig.ApiHttps}",
            $"http://localhost:{_portConfig.WebHttp}",
            $"https://localhost:{_portConfig.WebHttps}",
            "http://10.0.2.2:8080",
            "capacitor://localhost",
            "https://yourmobileapp.com"
        };
        
        _builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowConfiguredOrigins", policy =>
            {
                policy.WithOrigins(corsOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });
        
        return this;
    }
    
    public WebApplicationBuilder Build() => _builder;
}
