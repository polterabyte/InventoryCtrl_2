﻿using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Inventory.API.Middleware;
using Inventory.Shared.Services;
using Inventory.API.Services;
using Inventory.API.Extensions;
using Inventory.Shared.Interfaces;
using Inventory.Shared.DTOs;
using System.Threading.RateLimiting;
using FluentValidation;
using Inventory.API.Validators;
using Inventory.API.Hubs;
using Microsoft.AspNetCore.Components.WebAssembly.Server;
using Inventory.API.Configuration;
using Inventory.API.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Clear default logging and configure Serilog
builder.Logging.ClearProviders();
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));
;

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serializer to handle circular references
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Inventory Control API",
        Version = "v1",
        Description = "RESTful API for inventory management system with comprehensive user-warehouse access control",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Inventory Control Team",
            Email = "support@inventorycontrol.com"
        }
    });

    // Add API operation grouping by tags
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
    c.DocInclusionPredicate((name, api) => true);

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    
    // Configure operation grouping for better organization
    c.EnableAnnotations();
});

// Add CORS configuration
builder.Services.AddCorsConfiguration();

// Database
builder.Services.AddDbContext<Inventory.API.Models.AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<Inventory.API.Models.User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<Inventory.API.Models.AppDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("JWT:Key must be provided via configuration/environment variables in non-development environments.");
}
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey ?? string.Empty)),
        ClockSkew = TimeSpan.FromMinutes(5) // Add some clock skew tolerance
    };
    
    // Configure JWT for SignalR and add event handlers for debugging
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
            {
                context.Token = accessToken;
            }
            
            // Also check Authorization header for SignalR
            if (string.IsNullOrEmpty(accessToken))
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                }
            }
            
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError("JWT Authentication failed: {Error}", context.Exception?.Message ?? "Unknown error");
            logger.LogError("JWT Exception: {Exception}", context.Exception?.ToString());
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var username = context.Principal?.Identity?.Name;
            logger.LogInformation("JWT Token validated for user: {User} (ID: {UserId})", username ?? "Unknown", userId ?? "Unknown");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT Challenge: {Error}", context.Error);
            logger.LogWarning("JWT Challenge Description: {Description}", context.ErrorDescription);
            logger.LogWarning("JWT Challenge URI: {Uri}", context.ErrorUri);
            return Task.CompletedTask;
        }
    };
});

// (Duplicate CORS registration removed)

// Additional CORS configuration for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        var originsEnv = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
        var origins = (originsEnv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (origins.Length > 0)
        {
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins(
                      "http://localhost",
                      "https://localhost",
                      "http://localhost:5000",
                      "https://localhost:5001",
                      "http://localhost:7000",
                      "https://localhost:7001"
                  )
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Add custom services
builder.Services.AddScoped<ILoggingService, LoggingService>();
builder.Services.AddScoped<IDebugLogsService, DebugLogsService>();
builder.Services.AddScoped<IRetryService, RetryService>();
builder.Services.AddScoped<Inventory.Shared.Interfaces.INotificationService, Inventory.API.Services.NotificationService>();
// Remove this line - using Radzen notifications directly now
// builder.Services.AddScoped<Inventory.Shared.Services.IUINotificationService, Inventory.Shared.Services.NotificationService>();
builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Add audit services
builder.Services.AddScoped<SafeSerializationService>();
builder.Services.AddAuditServices();

// Add notification services
builder.Services.AddNotificationServices();

// Add SSL services
builder.Services.AddSSLServices();

// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
    options.StreamBufferCapacity = 10;
});

// Add SignalR notification service
builder.Services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();

// VAPID/Web Push removed

// Add notification rule engine
builder.Services.AddScoped<Inventory.Shared.Interfaces.INotificationRuleEngine, Inventory.API.Services.NotificationRuleEngine>();

// Add reference data services
builder.Services.AddScoped<IReferenceDataService<UnitOfMeasureDto, CreateUnitOfMeasureDto, UpdateUnitOfMeasureDto>, UnitOfMeasureService>();
builder.Services.AddScoped<ILocationService, LocationApiService>();

// Add HttpClient for LocationApiService
builder.Services.AddHttpClient<ILocationService, LocationApiService>(client =>
{
    var apiUrl = builder.Configuration["ApiUrl"];
    if (string.IsNullOrWhiteSpace(apiUrl))
    {
        throw new InvalidOperationException("ApiUrl configuration is required for LocationApiService");
    }
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Add FluentValidation
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<Inventory.API.Services.IUserWarehouseService, UserWarehouseService>();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Global rate limiting policy
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Get user role for different rate limits
        var userRole = context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Anonymous";
        
        return RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: userRole,
            factory: partitionKey => new TokenBucketRateLimiterOptions
            {
                TokenLimit = partitionKey switch
                {
                    "Admin" => 1000,
                    "Manager" => 500,
                    "User" => 100,
                    _ => 50 // Anonymous users
                },
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = partitionKey switch
                {
                    "Admin" => 1000,
                    "Manager" => 500,
                    "User" => 100,
                    _ => 50 // Anonymous users
                },
                AutoReplenishment = true
            });
    });

    // Specific policies for different endpoints
    options.AddPolicy("AuthPolicy", context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            factory: partitionKey => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5, // 5 attempts per window
                Window = TimeSpan.FromMinutes(15), // 15 minute window
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            });
    });

    options.AddPolicy("ApiPolicy", context =>
    {
        var userRole = context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Anonymous";
        return RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: userRole,
            factory: partitionKey => new TokenBucketRateLimiterOptions
            {
                TokenLimit = partitionKey switch
                {
                    "Admin" => 200,
                    "Manager" => 100,
                    "User" => 50,
                    _ => 20 // Anonymous users
                },
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = partitionKey switch
                {
                    "Admin" => 200,
                    "Manager" => 100,
                    "User" => 50,
                    _ => 20 // Anonymous users
                },
                AutoReplenishment = true
            });
    });

    // Configure rejection response
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        
        var response = new
        {
            success = false,
            errorMessage = "Rate limit exceeded. Please try again later.",
            retryAfter = 60
        };
        
        await context.HttpContext.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    };
});

// Note: API services are registered in the client application, not in the API project

var app = builder.Build();

// Log JWT configuration for debugging (do not log key)
var jwtConfigLogger = app.Services.GetRequiredService<ILogger<Program>>();
var jwtConfigSettings = app.Configuration.GetSection("Jwt");
jwtConfigLogger.LogInformation("JWT Configuration - Issuer: {Issuer}, Audience: {Audience}", 
    jwtConfigSettings["Issuer"], jwtConfigSettings["Audience"]);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Initialize database (skip in testing environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    await Inventory.API.Models.DbInitializer.InitializeAsync(app.Services);
}

// Enable static files serving for Blazor WebAssembly
app.UseStaticFiles();

// Enable Blazor WebAssembly files serving
app.UseBlazorFrameworkFiles();

// Skip HTTPS redirection in testing environment or container
if (!app.Environment.IsEnvironment("Testing") && Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    app.UseHttpsRedirection();
}

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// Add authentication middleware
app.UseMiddleware<AuthenticationMiddleware>();

// Add audit middleware
app.UseMiddleware<AuditMiddleware>();

// Configure CORS with port configuration
app.ConfigureCors();

// Apply SignalR CORS policy
app.UseCors("SignalRPolicy");

// Add rate limiting
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<NotificationHub>("/notificationHub", options =>
{
    options.Transports =
        Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
        Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents |
        Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
    options.CloseOnAuthenticationExpiration = true;
    options.ApplicationMaxBufferSize = 1024 * 1024; // 1MB
    options.TransportMaxBufferSize = 1024 * 1024; // 1MB
});

// Map Blazor WebAssembly fallback
app.MapFallbackToFile("index.html");

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 Inventory Control API starting...");
logger.LogInformation("🔗 API running on configured ports");

app.Run();

// Make Program class public for testing
public partial class Program { }



