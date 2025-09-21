using Inventory.API.Services;
using Inventory.Shared.Interfaces;

namespace Inventory.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowConfiguredOrigins", policy =>
            {
                // Get server IP from environment or use default
                var serverIp = Environment.GetEnvironmentVariable("SERVER_IP") ?? "192.168.139.96";
                
                policy.WithOrigins(
                        // IP address origins
                        $"https://{serverIp}",
                        $"http://{serverIp}",
                        // Localhost origins
                        "https://localhost:5001",  // Blazor WebAssembly client
                        "https://localhost:7001",  // Alternative client port
                        "http://localhost:5001",   // HTTP fallback
                        "http://localhost:7001",   // HTTP fallback
                        // Additional mobile/development origins
                        "http://10.0.2.2:8080",
                        "capacitor://localhost"
                      )
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials(); // Required for Blazor WASM authentication with JWT
            });
        });
        return services;
    }
    
    public static IServiceCollection AddAuditServices(this IServiceCollection services)
    {
        services.AddScoped<AuditService>();
        services.AddHttpContextAccessor();
        return services;
    }
    
    public static IServiceCollection AddNotificationServices(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationRuleEngine, NotificationRuleEngine>();
        return services;
    }
    
    public static IServiceCollection AddSSLServices(this IServiceCollection services)
    {
        services.AddScoped<ISSLCertificateService, SSLCertificateService>();
        return services;
    }
}

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureCors(this WebApplication app)
    {
        app.UseCors("AllowConfiguredOrigins");
        
        return app;
    }
}
