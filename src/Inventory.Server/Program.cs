using Serilog;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<Inventory.Server.Models.AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
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

    app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseAuthentication();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();