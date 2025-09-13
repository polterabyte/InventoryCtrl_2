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
                Log.Information("Роль создана: {Role}", role);
            }
        }
    }

    private static async Task CreateSuperUserAsync(UserManager<User> userManager)
    {
        const string superUserEmail = "admin@inventory.com";
        const string superUserPassword = "SuperAdmin123!";
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
                Log.Error("Ошибка создания суперпользователя: {Errors}", 
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
                    Log.Information("Имя пользователя обновлено на superadmin для {Email}", superUserEmail);
                }
                else
                {
                    Log.Error("Ошибка обновления имени пользователя: {Errors}", 
                        string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                }
            }
            Log.Information("Суперпользователь уже существует: {Email}", superUserEmail);
        }
    }
}
