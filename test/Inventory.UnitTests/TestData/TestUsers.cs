using Inventory.API.Models;

namespace Inventory.UnitTests.TestData;

public static class TestUsers
{
    public static User AdminUser => new()
    {
        Id = "admin-id",
        UserName = "admin",
        Email = "admin@test.com",
        Role = "Admin",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public static User RegularUser => new()
    {
        Id = "user-id",
        UserName = "user",
        Email = "user@test.com",
        Role = "User",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public static User ManagerUser => new()
    {
        Id = "manager-id",
        UserName = "manager",
        Email = "manager@test.com",
        Role = "Manager",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
