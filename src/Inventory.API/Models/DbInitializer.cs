using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Inventory.API.Models;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // Применение миграций
        await db.Database.MigrateAsync();

        // Обновление/создание SQL VIEW'ов (идемпотентно)
        await SqlViewInitializer.EnsureViewsAsync(db);

        // Создание ролей
        await CreateRolesAsync(roleManager);

        // Создание администратора
        await CreateAdminUserAsync(userManager);
        
        // Seed notification data
        await NotificationSeeder.SeedAsync(db);
    }

    private static async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        const string adminRole = "Admin";
        const string managerRole = "Manager";
        const string userRole = "User";

        var roles = new[] { adminRole, managerRole, userRole };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                Log.Information("Role created: {Role}", role);
            }
        }
    }

    private static async Task CreateAdminUserAsync(UserManager<User> userManager)
    {
        const string adminRole = "Admin";
        // Prefer IConfiguration; allow ENV to override
        var configSection = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var adminEmail = configSection["ADMIN_EMAIL"];
        var adminUserName = configSection["ADMIN_USERNAME"];
        var adminPassword = configSection["ADMIN_PASSWORD"];
        // Fallbacks to appsettings (Identity:DefaultAdmin)
        adminEmail ??= userManager.Options?.Stores?.ProtectPersonalData == false
            ? null
            : null; // keep null; we'll check later
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminUserName))
        {
            // As a last resort, use safe defaults
            adminEmail ??= "admin@localhost";
            adminUserName ??= "admin";
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = adminUserName,
                Email = adminEmail,
                EmailConfirmed = true,
                Role = adminRole,
                CreatedAt = DateTime.UtcNow
            };
            
            IdentityResult result;
            if (!string.IsNullOrEmpty(adminPassword))
            {
                result = await userManager.CreateAsync(adminUser, adminPassword);
            }
            else
            {
                // Create without password if none provided, admin must set it later
                result = await userManager.CreateAsync(adminUser);
            }
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, adminRole);
                Log.Information("Admin user created: {Email} with username: {Username}", 
                    adminEmail, adminUser.UserName);
            }
            else
            {
                Log.Error("Error creating admin user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Обновляем имя пользователя если оно изменилось
            if (adminUser.UserName != adminUserName)
            {
                adminUser.UserName = adminUserName;
                adminUser.NormalizedUserName = userManager.NormalizeName(adminUserName);
                var updateResult = await userManager.UpdateAsync(adminUser);
                if (updateResult.Succeeded)
                {
                    Log.Information("Username updated for {Email}", adminEmail);
                }
                else
                {
                    Log.Error("Error updating username: {Errors}", 
                        string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                }
            }
            Log.Information("Admin user already exists: {Email} with username: {Username}", 
                adminEmail, adminUser.UserName);
        }
    }
}
