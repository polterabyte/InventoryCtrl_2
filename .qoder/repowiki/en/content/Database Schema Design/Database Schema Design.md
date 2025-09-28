# Database Schema Design

<cite>
**Referenced Files in This Document**   
- [Product.cs](file://src/Inventory.API/Models/Product.cs)
- [InventoryTransaction.cs](file://src/Inventory.API/Models/InventoryTransaction.cs)
- [User.cs](file://src/Inventory.API/Models/User.cs)
- [Warehouse.cs](file://src/Inventory.API/Models/Warehouse.cs)
- [Location.cs](file://src/Inventory.API/Models/Location.cs)
- [Category.cs](file://src/Inventory.API/Models/Category.cs)
- [Manufacturer.cs](file://src/Inventory.API/Models/Manufacturer.cs)
- [AppDbContext.cs](file://src/Inventory.API/Models/AppDbContext.cs)
- [reconcile-quantities.sql](file://scripts/sql/reconcile-quantities.sql)
- [apply-indexes.sql](file://scripts/sql/apply-indexes.sql)
</cite>

## Table of Contents
1. [Introduction](#introduction)
2. [Entity-Relationship Diagram](#entity-relationship-diagram)
3. [Core Entities](#core-entities)
4. [Data Access and Context](#data-access-and-context)
5. [Indexing and Performance](#indexing-and-performance)
6. [Complex Queries and Data Reconciliation](#complex-queries-and-data-reconciliation)
7. [Business Rules and Validation](#business-rules-and-validation)
8. [Conclusion](#conclusion)

## Introduction
This document provides comprehensive documentation of the database schema for InventoryCtrl_2, detailing the core entities, their relationships, constraints, and performance considerations. The system is built using Entity Framework Core with PostgreSQL, featuring a normalized relational model enhanced with database views for performance-critical aggregations. The schema supports inventory tracking, transaction logging, user management, and notification systems.

## Entity-Relationship Diagram

```mermaid
erDiagram
PRODUCT {
int Id PK
string Name
string SKU UK
string? Description
int UnitOfMeasureId FK
bool IsActive
int CategoryId FK
int ManufacturerId FK
int ProductModelId FK
int ProductGroupId FK
int MinStock
int MaxStock
string? Note
datetime CreatedAt
datetime? UpdatedAt
}
INVENTORY_TRANSACTION {
int Id PK
int ProductId FK
int WarehouseId FK
enum Type
int Quantity
datetime Date
string UserId FK
int? LocationId FK
int? RequestId
decimal? UnitPrice
decimal? TotalPrice
string? Description
datetime CreatedAt
datetime? UpdatedAt
}
USER {
string Id PK
string UserName
string Email
string Role
datetime CreatedAt
datetime? UpdatedAt
string? RefreshToken
datetime? RefreshTokenExpiry
}
WAREHOUSE {
int Id PK
string Name
string? Description
int? LocationId FK
string? ContactInfo
bool IsActive
datetime CreatedAt
datetime? UpdatedAt
}
LOCATION {
int Id PK
string Name
string? Description
bool IsActive
int? ParentLocationId FK
datetime CreatedAt
datetime? UpdatedAt
}
CATEGORY {
int Id PK
string Name
string? Description
bool IsActive
int? ParentCategoryId FK
datetime CreatedAt
datetime? UpdatedAt
}
MANUFACTURER {
int Id PK
string Name
string? Description
string? ContactInfo
string? Website
bool IsActive
int LocationId FK
datetime CreatedAt
datetime? UpdatedAt
}
PRODUCT ||--o{ INVENTORY_TRANSACTION : "has"
USER ||--o{ INVENTORY_TRANSACTION : "creates"
WAREHOUSE ||--o{ INVENTORY_TRANSACTION : "holds"
LOCATION ||--o{ INVENTORY_TRANSACTION : "install_location"
LOCATION ||--o{ WAREHOUSE : "warehouse_location"
LOCATION ||--o{ MANUFACTURER : "manufacturer_location"
CATEGORY ||--o{ PRODUCT : "categorizes"
MANUFACTURER ||--o{ PRODUCT : "produces"
PRODUCT ||--o{ PRODUCT_TAG : "tagged_with" via "ProductProductTags"
```

**Diagram sources**
- [Product.cs](file://src/Inventory.API/Models/Product.cs#L4-L35)
- [InventoryTransaction.cs](file://src/Inventory.API/Models/InventoryTransaction.cs#L12-L38)
- [User.cs](file://src/Inventory.API/Models/User.cs#L2-L11)
- [Warehouse.cs](file://src/Inventory.API/Models/Warehouse.cs#L2-L14)
- [Location.cs](file://src/Inventory.API/Models/Location.cs#L2-L14)
- [Category.cs](file://src/Inventory.API/Models/Category.cs#L2-L14)
- [Manufacturer.cs](file://src/Inventory.API/Models/Manufacturer.cs#L2-L19)

## Core Entities

### Product
The Product entity represents inventory items with comprehensive metadata including categorization, manufacturer information, and stock thresholds.

**Properties:**
- `Id`: Primary key (int)
- `Name`: Product name (string, required)
- `SKU`: Stock Keeping Unit (string, required, unique)
- `Description`: Optional description (string)
- `UnitOfMeasureId`: Foreign key to UnitOfMeasure (int)
- `IsActive`: Status flag (bool, default: true)
- `CategoryId`: Foreign key to Category (int)
- `ManufacturerId`: Foreign key to Manufacturer (int)
- `ProductModelId`: Foreign key to ProductModel (int)
- `ProductGroupId`: Foreign key to ProductGroup (int)
- `MinStock`/`MaxStock`: Reorder thresholds (int)
- `CurrentQuantity`: Computed property from vw_product_on_hand view ([NotMapped])

**Business Rules:**
- Direct Quantity field was removed to prevent data duplication
- CurrentQuantity is computed from transaction history via database view
- Products are soft-deleted via IsActive flag

**Section sources**
- [Product.cs](file://src/Inventory.API/Models/Product.cs#L4-L35)

### InventoryTransaction
Represents all inventory movements including income, outcome, installation, and pending transactions.

**Properties:**
- `Id`: Primary key (int)
- `ProductId`: Foreign key to Product (int)
- `WarehouseId`: Foreign key to Warehouse (int)
- `Type`: TransactionType enum (Income, Outcome, Install, Pending)
- `Quantity`: Integer amount (int)
- `Date`: Transaction date/time (DateTime)
- `UserId`: Foreign key to User (string)
- `LocationId`: Optional foreign key to Location (int?)
- `RequestId`: Optional foreign key to Request (int?)
- `UnitPrice`/`TotalPrice`: Financial tracking (decimal?)
- `Description`: Optional notes (string)

**Business Rules:**
- Transactions are immutable once created
- User navigation property marked with [JsonIgnore] to prevent serialization cycles
- Each transaction type serves distinct business processes

**Section sources**
- [InventoryTransaction.cs](file://src/Inventory.API/Models/InventoryTransaction.cs#L12-L38)

### User
Extends ASP.NET Identity User with application-specific properties and relationships.

**Properties:**
- Inherits all IdentityUser properties (Id, UserName, Email, etc.)
- `Role`: Application role (string)
- `CreatedAt`/`UpdatedAt`: Timestamps (DateTime)
- `RefreshToken`/`RefreshTokenExpiry`: JWT refresh token management
- Navigation collections for Transactions and ProductHistories

**Relationships:**
- One-to-many with InventoryTransaction
- One-to-many with ProductHistory
- One-to-many with AuditLog

**Section sources**
- [User.cs](file://src/Inventory.API/Models/User.cs#L2-L11)

### Warehouse
Represents physical storage locations with optional association to geographic locations.

**Properties:**
- `Id`: Primary key (int)
- `Name`: Warehouse name (string, required)
- `Description`: Optional description (string)
- `LocationId`: Optional foreign key to Location (int?)
- `ContactInfo`: Contact details (string)
- `IsActive`: Status flag (bool, default: true)
- Navigation collection for Transactions

**Relationships:**
- One-to-many with InventoryTransaction
- Optional one-to-one with Location

**Section sources**
- [Warehouse.cs](file://src/Inventory.API/Models/Warehouse.cs#L2-L14)

### Location
Hierarchical location structure supporting nested locations (zones, aisles, shelves, bins).

**Properties:**
- `Id`: Primary key (int)
- `Name`: Location name (string, required)
- `Description`: Optional description (string)
- `ParentLocationId`: Self-referential foreign key (int?)
- `IsActive`: Status flag (bool, default: true)
- Navigation collections for SubLocations and InstallTransactions

**Relationships:**
- Self-referential hierarchy (Parent/Child)
- One-to-many with InventoryTransaction (Install type)
- One-to-many with Warehouse
- One-to-many with Manufacturer

**Section sources**
- [Location.cs](file://src/Inventory.API/Models/Location.cs#L2-L14)

### Category
Hierarchical product categorization with self-referential parent-child relationships.

**Properties:**
- `Id`: Primary key (int)
- `Name`: Category name (string, required)
- `Description`: Optional description (string)
- `ParentCategoryId`: Self-referential foreign key (int?)
- `IsActive`: Status flag (bool, default: true)
- Navigation collections for SubCategories and Products

**Relationships:**
- Self-referential hierarchy for nested categories
- One-to-many with Product

**Section sources**
- [Category.cs](file://src/Inventory.API/Models/Category.cs#L2-L14)

### Manufacturer
Represents product manufacturers with contact information and location association.

**Properties:**
- `Id`: Primary key (int)
- `Name`: Manufacturer name (string, required)
- `Description`: Optional description (string)
- `ContactInfo`: Contact details (string)
- `Website`: URL (string)
- `LocationId`: Required foreign key to Location (int)
- Navigation collections for Models and Products

**Relationships:**
- Required one-to-one with Location
- One-to-many with Product
- One-to-many with ProductModel

**Section sources**
- [Manufacturer.cs](file://src/Inventory.API/Models/Manufacturer.cs#L2-L19)

## Data Access and Context

### AppDbContext
The central data access point implementing EF Core DbContext pattern with comprehensive entity configuration.

**Key Features:**
- Inherits from IdentityDbContext for user management
- Configures all entity relationships and constraints in OnModelCreating
- Defines DbSet properties for all entities and views
- Implements keyless entity types for database views

**View Mappings:**
- `ProductPendingView` → vw_product_pending
- `ProductOnHandView` → vw_product_on_hand  
- `ProductInstalledView` → vw_product_installed

**Relationship Configurations:**
- Cascade delete for User → Notification
- SetNull delete for Product → Notification
- Restrict delete for AuditLog → User
- Unique constraint on NotificationPreference (UserId + EventType)

**Section sources**
- [AppDbContext.cs](file://src/Inventory.API/Models/AppDbContext.cs#L9-L204)

## Indexing and Performance

### Critical Indexes
Performance-critical indexes are applied through both EF Core migrations and manual SQL scripts to ensure optimal query performance.

**InventoryTransactions Indexes:**
- `IX_InventoryTransactions_ProductId_Date`: Product movement history
- `IX_InventoryTransactions_Type_Date`: Transaction type filtering
- `IX_InventoryTransactions_WarehouseId_Type`: Warehouse operations
- `IX_InventoryTransactions_UserId_Date`: User activity tracking

**Products Indexes:**
- `IX_Products_SKU`: Unique index on SKU for fast lookup
- `IX_Products_CategoryId_IsActive`: Category filtering with status
- `IX_Products_ManufacturerId_IsActive`: Manufacturer filtering with status

**AuditLogs Indexes:**
- `IX_AuditLogs_EntityName_EntityId`: Entity-specific audit retrieval
- `IX_AuditLogs_UserId_Timestamp`: User activity timeline

**Implementation:**
Indexes are created using idempotent DO $$ BEGIN blocks in apply-indexes.sql to prevent duplication during deployment.

**Section sources**
- [apply-indexes.sql](file://scripts/sql/apply-indexes.sql#L1-L96)

## Complex Queries and Data Reconciliation

### Quantity Reconciliation
The system uses database views to compute current inventory levels, with reconciliation scripts to verify data integrity.

**vw_product_on_hand View:**
- Computes net quantity from transaction history
- Used for CurrentQuantity property via [NotMapped]
- Prevents data duplication and ensures consistency

**Reconciliation Query (reconcile-quantities.sql):**
- Compares Products.Quantity (if present) with computed on-hand
- Identifies discrepancies for data correction
- Orders by delta magnitude for prioritization
- Limits to top 200 discrepancies

**Purpose:**
- Data integrity verification
- Migration validation
- Debugging inventory discrepancies
- Audit trail consistency checking

**Section sources**
- [reconcile-quantities.sql](file://scripts/sql/reconcile-quantities.sql#L1-L20)

## Business Rules and Validation

### Data Integrity Rules
- **Quantity Management**: Current inventory is computed from transactions, not stored directly
- **Soft Deletes**: Entities use IsActive flag instead of physical deletion
- **Hierarchical Structures**: Categories, Locations, and ProductGroups support nested hierarchies
- **Referential Integrity**: Cascading rules configured for dependent entities

### Validation Constraints
- **SKU Uniqueness**: Unique constraint ensures no duplicate SKUs
- **Required Fields**: Name fields required for Products, Categories, etc.
- **Length Limits**: String fields have appropriate maximum lengths
- **Composite Constraints**: NotificationPreference uniqueness on (UserId, EventType)

### Business Logic Implementation
- **Stock Level Monitoring**: MinStock/MaxStock thresholds trigger notifications
- **Transaction Immutability**: Once recorded, transactions cannot be altered
- **Audit Trail**: All entity changes tracked in AuditLog
- **User Role Management**: Role-based access control integrated with Identity

## Conclusion
The InventoryCtrl_2 database schema provides a robust foundation for inventory management with a well-normalized structure, comprehensive relationships, and performance optimizations. The design emphasizes data integrity through computed quantities, proper indexing, and referential constraints. The integration with EF Core enables type-safe data access while allowing for complex queries through database views. The schema supports all required business operations including inventory tracking, transaction logging, user management, and notification systems.