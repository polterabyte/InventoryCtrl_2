using Inventory.API.Models;
using Xunit;
using FluentAssertions;

namespace Inventory.UnitTests.Models;

public class CategoryTests
{
    [Fact]
    public void Category_DefaultConstructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var category = new Category();

        // Assert
        category.Should().NotBeNull();
        category.Name.Should().Be(string.Empty);
        category.Description.Should().BeNull();
        category.IsActive.Should().BeTrue();
        category.ParentCategoryId.Should().BeNull();
        category.ParentCategory.Should().BeNull();
        category.SubCategories.Should().NotBeNull();
        category.SubCategories.Should().BeEmpty();
        category.Products.Should().NotBeNull();
        category.Products.Should().BeEmpty();
        category.CreatedAt.Should().Be(default(DateTime));
        category.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Category_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var parentCategory = new Category { Id = 1, Name = "Parent" };

        // Act
        var category = new Category
        {
            Id = 1,
            Name = "Electronics",
            Description = "Electronic devices",
            IsActive = true,
            ParentCategoryId = 1,
            ParentCategory = parentCategory,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Assert
        category.Id.Should().Be(1);
        category.Name.Should().Be("Electronics");
        category.Description.Should().Be("Electronic devices");
        category.IsActive.Should().BeTrue();
        category.ParentCategoryId.Should().Be(1);
        category.ParentCategory.Should().Be(parentCategory);
        category.CreatedAt.Should().Be(now);
        category.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Category_WithSubCategories_ShouldMaintainHierarchy()
    {
        // Arrange
        var parentCategory = new Category { Id = 1, Name = "Electronics" };
        var subCategory1 = new Category { Id = 2, Name = "Smartphones", ParentCategoryId = 1 };
        var subCategory2 = new Category { Id = 3, Name = "Laptops", ParentCategoryId = 1 };

        // Act
        parentCategory.SubCategories.Add(subCategory1);
        parentCategory.SubCategories.Add(subCategory2);

        // Assert
        parentCategory.SubCategories.Should().HaveCount(2);
        parentCategory.SubCategories.Should().Contain(subCategory1);
        parentCategory.SubCategories.Should().Contain(subCategory2);
        
        subCategory1.ParentCategory.Should().BeNull(); // Not set automatically
        subCategory2.ParentCategory.Should().BeNull(); // Not set automatically
    }

    [Fact]
    public void Category_WithProducts_ShouldMaintainProductRelationship()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Electronics" };
        var product1 = new Product { Id = 1, Name = "iPhone", CategoryId = 1 };
        var product2 = new Product { Id = 2, Name = "Samsung Galaxy", CategoryId = 1 };

        // Act
        category.Products.Add(product1);
        category.Products.Add(product2);

        // Assert
        category.Products.Should().HaveCount(2);
        category.Products.Should().Contain(product1);
        category.Products.Should().Contain(product2);
    }

    [Fact]
    public void Category_IsActive_ShouldDefaultToTrue()
    {
        // Act
        var category = new Category();

        // Assert
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Category_CanBeInactive()
    {
        // Act
        var category = new Category { IsActive = false };

        // Assert
        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Category_CanHaveNullDescription()
    {
        // Act
        var category = new Category { Description = null };

        // Assert
        category.Description.Should().BeNull();
    }

    [Fact]
    public void Category_CanHaveEmptyDescription()
    {
        // Act
        var category = new Category { Description = string.Empty };

        // Assert
        category.Description.Should().Be(string.Empty);
    }

    [Fact]
    public void Category_CanBeRootCategory()
    {
        // Act
        var category = new Category { ParentCategoryId = null };

        // Assert
        category.ParentCategoryId.Should().BeNull();
        category.ParentCategory.Should().BeNull();
    }

    [Fact]
    public void Category_CanBeChildCategory()
    {
        // Arrange
        var parentCategory = new Category { Id = 1, Name = "Parent" };

        // Act
        var category = new Category 
        { 
            ParentCategoryId = 1, 
            ParentCategory = parentCategory 
        };

        // Assert
        category.ParentCategoryId.Should().Be(1);
        category.ParentCategory.Should().Be(parentCategory);
    }
}
