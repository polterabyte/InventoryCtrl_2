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
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // Применение миграций
        await db.Database.MigrateAsync();

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
        const string adminEmail = "admin@localhost";
        const string adminPassword = "Admin123!";
        const string adminRole = "Admin";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true,
                Role = adminRole,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, adminRole);
                Log.Information("Admin user created: {Email} with username: {Username} and password: {Password}", 
                    adminEmail, adminUser.UserName, adminPassword);
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
            if (adminUser.UserName != "admin")
            {
                adminUser.UserName = "admin";
                adminUser.NormalizedUserName = userManager.NormalizeName("admin");
                var updateResult = await userManager.UpdateAsync(adminUser);
                if (updateResult.Succeeded)
                {
                    Log.Information("Username updated to admin for {Email}", adminEmail);
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
