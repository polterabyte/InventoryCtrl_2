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
                // Prefer CORS origins from environment variable CORS_ALLOWED_ORIGINS (comma-separated)
                var originsEnv = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
                string[] origins = Array.Empty<string>();
                if (!string.IsNullOrWhiteSpace(originsEnv))
                {
                    origins = originsEnv
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToArray();
                }
                
                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
                else
                {
                    // Safe default: allow localhost origins commonly used in dev, no external domains
                    policy.WithOrigins(
                              "http://localhost",
                              "https://localhost",
                              "http://localhost:5000",
                              "https://localhost:5001",
                              "http://localhost:7000",
                              "https://localhost:7001",
                              "http://10.0.2.2:8080",
                              "capacitor://localhost"
                          )
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
            });
        });
        return services;
    }
    
    public static IServiceCollection AddAuditServices(this IServiceCollection services)
    {
        services.AddScoped<IInternalAuditService, AuditService>();
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
