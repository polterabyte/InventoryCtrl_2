using Inventory.API.Services;
using Inventory.Shared.Interfaces;

namespace Inventory.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPortConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<IPortConfigurationService, PortConfigurationService>();
        return services;
    }
    
    public static IServiceCollection AddCorsWithPorts(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowConfiguredOrigins", policy =>
            {
                // CORS будет настроен в ConfigureCorsWithPorts с использованием портов из ports.json
                policy.AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials(); // Required for Blazor WASM authentication
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
}

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureCorsWithPorts(this WebApplication app)
    {
        var portService = app.Services.GetRequiredService<IPortConfigurationService>();
        var corsOrigins = portService.GetCorsOrigins();
        
        app.UseCors(builder =>
        {
            builder.WithOrigins(corsOrigins)
                   .AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials(); // Required for Blazor WASM authentication
        });
        
        return app;
    }
}
