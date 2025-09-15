using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Inventory.API.Middleware;
using Inventory.Shared.Services;
using Inventory.API.Services;
using Inventory.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Clear default logging and configure Serilog
builder.Logging.ClearProviders();
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));
;

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Inventory Control API",
        Version = "v1",
        Description = "RESTful API for inventory management system",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Inventory Control Team",
            Email = "support@inventorycontrol.com"
        }
    });

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
});

// Add port configuration service
builder.Services.AddPortConfiguration();

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? string.Empty))
    };
});

// CORS with port configuration
builder.Services.AddCorsWithPorts();

// Add custom services
builder.Services.AddScoped<ILoggingService, LoggingService>();
builder.Services.AddScoped<IDebugLogsService, DebugLogsService>();
builder.Services.AddScoped<IRetryService, RetryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Initialize database (skip in testing environment)
    if (!app.Environment.IsEnvironment("Testing"))
    {
        await Inventory.API.Models.DbInitializer.InitializeAsync(app.Services);
    }
}

// Skip HTTPS redirection in testing environment
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure CORS with port configuration
app.ConfigureCorsWithPorts();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Configure ports and log information
var portConfigService = app.Services.GetRequiredService<IPortConfigurationService>();
var portConfig = portConfigService.LoadPortConfiguration();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Configure Kestrel to listen on configured ports
app.Urls.Clear();
app.Urls.Add($"http://localhost:{portConfig.ApiHttp}");
app.Urls.Add($"https://localhost:{portConfig.ApiHttps}");

// Log port information on startup
logger.LogInformation("🚀 Inventory Control API starting...");
logger.LogInformation("📡 API HTTP Port: {HttpPort}", portConfig.ApiHttp);
logger.LogInformation("🔒 API HTTPS Port: {HttpsPort}", portConfig.ApiHttps);
logger.LogInformation("🌐 Web HTTP Port: {WebHttpPort}", portConfig.WebHttp);
logger.LogInformation("🔐 Web HTTPS Port: {WebHttpsPort}", portConfig.WebHttps);
logger.LogInformation("🔗 API URLs: http://localhost:{HttpPort} | https://localhost:{HttpsPort}", portConfig.ApiHttp, portConfig.ApiHttps);
logger.LogInformation("🔗 Web URLs: http://localhost:{WebHttpPort} | https://localhost:{WebHttpsPort}", portConfig.WebHttp, portConfig.WebHttps);

app.Run();

// Make Program class public for testing
public partial class Program { }