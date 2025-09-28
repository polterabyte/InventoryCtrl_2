# Category and Manufacturer Models

<cite>
**Referenced Files in This Document**   
- [Category.cs](file://src/Inventory.API/Models/Category.cs)
- [Manufacturer.cs](file://src/Inventory.API/Models/Manufacturer.cs)
- [Product.cs](file://src/Inventory.API/Models/Product.cs)
- [CreateCategoryDtoValidator.cs](file://src/Inventory.API/Validators/CreateCategoryDtoValidator.cs)
- [UpdateCategoryDtoValidator.cs](file://src/Inventory.API/Validators/UpdateCategoryDtoValidator.cs)
- [CreateManufacturerDtoValidator.cs](file://src/Inventory.API/Validators/CreateManufacturerDtoValidator.cs)
- [UpdateManufacturerDtoValidator.cs](file://src/Inventory.API/Validators/UpdateManufacturerDtoValidator.cs)
- [20250928092414_InitialCreate.cs](file://src/Inventory.API/Migrations/20250928092414_InitialCreate.cs)
- [20250928113056_AddCriticalPerformanceIndexes.Designer.cs](file://src/Inventory.API/Migrations/20250928113056_AddCriticalPerformanceIndexes.Designer.cs)
</cite>

## Table of Contents
1. [Introduction](#introduction)
2. [Category Model](#category-model)
3. [Manufacturer Model](#manufacturer-model)
4. [Data Relationships](#data-relationships)
5. [Validation Rules](#validation-rules)
6. [Usage in Product Management](#usage-in-product-management)
7. [Query Examples](#query-examples)
8. [Performance Considerations](#performance-considerations)

## Introduction
This document provides comprehensive data model documentation for the Category and Manufacturer entities in the InventoryCtrl_2 system. These reference data entities are fundamental to product organization, categorization, and reporting. The Category model supports hierarchical classification of products, while the Manufacturer model captures supplier information and relationships. Both entities are critical for inventory management, filtering, and analytics.

## Category Model

The Category entity represents a classification system for products, enabling hierarchical organization and efficient product discovery. It supports nested subcategories through a self-referencing relationship.

### Properties
- **Id**: Unique integer identifier (Primary Key)
- **Name**: Required string (2-100 characters), represents the category name
- **Description**: Optional string (up to 500 characters), provides additional details
- **IsActive**: Boolean flag indicating if the category is active (default: true)
- **ParentCategoryId**: Optional integer, foreign key to parent category for hierarchy
- **CreatedAt**: Timestamp of creation (required)
- **UpdatedAt**: Timestamp of last modification (optional)

### Hierarchical Structure
The Category model implements a self-referencing hierarchy through the `ParentCategoryId` and `ParentCategory` navigation property. This allows for unlimited nesting levels, enabling complex classification systems (e.g., Electronics → Computers → Laptops).

**Section sources**
- [Category.cs](file://src/Inventory.API/Models/Category.cs#L2-L14)

## Manufacturer Model

The Manufacturer entity represents product suppliers or brands within the inventory system. It captures essential information about manufacturers and their relationship to products.

### Properties
- **Id**: Unique integer identifier (Primary Key)
- **Name**: Required string (2-100 characters), represents the manufacturer name
- **Description**: Optional string (up to 500 characters), provides additional details
- **Website**: Optional valid URL (http/https), represents the manufacturer's website
- **ContactInfo**: Optional string (up to 200 characters), stores contact information
- **IsActive**: Boolean flag indicating if the manufacturer is active (default: true)
- **LocationId**: Required integer, foreign key to the manufacturer's physical location
- **CreatedAt**: Timestamp of creation (required)
- **UpdatedAt**: Timestamp of last modification (optional)

**Section sources**
- [Manufacturer.cs](file://src/Inventory.API/Models/Manufacturer.cs#L2-L19)

## Data Relationships

### Category Relationships
The Category entity maintains two primary relationships:

1. **Self-Referencing Hierarchy**: A category can have a parent category and multiple subcategories, enabling tree-like organization.
2. **One-to-Many with Product**: Each category can be associated with multiple products, while each product belongs to exactly one category.

```mermaid
erDiagram
CATEGORY {
int Id PK
string Name
string? Description
bool IsActive
int? ParentCategoryId FK
datetime CreatedAt
datetime? UpdatedAt
}
PRODUCT {
int Id PK
string Name
string SKU
int CategoryId FK
int ManufacturerId FK
datetime CreatedAt
datetime? UpdatedAt
}
CATEGORY ||--o{ CATEGORY : "parent/subcategory"
CATEGORY ||--o{ PRODUCT : "category/products"
MANUFACTURER ||--o{ PRODUCT : "manufacturer/products"
```

**Diagram sources**
- [Category.cs](file://src/Inventory.API/Models/Category.cs#L2-L14)
- [Product.cs](file://src/Inventory.API/Models/Product.cs#L4-L35)

### Manufacturer Relationships
The Manufacturer entity maintains relationships with:

1. **One-to-Many with Product**: Each manufacturer can produce multiple products, while each product is made by exactly one manufacturer.
2. **One-to-Many with ProductModel**: Manufacturers can have multiple product models.
3. **Many-to-One with Location**: Each manufacturer is associated with a physical location.

**Section sources**
- [Manufacturer.cs](file://src/Inventory.API/Models/Manufacturer.cs#L2-L19)
- [Product.cs](file://src/Inventory.API/Models/Product.cs#L4-L35)

## Validation Rules

### Category Validation
The system enforces the following validation rules for categories:

- **Name**: Required, 2-100 characters
- **Description**: Maximum 500 characters
- **ParentCategoryId**: If specified, must be greater than 0
- **Uniqueness**: Category names must be unique within the system

These rules are enforced through FluentValidation in both creation and update operations.

### Manufacturer Validation
The system enforces the following validation rules for manufacturers:

- **Name**: Required, 2-100 characters
- **Description**: Maximum 500 characters
- **ContactInfo**: Maximum 200 characters
- **Website**: Must be a valid HTTP or HTTPS URL if provided
- **Uniqueness**: Manufacturer names must be unique within the system

These validation rules ensure data integrity and consistency across the inventory system.

**Section sources**
- [CreateCategoryDtoValidator.cs](file://src/Inventory.API/Validators/CreateCategoryDtoValidator.cs#L1-L30)
- [UpdateCategoryDtoValidator.cs](file://src/Inventory.API/Validators/UpdateCategoryDtoValidator.cs#L1-L30)
- [CreateManufacturerDtoValidator.cs](file://src/Inventory.API/Validators/CreateManufacturerDtoValidator.cs#L1-L43)
- [UpdateManufacturerDtoValidator.cs](file://src/Inventory.API/Validators/UpdateManufacturerDtoValidator.cs#L1-L43)

## Usage in Product Management

### Product Categorization
Categories are used to organize products into logical groups, enabling:
- Hierarchical browsing of inventory
- Targeted filtering and searching
- Reporting by category and subcategory
- Stock level monitoring by category

### Filtering and Reporting
Both Category and Manufacturer entities serve as primary dimensions for:
- Product filtering in the user interface
- Inventory reports and analytics
- Stock level alerts by category or manufacturer
- Purchase planning based on manufacturer performance

The system leverages these reference data entities to provide meaningful insights and efficient navigation through large inventory datasets.

**Section sources**
- [Product.cs](file://src/Inventory.API/Models/Product.cs#L4-L35)
- [Category.cs](file://src/Inventory.API/Models/Category.cs#L2-L14)
- [Manufacturer.cs](file://src/Inventory.API/Models/Manufacturer.cs#L2-L19)

## Query Examples

### Hierarchical Category Queries
To retrieve a category with its subcategories:
```sql
SELECT c.Id, c.Name, c.Description, p.Name as ParentCategoryName
FROM Categories c
LEFT JOIN Categories p ON c.ParentCategoryId = p.Id
WHERE c.IsActive = true
ORDER BY p.Name, c.Name;
```

To find all products in a category and its subcategories:
```sql
WITH RECURSIVE CategoryTree AS (
    SELECT Id FROM Categories WHERE Id = @CategoryId
    UNION
    SELECT c.Id FROM Categories c
    INNER JOIN CategoryTree ct ON c.ParentCategoryId = ct.Id
)
SELECT p.* FROM Products p
INNER JOIN CategoryTree ct ON p.CategoryId = ct.Id;
```

### Manufacturer-Based Product Searches
To find all products by a specific manufacturer:
```sql
SELECT p.Id, p.Name, p.SKU, p.CurrentQuantity
FROM Products p
INNER JOIN Manufacturers m ON p.ManufacturerId = m.Id
WHERE m.Name LIKE '%@SearchTerm%'
AND p.IsActive = true;
```

**Section sources**
- [20250928092414_InitialCreate.cs](file://src/Inventory.API/Migrations/20250928092414_InitialCreate.cs#L654-L678)

## Performance Considerations

### Indexing Strategy
The system implements critical performance indexes on:
- **Category.Name**: Ensures fast lookups and filtering by category name
- **Manufacturer.Name**: Enables efficient manufacturer-based searches
- **Product.CategoryId and Product.ManufacturerId**: Optimizes joins and filtering operations

These indexes are defined in database migrations to ensure optimal query performance.

### Caching Strategies
Reference data entities (Category and Manufacturer) are ideal candidates for caching due to:
- Relatively low update frequency
- High read frequency in product operations
- Use in dropdowns and filtering interfaces

Recommended caching approach:
- Implement server-side caching with expiration policies
- Cache individual entities by ID
- Cache category hierarchies to avoid recursive database queries
- Invalidate cache on create, update, or delete operations

**Section sources**
- [20250928113056_AddCriticalPerformanceIndexes.Designer.cs](file://src/Inventory.API/Migrations/20250928113056_AddCriticalPerformanceIndexes.Designer.cs#L271-L309)
- [20250928113056_AddCriticalPerformanceIndexes.Designer.cs](file://src/Inventory.API/Migrations/20250928113056_AddCriticalPerformanceIndexes.Designer.cs#L1491-L1523)