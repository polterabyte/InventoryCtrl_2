using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;
using Inventory.API.Models;
using Inventory.API.Services;
using Inventory.Shared.DTOs;

namespace Inventory.API.Tests.Services;

public class UserWarehouseServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ILogger<UserWarehouseService>> _loggerMock;
    private readonly UserWarehouseService _service;

    public UserWarehouseServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<UserWarehouseService>>();

        // Mock UserManager
        var store = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);

        _service = new UserWarehouseService(_context, _userManagerMock.Object, _loggerMock.Object);

        SeedTestData();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        // Add test users
        var users = new List<User>
        {
            new() { Id = "user1", UserName = "testuser1", Email = "user1@test.com", Role = "User" },
            new() { Id = "user2", UserName = "testuser2", Email = "user2@test.com", Role = "Manager" },
            new() { Id = "admin1", UserName = "admin1", Email = "admin@test.com", Role = "Admin" }
        };
        _context.Users.AddRange(users);

        // Add test warehouses
        var warehouses = new List<Warehouse>
        {
            new() { Id = 1, Name = "Warehouse A", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Warehouse B", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Name = "Warehouse C", IsActive = false, CreatedAt = DateTime.UtcNow }
        };
        _context.Warehouses.AddRange(warehouses);

        _context.SaveChanges();
    }

    [Fact]
    public async Task AssignWarehouseToUserAsync_ValidAssignment_ShouldSucceed()
    {
        // Arrange
        var userId = "user1";
        var assignmentDto = new AssignWarehouseDto
        {
            WarehouseId = 1,
            AccessLevel = "Full",
            IsDefault = true
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, UserName = "testuser1" });

        // Act
        var result = await _service.AssignWarehouseToUserAsync(userId, assignmentDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(userId, result.Data.UserId);
        Assert.Equal(1, result.Data.WarehouseId);
        Assert.Equal("Full", result.Data.AccessLevel);
        Assert.True(result.Data.IsDefault);

        // Verify database
        var assignment = await _context.UserWarehouses
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WarehouseId == 1);
        Assert.NotNull(assignment);
        Assert.True(assignment.IsDefault);
    }

    [Fact]
    public async Task AssignWarehouseToUserAsync_DuplicateAssignment_ShouldFail()
    {
        // Arrange
        var userId = "user1";
        var warehouseId = 1;

        // Create existing assignment
        var existingAssignment = new UserWarehouse
        {
            UserId = userId,
            WarehouseId = warehouseId,
            AccessLevel = "ReadOnly",
            IsDefault = false,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserWarehouses.Add(existingAssignment);
        await _context.SaveChangesAsync();

        var assignmentDto = new AssignWarehouseDto
        {
            WarehouseId = warehouseId,
            AccessLevel = "Full",
            IsDefault = false
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, UserName = "testuser1" });

        // Act
        var result = await _service.AssignWarehouseToUserAsync(userId, assignmentDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already assigned", result.Error);
    }

    [Fact]
    public async Task AssignWarehouseToUserAsync_SetAsDefault_ShouldClearPreviousDefault()
    {
        // Arrange
        var userId = "user1";

        // Create existing default assignment
        var existingDefault = new UserWarehouse
        {
            UserId = userId,
            WarehouseId = 1,
            AccessLevel = "Full",
            IsDefault = true,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserWarehouses.Add(existingDefault);
        await _context.SaveChangesAsync();

        var assignmentDto = new AssignWarehouseDto
        {
            WarehouseId = 2,
            AccessLevel = "Full",
            IsDefault = true
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, UserName = "testuser1" });

        // Act
        var result = await _service.AssignWarehouseToUserAsync(userId, assignmentDto);

        // Assert
        Assert.True(result.Success);

        // Verify old default is cleared
        var oldDefault = await _context.UserWarehouses
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WarehouseId == 1);
        Assert.NotNull(oldDefault);
        Assert.False(oldDefault.IsDefault);

        // Verify new default is set
        var newDefault = await _context.UserWarehouses
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WarehouseId == 2);
        Assert.NotNull(newDefault);
        Assert.True(newDefault.IsDefault);
    }

    [Fact]
    public async Task RemoveWarehouseAssignmentAsync_LastAssignment_ShouldFail()
    {
        // Arrange
        var userId = "user1";
        var warehouseId = 1;

        // Create only one assignment
        var assignment = new UserWarehouse
        {
            UserId = userId,
            WarehouseId = warehouseId,
            AccessLevel = "Full",
            IsDefault = true,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserWarehouses.Add(assignment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RemoveWarehouseAssignmentAsync(userId, warehouseId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("last warehouse assignment", result.Error);
    }

    [Fact]
    public async Task RemoveWarehouseAssignmentAsync_DefaultWarehouse_ShouldSetNewDefault()
    {
        // Arrange
        var userId = "user1";

        // Create multiple assignments, one default
        var assignments = new List<UserWarehouse>
        {
            new() { UserId = userId, WarehouseId = 1, AccessLevel = "Full", IsDefault = true, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { UserId = userId, WarehouseId = 2, AccessLevel = "ReadOnly", IsDefault = false, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        };
        _context.UserWarehouses.AddRange(assignments);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RemoveWarehouseAssignmentAsync(userId, 1);

        // Assert
        Assert.True(result.Success);

        // Verify assignment is removed
        var removedAssignment = await _context.UserWarehouses
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WarehouseId == 1);
        Assert.Null(removedAssignment);

        // Verify other assignment becomes default
        var newDefault = await _context.UserWarehouses
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WarehouseId == 2);
        Assert.NotNull(newDefault);
        Assert.True(newDefault.IsDefault);
    }

    [Fact]
    public async Task UpdateWarehouseAssignmentAsync_ValidUpdate_ShouldSucceed()
    {
        // Arrange
        var userId = "user1";
        var warehouseId = 1;

        var assignment = new UserWarehouse
        {
            UserId = userId,
            WarehouseId = warehouseId,
            AccessLevel = "ReadOnly",
            IsDefault = false,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserWarehouses.Add(assignment);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateWarehouseAssignmentDto
        {
            AccessLevel = "Full",
            IsDefault = true
        };

        // Act
        var result = await _service.UpdateWarehouseAssignmentAsync(userId, warehouseId, updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Full", result.Data.AccessLevel);
        Assert.True(result.Data.IsDefault);

        // Verify database
        var updatedAssignment = await _context.UserWarehouses
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WarehouseId == warehouseId);
        Assert.NotNull(updatedAssignment);
        Assert.Equal("Full", updatedAssignment.AccessLevel);
        Assert.True(updatedAssignment.IsDefault);
    }

    [Fact]
    public async Task SetDefaultWarehouseAsync_ValidAssignment_ShouldSucceed()
    {
        // Arrange
        var userId = "user1";

        var assignments = new List<UserWarehouse>
        {
            new() { UserId = userId, WarehouseId = 1, AccessLevel = "Full", IsDefault = true, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { UserId = userId, WarehouseId = 2, AccessLevel = "ReadOnly", IsDefault = false, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        };
        _context.UserWarehouses.AddRange(assignments);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SetDefaultWarehouseAsync(userId, 2);

        // Assert
        Assert.True(result.Success);

        // Verify old default is cleared
        var oldDefault = await _context.UserWarehouses
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WarehouseId == 1);
        Assert.NotNull(oldDefault);
        Assert.False(oldDefault.IsDefault);

        // Verify new default is set
        var newDefault = await _context.UserWarehouses
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WarehouseId == 2);
        Assert.NotNull(newDefault);
        Assert.True(newDefault.IsDefault);
    }

    [Fact]
    public async Task GetUserWarehousesAsync_ValidUser_ShouldReturnAssignments()
    {
        // Arrange
        var userId = "user1";

        var assignments = new List<UserWarehouse>
        {
            new() { UserId = userId, WarehouseId = 1, AccessLevel = "Full", IsDefault = true, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { UserId = userId, WarehouseId = 2, AccessLevel = "ReadOnly", IsDefault = false, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        };
        _context.UserWarehouses.AddRange(assignments);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserWarehousesAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.WarehouseId == 1 && x.IsDefault);
        Assert.Contains(result, x => x.WarehouseId == 2 && !x.IsDefault);
    }

    [Fact]
    public async Task CheckWarehouseAccessAsync_AdminUser_ShouldHaveFullAccess()
    {
        // Arrange
        var userId = "admin1";
        var warehouseId = 1;

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, Role = "Admin" });

        // Act
        var result = await _service.CheckWarehouseAccessAsync(userId, warehouseId);

        // Assert
        Assert.True(result.HasAccess);
        Assert.Equal("Full", result.AccessLevel);
    }

    [Fact]
    public async Task CheckWarehouseAccessAsync_UserWithAssignment_ShouldHaveAccess()
    {
        // Arrange
        var userId = "user1";
        var warehouseId = 1;

        var assignment = new UserWarehouse
        {
            UserId = userId,
            WarehouseId = warehouseId,
            AccessLevel = "ReadOnly",
            IsDefault = false,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserWarehouses.Add(assignment);
        await _context.SaveChangesAsync();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, Role = "User" });

        // Act
        var result = await _service.CheckWarehouseAccessAsync(userId, warehouseId);

        // Assert
        Assert.True(result.HasAccess);
        Assert.Equal("ReadOnly", result.AccessLevel);
    }

    [Fact]
    public async Task CheckWarehouseAccessAsync_UserWithoutAssignment_ShouldNotHaveAccess()
    {
        // Arrange
        var userId = "user1";
        var warehouseId = 1;

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, Role = "User" });

        // Act
        var result = await _service.CheckWarehouseAccessAsync(userId, warehouseId);

        // Assert
        Assert.False(result.HasAccess);
        Assert.Null(result.AccessLevel);
    }

    [Fact]
    public async Task GetAccessibleWarehouseIdsAsync_AdminUser_ShouldReturnAllActive()
    {
        // Arrange
        var userId = "admin1";
        var userRole = "Admin";

        // Act
        var result = await _service.GetAccessibleWarehouseIdsAsync(userId, userRole);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Only active warehouses (1 and 2)
        Assert.Contains(1, result);
        Assert.Contains(2, result);
        Assert.DoesNotContain(3, result); // Inactive warehouse
    }

    [Fact]
    public async Task GetAccessibleWarehouseIdsAsync_RegularUser_ShouldReturnOnlyAssigned()
    {
        // Arrange
        var userId = "user1";
        var userRole = "User";

        var assignment = new UserWarehouse
        {
            UserId = userId,
            WarehouseId = 1,
            AccessLevel = "Full",
            IsDefault = true,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserWarehouses.Add(assignment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAccessibleWarehouseIdsAsync(userId, userRole);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(1, result);
    }

    [Fact]
    public async Task ValidateAssignmentAsync_NonexistentUser_ShouldFail()
    {
        // Arrange
        var userId = "nonexistent";
        var warehouseId = 1;

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((User)null);

        // Act
        var result = await _service.ValidateAssignmentAsync(userId, warehouseId);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("User not found", result.Errors);
    }

    [Fact]
    public async Task ValidateAssignmentAsync_InactiveWarehouse_ShouldFail()
    {
        // Arrange
        var userId = "user1";
        var warehouseId = 3; // Inactive warehouse

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId });

        // Act
        var result = await _service.ValidateAssignmentAsync(userId, warehouseId);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Cannot assign user to inactive warehouse", result.Errors);
    }

    [Fact]
    public async Task BulkAssignUsersToWarehouseAsync_ValidAssignments_ShouldSucceed()
    {
        // Arrange
        var bulkAssignDto = new BulkAssignWarehousesDto
        {
            UserIds = new List<string> { "user1", "user2" },
            WarehouseId = 1,
            AccessLevel = "Full",
            SetAsDefault = false
        };

        _userManagerMock.Setup(x => x.FindByIdAsync("user1"))
            .ReturnsAsync(new User { Id = "user1" });
        _userManagerMock.Setup(x => x.FindByIdAsync("user2"))
            .ReturnsAsync(new User { Id = "user2" });

        // Act
        var result = await _service.BulkAssignUsersToWarehouseAsync(bulkAssignDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);
        Assert.Empty(result.Errors);

        // Verify database
        var assignments = await _context.UserWarehouses
            .Where(uw => uw.WarehouseId == 1)
            .ToListAsync();
        Assert.Equal(2, assignments.Count);
    }
}