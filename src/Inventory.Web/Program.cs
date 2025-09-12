using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Inventory.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using Blazored.LocalStorage;
using Inventory.Shared.Interfaces;
using Inventory.Shared.Services;
using Microsoft.Extensions.Logging;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client to point to API server
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5000") });

// Add Blazor authorization services
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Configure logging
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddConsole();

// Register Shared services
builder.Services.AddScoped<IAuthService, AuthApiService>();
builder.Services.AddScoped<IProductService, ProductApiService>();
builder.Services.AddScoped<ICategoryService, CategoryApiService>();

// Register logging and error handling services
builder.Services.AddScoped<ILoggingService, LoggingService>();
builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();

await builder.Build().RunAsync();
