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

        // Создание суперпользователя
        await CreateSuperUserAsync(userManager);
        
        // Seed notification data
        await NotificationSeeder.SeedAsync(db);
    }

    private static async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        const string adminRole = "Admin";
        const string superUserRole = "SuperUser";
        const string userRole = "User";

        var roles = new[] { adminRole, superUserRole, userRole };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                Log.Information("Role created: {Role}", role);
            }
        }
    }

    private static async Task CreateSuperUserAsync(UserManager<User> userManager)
    {
        const string superUserEmail = "admin@localhost";
        const string superUserPassword = "Admin123!";
        const string superUserRole = "SuperUser";
        const string adminRole = "Admin";

        var superUser = await userManager.FindByEmailAsync(superUserEmail);
        if (superUser == null)
        {
            superUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "superadmin",
                Email = superUserEmail,
                EmailConfirmed = true,
                Role = superUserRole,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(superUser, superUserPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superUser, superUserRole);
                await userManager.AddToRoleAsync(superUser, adminRole);
                Log.Information("Superuser created: {Email} with username: {Username} and password: {Password}", 
                    superUserEmail, superUser.UserName, superUserPassword);
            }
            else
            {
                Log.Error("Error creating superuser: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Обновляем имя пользователя если оно изменилось
            if (superUser.UserName != "superadmin")
            {
                superUser.UserName = "superadmin";
                superUser.NormalizedUserName = userManager.NormalizeName("superadmin");
                var updateResult = await userManager.UpdateAsync(superUser);
                if (updateResult.Succeeded)
                {
                    Log.Information("Username updated to superadmin for {Email}", superUserEmail);
                }
                else
                {
                    Log.Error("Error updating username: {Errors}", 
                        string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                }
            }
            Log.Information("Superuser already exists: {Email} with username: {Username}", 
                superUserEmail, superUser.UserName);
        }
    }
}
