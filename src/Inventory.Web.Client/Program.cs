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

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add port configuration service first
builder.Services.AddScoped<PortConfigurationService>();

// Configure API HTTP client using port configuration service
builder.Services.AddScoped(sp =>
{
    var portService = sp.GetRequiredService<PortConfigurationService>();
    var apiUrl = portService.GetApiUrl();
    return new HttpClient { BaseAddress = new Uri(apiUrl) };
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

// Register Shared services
builder.Services.AddScoped<IAuthService, AuthApiService>();
builder.Services.AddScoped<IProductService, ProductApiService>();
builder.Services.AddScoped<ICategoryService, CategoryApiService>();
builder.Services.AddScoped<IDashboardService, DashboardApiService>();

// Register logging and error handling services
builder.Services.AddScoped<ILoggingService, LoggingService>();
builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();

// Register notification and retry services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRetryService, RetryService>();
builder.Services.AddScoped<IDebugLogsService, DebugLogsService>();

// Register authorization services
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();

await builder.Build().RunAsync();
