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

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Initialize database
    await Inventory.API.Models.DbInitializer.InitializeAsync(app.Services);
}

app.UseHttpsRedirection();

// Configure static files
app.UseDefaultFiles();
app.UseStaticFiles();

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure CORS with port configuration
app.ConfigureCorsWithPorts();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Serve static files from WebAssembly client
app.MapFallbackToFile("index.html");

app.Run();
