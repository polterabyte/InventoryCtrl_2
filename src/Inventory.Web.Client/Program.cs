using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Inventory.Web.Client;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using Blazored.LocalStorage;
using Inventory.Shared.Interfaces;
using Inventory.Shared.Services;
using Microsoft.Extensions.Logging;
using Inventory.Web.Client.Services;
using Microsoft.AspNetCore.Components;
using Inventory.Web.Client.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Localization;
using System.Globalization;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API settings
builder.Services.Configure<ApiConfiguration>(
    builder.Configuration.GetSection(ApiConfiguration.SectionName));

// Configure Token settings
builder.Services.Configure<TokenConfiguration>(
    builder.Configuration.GetSection(TokenConfiguration.SectionName));

// Register API URL service
builder.Services.AddScoped<Inventory.Web.Client.Services.IApiUrlService, Inventory.Web.Client.Services.ApiUrlService>();

// Register URL builder service
builder.Services.AddScoped<IUrlBuilderService, UrlBuilderService>();

// Register API error handler
builder.Services.AddScoped<IApiErrorHandler, ApiErrorHandler>();

// Register request validator
builder.Services.AddScoped<IRequestValidator, RequestValidator>();

// Register validation service
builder.Services.AddScoped<ValidationService>();

// Register health check service
builder.Services.AddScoped<IApiHealthService, ApiHealthService>();

// Register resilient API service
builder.Services.AddScoped<IResilientApiService, ResilientApiService>();

// Register token refresh service (without circular dependencies)
builder.Services.AddScoped<ITokenRefreshService>(sp =>
{
    // Separate HttpClient to avoid interceptor recursion; no fixed BaseAddress
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

    var logger = sp.GetRequiredService<ILogger<TokenRefreshService>>();
    var config = sp.GetRequiredService<IOptions<TokenConfiguration>>();
    var urlBuilder = sp.GetRequiredService<IUrlBuilderService>();

    return new TokenRefreshService(httpClient, logger, config, urlBuilder);
});

// Register token management service
builder.Services.AddScoped<ITokenManagementService, TokenManagementService>();

// Register HTTP interceptor as a delegating handler
builder.Services.AddScoped<JwtHttpInterceptor>();

// Configure API HTTP client with interceptor support
builder.Services.AddHttpClient("API", client =>
    {
        // No BaseAddress here, it will be set dynamically by ApiUrlService
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
    })
    .AddHttpMessageHandler<JwtHttpInterceptor>();

// Register HttpClient for API services - uses the "API" client with JWT interceptor
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("API");
});


// Add Blazor authorization services
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<ICustomAuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Configure logging
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
//builder.Logging.AddConsole(); -- not work in browser

// Register Web-specific services
builder.Services.AddScoped<IAuthService, WebAuthApiService>();
builder.Services.AddScoped<IProductService, WebProductApiService>();
builder.Services.AddScoped<IUnitOfMeasureApiService, WebUnitOfMeasureApiService>();
builder.Services.AddScoped<IClientCategoryService, WebCategoryApiService>();
builder.Services.AddScoped<IManufacturerService, WebManufacturerApiService>();
builder.Services.AddScoped<IProductGroupService, WebProductGroupApiService>();
builder.Services.AddScoped<IProductModelService, WebProductModelApiService>();
builder.Services.AddScoped<IWarehouseService, WebWarehouseApiService>();
builder.Services.AddScoped<ILocationService, WebLocationApiService>();
builder.Services.AddScoped<IDashboardService, WebDashboardApiService>();

// Register logging and error handling services
builder.Services.AddScoped<ILoggingService, LoggingService>();
builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();

// Register notification and retry services
// Remove this line - using Radzen notifications directly now
// builder.Services.AddScoped<IUINotificationService, Inventory.Shared.Services.NotificationService>();
builder.Services.AddScoped<IRetryService, RetryService>();
builder.Services.AddScoped<IDebugLogsService, DebugLogsService>();

// Register authentication service
builder.Services.AddScoped<Inventory.UI.Services.IAuthenticationService, Inventory.UI.Services.AuthenticationService>();

// Register user management service
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IUserWarehouseService, WebUserWarehouseApiService>();

// Register audit services
builder.Services.AddScoped<IAuditService, WebAuditApiService>();

// Register request services
builder.Services.AddScoped<IRequestApiService, WebRequestApiService>();

// Initialize validators
builder.Services.AddScoped(provider =>
{
    var validationService = provider.GetRequiredService<ValidationService>();
    validationService.RegisterAllValidators();
    return validationService;
});

// Register SignalR service (C# client)
builder.Services.AddScoped<ISignalRService, SignalRService>();

// Register Radzen services
builder.Services.AddRadzenComponents();

// Configure Localization
builder.Services.AddLocalization();

// Configure supported cultures (for WebAssembly client)
var supportedCultures = new[] { "en-US", "ru-RU" };

// Register Theme service
builder.Services.AddScoped<IThemeService, Inventory.Web.Client.Services.ThemeService>();

// Register Culture service
builder.Services.AddScoped<ICultureService, CultureService>();

await builder.Build().RunAsync();


