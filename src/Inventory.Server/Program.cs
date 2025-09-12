using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;

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



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<Inventory.Server.Models.AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
        };
    });

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
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

    var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

    const string adminUser = "admin";
    const string adminEmail = "admin@localhost";
    const string adminPassword = "Admin123!";
    const string adminRole = "Admin";

    // Создать роль Admin, если нет
    if (!roleManager.RoleExistsAsync(adminRole).Result)
    {
        roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(adminRole)).Wait();
    }

    // Создать пользователя admin, если нет
    var user = userManager.FindByNameAsync(adminUser).Result;
    if (user == null)
    {
        user = new Microsoft.AspNetCore.Identity.IdentityUser { UserName = adminUser, Email = adminEmail, EmailConfirmed = true };
        var result = userManager.CreateAsync(user, adminPassword).Result;
        if (result.Succeeded)
        {
            userManager.AddToRoleAsync(user, adminRole).Wait();
        }
    }
}
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowConfiguredOrigins");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Настройка Blazor Components с WebAssembly
app.MapRazorComponents<Inventory.Client.App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        // typeof(Inventory.Shared.LoginRequest).Assembly
    );

app.Run();