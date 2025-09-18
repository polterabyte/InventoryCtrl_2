using Inventory.API.Models;
using Xunit;
using FluentAssertions;

namespace Inventory.UnitTests.Models;

public class WarehouseTests
{
    [Fact]
    public void Warehouse_DefaultConstructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var warehouse = new Warehouse();

        // Assert
        warehouse.Should().NotBeNull();
        warehouse.Name.Should().Be(string.Empty);
        warehouse.Address.Should().Be(string.Empty);
        warehouse.IsActive.Should().BeTrue();
        warehouse.Transactions.Should().NotBeNull();
        warehouse.Transactions.Should().BeEmpty();
        warehouse.CreatedAt.Should().Be(default(DateTime));
        warehouse.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Warehouse_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Main Warehouse",
            Address = "123 Main Street, City, State",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Assert
        warehouse.Id.Should().Be(1);
        warehouse.Name.Should().Be("Main Warehouse");
        warehouse.Address.Should().Be("123 Main Street, City, State");
        warehouse.IsActive.Should().BeTrue();
        warehouse.CreatedAt.Should().Be(now);
        warehouse.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Warehouse_WithTransactions_ShouldMaintainTransactionRelationship()
    {
        // Arrange
        var warehouse = new Warehouse { Id = 1, Name = "Main Warehouse" };
        var transaction1 = new InventoryTransaction 
        { 
            Id = 1, 
            WarehouseId = 1, 
            Type = TransactionType.Income, 
            Quantity = 10 
        };
        var transaction2 = new InventoryTransaction 
        { 
            Id = 2, 
            WarehouseId = 1, 
            Type = TransactionType.Outcome, 
            Quantity = 5 
        };

        // Act
        warehouse.Transactions.Add(transaction1);
        warehouse.Transactions.Add(transaction2);

        // Assert
        warehouse.Transactions.Should().HaveCount(2);
        warehouse.Transactions.Should().Contain(transaction1);
        warehouse.Transactions.Should().Contain(transaction2);
    }

    [Fact]
    public void Warehouse_IsActive_ShouldDefaultToTrue()
    {
        // Act
        var warehouse = new Warehouse();

        // Assert
        warehouse.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Warehouse_CanBeInactive()
    {
        // Act
        var warehouse = new Warehouse { IsActive = false };

        // Assert
        warehouse.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Warehouse_CanHaveEmptyName()
    {
        // Act
        var warehouse = new Warehouse { Name = string.Empty };

        // Assert
        warehouse.Name.Should().Be(string.Empty);
    }

    [Fact]
    public void Warehouse_CanHaveEmptyLocation()
    {
        // Act
        var warehouse = new Warehouse { Address = string.Empty };

        // Assert
        warehouse.Address.Should().Be(string.Empty);
    }

    [Fact]
    public void Warehouse_CanHaveNullUpdatedAt()
    {
        // Act
        var warehouse = new Warehouse { UpdatedAt = null };

        // Assert
        warehouse.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Warehouse_CanHaveUpdatedAt()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var warehouse = new Warehouse { UpdatedAt = now };

        // Assert
        warehouse.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Warehouse_CanHaveCreatedAt()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var warehouse = new Warehouse { CreatedAt = now };

        // Assert
        warehouse.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void Warehouse_CanHaveLongName()
    {
        // Arrange
        var longName = new string('A', 1000);

        // Act
        var warehouse = new Warehouse { Name = longName };

        // Assert
        warehouse.Name.Should().Be(longName);
        warehouse.Name.Should().HaveLength(1000);
    }

    [Fact]
    public void Warehouse_CanHaveLongAddress()
    {
        // Arrange
        var longLocation = new string('B', 1000);

        // Act
        var warehouse = new Warehouse { Address = longLocation };

        // Assert
        warehouse.Address.Should().Be(longLocation);
        warehouse.Address.Should().HaveLength(1000);
    }

    [Fact]
    public void Warehouse_CanHaveSpecialCharactersInName()
    {
        // Arrange
        var specialName = "Warehouse #1 - Main (North)";

        // Act
        var warehouse = new Warehouse { Name = specialName };

        // Assert
        warehouse.Name.Should().Be(specialName);
    }

    [Fact]
    public void Warehouse_CanHaveSpecialCharactersInAddress()
    {
        // Arrange
        var specialLocation = "123 Main St. #456, Suite 789, City, ST 12345-6789";

        // Act
        var warehouse = new Warehouse { Address = specialLocation };

        // Assert
        warehouse.Address.Should().Be(specialLocation);
    }
}
