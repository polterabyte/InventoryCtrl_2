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
using System.Net.Http.Headers;
using Inventory.Shared.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

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
        TestDatabaseName = $"inventory_test_{Guid.NewGuid():N}";
        
        // Set testing environment
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        
        Factory = factory.WithWebHostBuilder(builder =>
        {
            // Add test configuration first
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = $"Host=localhost;Port=5433;Database={TestDatabaseName};Username=postgres;Password=postgres;Pooling=false;",
                    ["Jwt:Key"] = "TestKeyThatIsAtLeast32CharactersLongForTestingPurposes",
                    ["Jwt:Issuer"] = "InventoryTest",
                    ["Jwt:Audience"] = "InventoryTestUsers"
                });
            });

            builder.ConfigureServices((context, services) =>
            {
                // Remove existing registrations to avoid conflicts
                var identityUserOptionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUserStore<User>));
                if (identityUserOptionsDescriptor != null)
                {
                    var identityServices = services.Where(s => s.ServiceType.FullName?.Contains("Microsoft.AspNetCore.Identity") == true).ToList();
                    foreach (var service in identityServices)
                    {
                        services.Remove(service);
                    }
                }

                var authenticationServiceDescriptors = services.Where(d => d.ServiceType.FullName?.Contains("Microsoft.AspNetCore.Authentication") == true).ToList();
                foreach (var service in authenticationServiceDescriptors)
                {
                    services.Remove(service);
                }

                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Remove SSL requirement for testing
                var dbSslRequirement = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));
                if (dbSslRequirement != null) services.Remove(dbSslRequirement);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection"));
                });

                // Configure Identity to match Program.cs
                services.AddIdentity<User, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequiredLength = 6;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

                var jwtSettings = context.Configuration.GetSection("Jwt");
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Bearer";
                    options.DefaultChallengeScheme = "Bearer";
                })
                .AddJwtBearer("Bearer", options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
                    };
                });
            });
        });

        Client = Factory.CreateClient();
        
        var scope = Factory.Services.CreateScope();
        Context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Ensure database is created
        Context.Database.EnsureCreated();
    }

    /// <summary>
    /// Initialize test environment (call this in test methods)
    /// </summary>
    protected async Task InitializeAsync()
    {
        // Create roles and users first
        await CreateTestRolesAsync();
        await CreateTestUsersAsync();
        
        // Then seed the rest of the data
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
            Role = "Admin",
            FirstName = "Test",
            LastName = "Admin"
        };

        var regularUser = new User
        {
            Id = "test-user-1", 
            UserName = "testuser",
            Email = "testuser@example.com",
            EmailConfirmed = true,
            Role = "User",
            FirstName = "Test",
            LastName = "User"
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
    /// Set authentication header for client
    /// </summary>
    protected async Task SetAuthHeaderAsync(string role = "Admin")
    {
        var token = await GetAuthTokenAsync(role);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Get authentication token for test user
    /// </summary>
    protected async Task<string> GetAuthTokenAsync(string role = "Admin")
    {
        // Ensure a clean state before creating users and logging in
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();

        var request = new LoginRequest
        {
            Username = $"test{role.ToLower()}",
            Password = $"{role}123!"
        };

        var content = JsonContent.Create(request);
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = $"Failed to get auth token for user {request.Username} (Role: {role}). " +
                               $"Status Code: {response.StatusCode}. " +
                               $"Response: {responseContent}";
            throw new Exception(errorMessage);
        }

        var authResult = await response.Content.ReadFromJsonAsync<AuthResult>();
        return authResult?.Token ?? throw new Exception("Failed to get auth token");
    }

    /// <summary>
    /// Clean up test data after each test
    /// </summary>
    protected async Task CleanupDatabaseAsync()
    {
        try
        {
            // Use raw SQL to disable foreign key constraints temporarily
            await Context.Database.ExecuteSqlRawAsync("SET session_replication_role = replica;");
            
            // Clear all tables in correct order (respecting foreign key dependencies)
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUserRoles\";");
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUserClaims\";");
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUserLogins\";");
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUserTokens\";");
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetRoleClaims\";");
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUsers\";");
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetRoles\";");
            
            // Clear application tables
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"InventoryTransactions\";");
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"Products\";");
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"ProductModels\";");
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"ProductGroups\";");
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"Categories\";");
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"Manufacturers\";");
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"Warehouses\";");
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"Locations\";");
            
            // Re-enable foreign key constraints
            await Context.Database.ExecuteSqlRawAsync("SET session_replication_role = DEFAULT;");
            
            // Reset identity sequences to start from 1
            await Context.Database.ExecuteSqlRawAsync("ALTER SEQUENCE \"Categories_Id_seq\" RESTART WITH 1;");
            await Context.Database.ExecuteSqlRawAsync("ALTER SEQUENCE \"Manufacturers_Id_seq\" RESTART WITH 1;");
            await Context.Database.ExecuteSqlRawAsync("ALTER SEQUENCE \"ProductGroups_Id_seq\" RESTART WITH 1;");
            await Context.Database.ExecuteSqlRawAsync("ALTER SEQUENCE \"ProductModels_Id_seq\" RESTART WITH 1;");
            await Context.Database.ExecuteSqlRawAsync("ALTER SEQUENCE \"Products_Id_seq\" RESTART WITH 1;");
            await Context.Database.ExecuteSqlRawAsync("ALTER SEQUENCE \"Warehouses_Id_seq\" RESTART WITH 1;");
            await Context.Database.ExecuteSqlRawAsync("ALTER SEQUENCE \"Locations_Id_seq\" RESTART WITH 1;");
            await Context.Database.ExecuteSqlRawAsync("ALTER SEQUENCE \"InventoryTransactions_Id_seq\" RESTART WITH 1;");
            
            // Clear EF Core change tracker
            Context.ChangeTracker.Clear();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error during database cleanup: {ex.Message}");
            
            // Fallback to EF Core cleanup if raw SQL fails
            try
            {
                Context.InventoryTransactions.RemoveRange(Context.InventoryTransactions);
                Context.Products.RemoveRange(Context.Products);
                Context.Categories.RemoveRange(Context.Categories);
                Context.Manufacturers.RemoveRange(Context.Manufacturers);
                Context.ProductModels.RemoveRange(Context.ProductModels);
                Context.ProductGroups.RemoveRange(Context.ProductGroups);
                Context.Warehouses.RemoveRange(Context.Warehouses);
                Context.Locations.RemoveRange(Context.Locations);
                
                await Context.SaveChangesAsync();
                Context.ChangeTracker.Clear();
            }
            catch (Exception fallbackEx)
            {
                Console.WriteLine($"Warning: Fallback cleanup also failed: {fallbackEx.Message}");
            }
        }
    }

    /// <summary>
    /// Create test roles
    /// </summary>
    private async Task CreateTestRolesAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        
        var roles = new[] { "Admin", "Manager", "User" };
        
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
        Location locationA, locationB;
        Category electronicsCategory, smartphonesCategory;

        if (!Context.Locations.Any())
        {
            // Create locations for warehouses
            locationA = new Location { Name = "Building A", IsActive = true, CreatedAt = DateTime.UtcNow };
            locationB = new Location { Name = "Building B", IsActive = true, CreatedAt = DateTime.UtcNow };
            Context.Locations.AddRange(locationA, locationB);
            await Context.SaveChangesAsync();
        }
        else
        {
            locationA = await Context.Locations.FirstAsync(l => l.Name == "Building A");
            locationB = await Context.Locations.FirstAsync(l => l.Name == "Building B");
        }

        // Create test categories
        if (!Context.Categories.Any())
        {
            electronicsCategory = new Category { Name = "Electronics" };
            smartphonesCategory = new Category { Name = "Smartphones" };
            Context.Categories.AddRange(electronicsCategory, smartphonesCategory);
            await Context.SaveChangesAsync();

            smartphonesCategory.ParentCategoryId = electronicsCategory.Id;
            await Context.SaveChangesAsync();
        }
        else
        {
            electronicsCategory = await Context.Categories.FirstAsync(c => c.Name == "Electronics");
            smartphonesCategory = await Context.Categories.FirstAsync(c => c.Name == "Smartphones");
        }

        // Create test manufacturers
        if (!Context.Manufacturers.Any())
        {
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
        }

        // Create test product groups
        if (!Context.ProductGroups.Any())
        {
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
        }

        // Create test product models
        if (!Context.ProductModels.Any())
        {
            var iphoneModel = new ProductModel
            {
                Name = "iPhone 15"
            };

            var galaxyModel = new ProductModel
            {
                Name = "Galaxy S24"
            };

            Context.ProductModels.AddRange(iphoneModel, galaxyModel);
            await Context.SaveChangesAsync();
        }

        // Create test warehouses
        if (!Context.Warehouses.Any())
        {
            var mainWarehouse = new Warehouse
            {
                Name = "Main Warehouse",
                LocationId = locationA.Id,
                IsActive = true
            };
            var secondaryWarehouse = new Warehouse
            {
                Name = "Secondary Warehouse",
                LocationId = locationB.Id,
                IsActive = true
            };
            Context.Warehouses.AddRange(mainWarehouse, secondaryWarehouse);
            await Context.SaveChangesAsync();
        }

        // Create test unit of measures
        if (!Context.UnitOfMeasures.Any())
        {
            var unitOfMeasure = new UnitOfMeasure
            {
                Name = "Pieces",
                Symbol = "pcs",
                Description = "Individual items",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            Context.UnitOfMeasures.Add(unitOfMeasure);
            await Context.SaveChangesAsync();
        }

        // Create test products
        if (!Context.Products.Any())
        {
            var iphoneProduct = new Product
            {
                Name = "iPhone 15",
                SKU = "IPHONE15-001",
                Description = "Latest iPhone model",
                CurrentQuantity = 50,
                UnitOfMeasureId = (await Context.UnitOfMeasures.FirstAsync()).Id,
                IsActive = true,
                CategoryId = smartphonesCategory.Id,
                ManufacturerId = (await Context.Manufacturers.FirstAsync(m => m.Name == "Apple")).Id,
                ProductModelId = (await Context.ProductModels.FirstAsync(m => m.Name == "iPhone 15")).Id,
                ProductGroupId = (await Context.ProductGroups.FirstAsync(g => g.Name == "Phones")).Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var samsungProduct = new Product
            {
                Name = "Samsung Galaxy S24",
                SKU = "GALAXYS24-001", 
                Description = "Latest Samsung Galaxy model",
                CurrentQuantity = 30,
                UnitOfMeasureId = (await Context.UnitOfMeasures.FirstAsync()).Id,
                IsActive = true,
                CategoryId = smartphonesCategory.Id,
                ManufacturerId = (await Context.Manufacturers.FirstAsync(m => m.Name == "Samsung")).Id,
                ProductModelId = (await Context.ProductModels.FirstAsync(m => m.Name == "Galaxy S24")).Id,
                ProductGroupId = (await Context.ProductGroups.FirstAsync(g => g.Name == "Phones")).Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Context.Products.AddRange(iphoneProduct, samsungProduct);
            await Context.SaveChangesAsync();
        }

        // Create test transactions
        if (!Context.InventoryTransactions.Any())
        {
            var incomeTransaction = new InventoryTransaction
            {
                ProductId = (await Context.Products.FirstAsync(p => p.Name == "iPhone 15")).Id,
                WarehouseId = (await Context.Warehouses.FirstAsync(w => w.Name == "Main Warehouse")).Id,
                Type = TransactionType.Income,
                Quantity = 20,
                Date = DateTime.UtcNow.AddDays(-1),
                UserId = "test-admin-1",
                Description = "Initial stock"
            };

            var outcomeTransaction = new InventoryTransaction
            {
                ProductId = (await Context.Products.FirstAsync(p => p.Name == "Samsung Galaxy S24")).Id,
                WarehouseId = (await Context.Warehouses.FirstAsync(w => w.Name == "Main Warehouse")).Id,
                Type = TransactionType.Outcome,
                Quantity = 5,
                Date = DateTime.UtcNow.AddDays(-2),
                UserId = "test-user-1",
                Description = "Sale transaction"
            };

            Context.InventoryTransactions.AddRange(incomeTransaction, outcomeTransaction);

            await Context.SaveChangesAsync();
        }
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
