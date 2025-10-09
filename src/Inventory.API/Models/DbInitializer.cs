using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        var kanbanSettings = await EnsureKanbanSettingsAsync(db, configuration);

        // Создание ролей
        await CreateRolesAsync(roleManager);

        // Создание администратора
        await CreateAdminUserAsync(userManager, configuration);
        
        // Seed reference data
        await SeedReferenceDataAsync(db);

        // Seed notification data
        await NotificationSeeder.SeedAsync(db, kanbanSettings);
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

    private static async Task CreateAdminUserAsync(UserManager<User> userManager, IConfiguration configuration)
    {
        const string adminRole = "Admin";
        // Use injected IConfiguration instead of creating new one
        var adminEmail = configuration["ADMIN_EMAIL"];
        var adminUserName = configuration["ADMIN_USERNAME"];
        var adminPassword = configuration["ADMIN_PASSWORD"];
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
            
            // Обновляем пароль если он указан в конфигурации
            if (!string.IsNullOrEmpty(adminPassword))
            {
                // Удаляем текущий пароль и устанавливаем новый
                await userManager.RemovePasswordAsync(adminUser);
                var passwordResult = await userManager.AddPasswordAsync(adminUser, adminPassword);
                if (passwordResult.Succeeded)
                {
                    Log.Information("Admin password updated for {Email}", adminEmail);
                }
                else
                {
                    Log.Error("Error updating admin password: {Errors}", 
                        string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
                }
            }
            
            Log.Information("Admin user already exists: {Email} with username: {Username}", 
                adminEmail, adminUser.UserName);
        }
    }

    private static async Task<KanbanSettings> EnsureKanbanSettingsAsync(AppDbContext db, IConfiguration configuration)
    {
        var existing = await db.KanbanSettings.FirstOrDefaultAsync();
        var configuredMin = configuration.GetValue<int?>("KanbanSettings:DefaultMinThreshold");
        var configuredMax = configuration.GetValue<int?>("KanbanSettings:DefaultMaxThreshold");

        if (existing != null)
        {
            var minThreshold = configuredMin ?? existing.DefaultMinThreshold;
            var maxThreshold = configuredMax ?? existing.DefaultMaxThreshold;

            if (maxThreshold < minThreshold)
            {
                maxThreshold = minThreshold;
            }

            if (existing.DefaultMinThreshold != minThreshold || existing.DefaultMaxThreshold != maxThreshold)
            {
                existing.DefaultMinThreshold = minThreshold;
                existing.DefaultMaxThreshold = maxThreshold;
                existing.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            return existing;
        }

        var resolvedMin = configuredMin ?? 5;
        var resolvedMax = configuredMax ?? Math.Max(resolvedMin, 20);
        if (resolvedMax < resolvedMin)
        {
            resolvedMax = resolvedMin;
        }

        var settings = new KanbanSettings
        {
            DefaultMinThreshold = resolvedMin,
            DefaultMaxThreshold = resolvedMax,
            UpdatedAt = DateTime.UtcNow
        };

        db.KanbanSettings.Add(settings);
        await db.SaveChangesAsync();
        return settings;
    }

    private static async Task SeedReferenceDataAsync(AppDbContext db)
    {
        // Seed locations
        if (!await db.Locations.AnyAsync())
        {
            var location = new Location
            {
                Name = "Main Building",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Locations.Add(location);
            await db.SaveChangesAsync();
            Log.Information("Created location: {Name}", location.Name);
        }

        // Seed unit of measures
        if (!await db.UnitOfMeasures.AnyAsync())
        {
            var unit = new UnitOfMeasure
            {
                Name = "Pieces",
                Symbol = "pcs",
                Description = "Individual items",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.UnitOfMeasures.Add(unit);
            await db.SaveChangesAsync();
            Log.Information("Created unit of measure: {Name}", unit.Name);
        }

        // Seed categories
        if (!await db.Categories.AnyAsync())
        {
            var category = new Category
            {
                Name = "Electronics",
                CreatedAt = DateTime.UtcNow
            };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            Log.Information("Created category: {Name}", category.Name);
        }

        // Seed product groups
        if (!await db.ProductGroups.AnyAsync())
        {
            var productGroup = new ProductGroup
            {
                Name = "Smartphones",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.ProductGroups.Add(productGroup);
            await db.SaveChangesAsync();
            Log.Information("Created product group: {Name}", productGroup.Name);
        }

        // Seed product models
        if (!await db.ProductModels.AnyAsync())
        {
            var productModel = new ProductModel
            {
                Name = "iPhone Series",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.ProductModels.Add(productModel);
            await db.SaveChangesAsync();
            Log.Information("Created product model: {Name}", productModel.Name);
        }

        // Seed warehouses
        if (!await db.Warehouses.AnyAsync())
        {
            var warehouse1 = new Warehouse
            {
                Name = "Main Warehouse",
                LocationId = 1,
                IsActive = true
            };
            var warehouse2 = new Warehouse
            {
                Name = "Secondary Warehouse",
                LocationId = 1,
                IsActive = true
            };
            db.Warehouses.AddRange(warehouse1, warehouse2);
            await db.SaveChangesAsync();
            Log.Information("Created warehouses: {Name1}, {Name2}", warehouse1.Name, warehouse2.Name);
        }

        // Seed products
        if (!await db.Products.AnyAsync())
        {
            var product1 = new Product
            {
                Name = "iPhone 15",
                Description = "Latest iPhone model",
                CurrentQuantity = 50,
                UnitOfMeasureId = 1,
                IsActive = true,
                CategoryId = 1,
                ProductGroupId = 1,
                ProductModelId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var product2 = new Product
            {
                Name = "Samsung Galaxy S24",
                Description = "Latest Samsung Galaxy model",
                CurrentQuantity = 30,
                UnitOfMeasureId = 1,
                IsActive = true,
                CategoryId = 1,
                ProductGroupId = 1,
                ProductModelId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Products.AddRange(product1, product2);
            await db.SaveChangesAsync();
            Log.Information("Created products: {Name1} (ID: {Id1}), {Name2} (ID: {Id2})",
                product1.Name, product1.Id, product2.Name, product2.Id);
        }
    }
}
