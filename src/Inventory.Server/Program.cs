using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using Inventory.Shared.Interfaces;
using Inventory.Shared.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Add HttpClient for Blazor components
builder.Services.AddHttpClient();

// Register Shared services for Razor components
builder.Services.AddScoped<IAuthService, AuthApiService>();
builder.Services.AddScoped<IProductService, ProductApiService>();
builder.Services.AddScoped<ICategoryService, CategoryApiService>();



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<Inventory.Server.Models.AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<Inventory.Server.Models.User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<Inventory.Server.Models.AppDbContext>()
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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins ?? Array.Empty<string>())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Автоматическое применение миграций
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Inventory.Server.Models.AppDbContext>();
    db.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Inventory.Server.Models.User>>();

    // Создать роли, если их нет
    const string adminRole = "Admin";
    const string superUserRole = "SuperUser";
    const string userRole = "User";

    if (!await roleManager.RoleExistsAsync(adminRole))
    {
        await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(adminRole));
    }
    
    if (!await roleManager.RoleExistsAsync(superUserRole))
    {
        await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(superUserRole));
    }
    
    if (!await roleManager.RoleExistsAsync(userRole))
    {
        await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(userRole));
    }

    // Создать суперпользователя, если его нет
    const string superUserEmail = "admin@inventory.com";
    const string superUserPassword = "SuperAdmin123!";
    
    var superUser = await userManager.FindByEmailAsync(superUserEmail);
    if (superUser == null)
    {
        superUser = new Inventory.Server.Models.User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "superadmin",
            Email = superUserEmail,
            EmailConfirmed = true,
            Role = superUserRole
        };
        
        var result = await userManager.CreateAsync(superUser, superUserPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(superUser, superUserRole);
            await userManager.AddToRoleAsync(superUser, adminRole);
            Log.Information("Суперпользователь создан: {Email}", superUserEmail);
        }
        else
        {
            Log.Error("Ошибка создания суперпользователя: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowConfiguredOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();

// Разрешаем CORS для Blazor WebAssembly клиента
app.MapFallback(async context =>
{
    context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
    await context.Response.WriteAsync("API Server is running. Use Blazor WebAssembly client to access the application.");
});

app.Run();