using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using System.Data.Common;
using Xunit;
using Microsoft.AspNetCore.Identity;
using System.Net.Http.Json;

namespace Inventory.IntegrationTests;

/// <summary>
/// Base class for integration tests with test database setup
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    protected readonly AppDbContext Context;
    protected readonly string TestDatabaseName;

    protected IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        TestDatabaseName = $"inventory_test_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        // Set testing environment
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Add test database connection with unique name
                services.AddDbContext<AppDbContext>(options =>
                {
                    var connectionString = $"Host=localhost;Port=5432;Database={TestDatabaseName};Username=postgres;Password=postgres;Pooling=false;";
                    options.UseNpgsql(connectionString);
                });
            });

            // Add test configuration
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = $"Host=localhost;Port=5432;Database={TestDatabaseName};Username=postgres;Password=postgres;Pooling=false;",
                    ["Jwt:Key"] = "TestKeyThatIsAtLeast32CharactersLongForTestingPurposes",
                    ["Jwt:Issuer"] = "InventoryTest",
                    ["Jwt:Audience"] = "InventoryTestUsers"
                });
            });
        });

        Client = Factory.CreateClient();
        Context = Factory.Services.GetRequiredService<AppDbContext>();
        
        // Ensure database is created
        Context.Database.EnsureCreated();
    }

    /// <summary>
    /// Initialize test environment (call this in test methods)
    /// </summary>
    protected async Task InitializeAsync()
    {
        // Create roles for testing
        await CreateTestRolesAsync();
        
        // Create test users
        await SeedTestDataAsync();
    }
    
    /// <summary>
    /// Initialize test environment without test data (for empty database tests)
    /// </summary>
    protected async Task InitializeEmptyAsync()
    {
        // Create roles for testing
        await CreateTestRolesAsync();
        
        // Create only test users, no other data
        await CreateTestUsersAsync();
    }
    
    /// <summary>
    /// Create test users only (without other test data)
    /// </summary>
    protected async Task CreateTestUsersAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        
        var adminUser = new User
        {
            Id = "test-admin-1",
            UserName = "testadmin",
            Email = "testadmin@example.com",
            EmailConfirmed = true,
            Role = "Admin"
        };

        var regularUser = new User
        {
            Id = "test-user-1", 
            UserName = "testuser",
            Email = "testuser@example.com",
            EmailConfirmed = true,
            Role = "User"
        };

        // Create or update users
        var existingAdmin = await userManager.FindByNameAsync("testadmin");
        if (existingAdmin == null)
        {
            var createResult = await userManager.CreateAsync(adminUser, "Admin123!");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        var existingUser = await userManager.FindByNameAsync("testuser");
        if (existingUser == null)
        {
            var createResult = await userManager.CreateAsync(regularUser, "User123!");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(regularUser, "User");
            }
        }
    }

    /// <summary>
    /// Get authentication token for test user
    /// </summary>
    protected async Task<string> GetAuthTokenAsync(string username = "testadmin", string password = "Admin123!")
    {
        var loginRequest = new { Username = username, Password = password };
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResult>();
            return result?.Token ?? string.Empty;
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Login failed for {username}: {response.StatusCode} - {errorContent}");
        }
        
        return string.Empty;
    }

    /// <summary>
    /// Set authentication header for client
    /// </summary>
    protected async Task SetAuthHeaderAsync(string username = "testadmin", string password = "Admin123!")
    {
        var token = await GetAuthTokenAsync(username, password);
        if (!string.IsNullOrEmpty(token))
        {
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            // Clear any existing auth header if token is empty
            Client.DefaultRequestHeaders.Authorization = null;
        }
    }

    /// <summary>
    /// Clean up test data after each test
    /// </summary>
    protected async Task CleanupDatabaseAsync()
    {
        // Delete all data from tables in reverse dependency order
        Context.InventoryTransactions.RemoveRange(Context.InventoryTransactions);
        Context.Products.RemoveRange(Context.Products);
        Context.Categories.RemoveRange(Context.Categories);
        Context.Manufacturers.RemoveRange(Context.Manufacturers);
        Context.ProductModels.RemoveRange(Context.ProductModels);
        Context.ProductGroups.RemoveRange(Context.ProductGroups);
        Context.Warehouses.RemoveRange(Context.Warehouses);
        Context.Locations.RemoveRange(Context.Locations);
        Context.Users.RemoveRange(Context.Users);
        
        await Context.SaveChangesAsync();
        
        // Clear Identity tables
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Remove all users
        var users = userManager.Users.ToList();
        foreach (var user in users)
        {
            await userManager.DeleteAsync(user);
        }
        
        // Remove all roles
        var roles = roleManager.Roles.ToList();
        foreach (var role in roles)
        {
            await roleManager.DeleteAsync(role);
        }
    }

    /// <summary>
    /// Create test roles
    /// </summary>
    private async Task CreateTestRolesAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        
        var roles = new[] { "Admin", "SuperUser", "User" };
        
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    /// <summary>
    /// Seed test data
    /// </summary>
    protected async Task SeedTestDataAsync()
    {
        // Ensure roles exist first
        await CreateTestRolesAsync();
        
        // Create test users through UserManager
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        
        var adminUser = new User
        {
            Id = "test-admin-1",
            UserName = "testadmin",
            Email = "testadmin@example.com",
            EmailConfirmed = true,
            Role = "Admin"
        };

        var regularUser = new User
        {
            Id = "test-user-1", 
            UserName = "testuser",
            Email = "testuser@example.com",
            EmailConfirmed = true,
            Role = "User"
        };

        // Create or update users
        var existingAdmin = await userManager.FindByNameAsync("testadmin");
        if (existingAdmin == null)
        {
            var createResult = await userManager.CreateAsync(adminUser, "Admin123!");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            else
            {
                Console.WriteLine($"Failed to create admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            // Update password if user exists
            await userManager.RemovePasswordAsync(existingAdmin);
            var passwordResult = await userManager.AddPasswordAsync(existingAdmin, "Admin123!");
            if (passwordResult.Succeeded)
            {
                if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
                {
                    await userManager.AddToRoleAsync(existingAdmin, "Admin");
                }
            }
            else
            {
                Console.WriteLine($"Failed to update admin password: {string.Join(", ", passwordResult.Errors.Select(e => e.Description))}");
            }
        }

        var existingUser = await userManager.FindByNameAsync("testuser");
        if (existingUser == null)
        {
            var createResult = await userManager.CreateAsync(regularUser, "User123!");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(regularUser, "User");
            }
            else
            {
                Console.WriteLine($"Failed to create regular user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            // Update password if user exists
            await userManager.RemovePasswordAsync(existingUser);
            var passwordResult = await userManager.AddPasswordAsync(existingUser, "User123!");
            if (passwordResult.Succeeded)
            {
                if (!await userManager.IsInRoleAsync(existingUser, "User"))
                {
                    await userManager.AddToRoleAsync(existingUser, "User");
                }
            }
            else
            {
                Console.WriteLine($"Failed to update regular user password: {string.Join(", ", passwordResult.Errors.Select(e => e.Description))}");
            }
        }

        // Create test categories
        var electronicsCategory = new Category
        {
            Name = "Electronics",
            Description = "Electronic devices",
            IsActive = true,
            ParentCategoryId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var smartphonesCategory = new Category
        {
            Name = "Smartphones",
            Description = "Mobile phones",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Context.Categories.AddRange(electronicsCategory, smartphonesCategory);
        await Context.SaveChangesAsync();
        
        // Set parent category after saving
        smartphonesCategory.ParentCategoryId = electronicsCategory.Id;
        await Context.SaveChangesAsync();

        // Create test manufacturers
        var appleManufacturer = new Manufacturer
        {
            Name = "Apple"
        };

        var samsungManufacturer = new Manufacturer
        {
            Name = "Samsung"
        };

        Context.Manufacturers.AddRange(appleManufacturer, samsungManufacturer);
        await Context.SaveChangesAsync();

        // Create test product groups
        var phoneGroup = new ProductGroup
        {
            Name = "Phones"
        };

        var tabletGroup = new ProductGroup
        {
            Name = "Tablets"
        };

        Context.ProductGroups.AddRange(phoneGroup, tabletGroup);
        await Context.SaveChangesAsync();

        // Create test product models
        var iphoneModel = new ProductModel
        {
            Name = "iPhone 15",
            ManufacturerId = appleManufacturer.Id
        };

        var galaxyModel = new ProductModel
        {
            Name = "Galaxy S24",
            ManufacturerId = samsungManufacturer.Id
        };

        Context.ProductModels.AddRange(iphoneModel, galaxyModel);
        await Context.SaveChangesAsync();

        // Create test warehouses
        var mainWarehouse = new Warehouse
        {
            Name = "Main Warehouse",
            Location = "Building A",
            IsActive = true
        };

        var secondaryWarehouse = new Warehouse
        {
            Name = "Secondary Warehouse", 
            Location = "Building B",
            IsActive = true
        };

        Context.Warehouses.AddRange(mainWarehouse, secondaryWarehouse);
        await Context.SaveChangesAsync();

        // Create test products
        var iphoneProduct = new Product
        {
            Name = "iPhone 15",
            SKU = "IPHONE15-001",
            Description = "Latest iPhone model",
            Quantity = 50,
            Unit = "pcs",
            IsActive = true,
            CategoryId = smartphonesCategory.Id,
            ManufacturerId = appleManufacturer.Id,
            ProductModelId = iphoneModel.Id,
            ProductGroupId = phoneGroup.Id,
            MinStock = 10,
            MaxStock = 100,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var samsungProduct = new Product
        {
            Name = "Samsung Galaxy S24",
            SKU = "GALAXYS24-001", 
            Description = "Latest Samsung Galaxy model",
            Quantity = 30,
            Unit = "pcs",
            IsActive = true,
            CategoryId = smartphonesCategory.Id,
            ManufacturerId = samsungManufacturer.Id,
            ProductModelId = galaxyModel.Id,
            ProductGroupId = phoneGroup.Id,
            MinStock = 5,
            MaxStock = 80,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Context.Products.AddRange(iphoneProduct, samsungProduct);
        await Context.SaveChangesAsync();

        // Create test transactions
        var incomeTransaction = new InventoryTransaction
        {
            ProductId = iphoneProduct.Id,
            WarehouseId = mainWarehouse.Id,
            Type = TransactionType.Income,
            Quantity = 20,
            Date = DateTime.UtcNow.AddDays(-1),
            UserId = "test-admin-1",
            Description = "Initial stock"
        };

        var outcomeTransaction = new InventoryTransaction
        {
            ProductId = samsungProduct.Id,
            WarehouseId = mainWarehouse.Id,
            Type = TransactionType.Outcome,
            Quantity = 5,
            Date = DateTime.UtcNow.AddDays(-2),
            UserId = "test-user-1",
            Description = "Sale transaction"
        };

        Context.InventoryTransactions.AddRange(incomeTransaction, outcomeTransaction);

        await Context.SaveChangesAsync();
    }

    public void Dispose()
    {
        // Clean up test database
        try
        {
            Context.Database.EnsureDeleted();
        }
        catch
        {
            // Ignore cleanup errors
        }
        
        Context.Dispose();
        Client.Dispose();
        
        // Clean up test database from PostgreSQL
        CleanupTestDatabase();
    }
    
    private void CleanupTestDatabase()
    {
        try
        {
            // Use PowerShell to clean up the test database
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"docker exec inventoryctrl-db-1 psql -U postgres -c 'DROP DATABASE IF EXISTS \\\"{TestDatabaseName}\\\";'\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            
            process.Start();
            process.WaitForExit(5000); // Wait max 5 seconds
            
            if (process.ExitCode != 0)
            {
                Console.WriteLine($"Warning: Failed to clean up test database {TestDatabaseName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error cleaning up test database: {ex.Message}");
        }
    }
}
