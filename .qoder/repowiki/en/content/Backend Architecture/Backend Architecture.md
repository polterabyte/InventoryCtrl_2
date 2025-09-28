# Backend Architecture

<cite>
**Referenced Files in This Document**   
- [CategoryController.cs](file://src/Inventory.API/Controllers/CategoryController.cs) - *Updated in recent commit*
- [AppDbContext.cs](file://src/Inventory.API/Models/AppDbContext.cs) - *Updated in recent commit*
- [AuditMiddleware.cs](file://src/Inventory.API/Middleware/AuditMiddleware.cs) - *Updated in recent commit*
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs) - *Updated in recent commit*
- [GlobalExceptionMiddleware.cs](file://src/Inventory.API/Middleware/GlobalExceptionMiddleware.cs) - *Updated in recent commit*
- [Program.cs](file://src/Inventory.API/Program.cs) - *Updated in recent commit*
- [AuditService.cs](file://src/Inventory.API/Services/AuditService.cs) - *Updated in recent commit*
- [NotificationService.cs](file://src/Inventory.API/Services/NotificationService.cs) - *Updated in recent commit*
- [CreateCategoryDtoValidator.cs](file://src/Inventory.API/Validators/CreateCategoryDtoValidator.cs) - *Updated in recent commit*
- [IProductService.cs](file://src/Inventory.Shared/Interfaces/IProductService.cs) - *Updated in recent commit*
- [Category.cs](file://src/Inventory.API/Models/Category.cs) - *Updated in recent commit*
- [CategoryDto.cs](file://src/Inventory.Shared/DTOs/CategoryDto.cs) - *Updated in recent commit*
</cite>

## Update Summary
**Changes Made**   
- Updated documentation to reflect comprehensive API documentation and audit functionality enhancements
- Added new sections for API endpoints and audit API documentation
- Refreshed code examples and diagrams to match current implementation
- Updated file references with commit annotations
- Enhanced source tracking system with detailed file references

## Table of Contents
1. [Introduction](#introduction)
2. [Project Structure](#project-structure)
3. [Core Components](#core-components)
4. [Architecture Overview](#architecture-overview)
5. [Detailed Component Analysis](#detailed-component-analysis)
6. [Dependency Analysis](#dependency-analysis)
7. [Performance Considerations](#performance-considerations)
8. [Troubleshooting Guide](#troubleshooting-guide)
9. [Conclusion](#conclusion)

## Introduction
The InventoryCtrl_2 backend implements a robust, scalable architecture using ASP.NET Core with Clean Architecture principles. The system follows a clear separation of concerns between Controllers (HTTP interface), Services (business logic), and AppDbContext (data access). This documentation details the RESTful API design, dependency injection, middleware pipeline, and cross-cutting concerns including authentication, authorization, audit logging, and error handling.

## Project Structure
The project follows a layered architecture with distinct components for API, shared models, services, and UI. The backend resides in the `src/Inventory.API` directory, containing Controllers, Middleware, Models, Services, and Validators. The `src/Inventory.Shared` directory contains DTOs, interfaces, and models shared between frontend and backend. This separation enables clean dependency flow and promotes reusability.

```mermaid
graph TB
subgraph "Frontend"
UI[Inventory.UI]
WebClient[Inventory.Web.Client]
end
subgraph "Backend"
API[Inventory.API]
Shared[Inventory.Shared]
end
UI --> API
WebClient --> API
API --> Shared
```

**Diagram sources**
- [src/Inventory.API](file://src/Inventory.API)
- [src/Inventory.Shared](file://src/Inventory.Shared)

**Section sources**
- [src/Inventory.API](file://src/Inventory.API)
- [src/Inventory.Shared](file://src/Inventory.Shared)

## Core Components
The core components of the system include RESTful API controllers, business services, data access through AppDbContext, and cross-cutting concerns handled by middleware components. The architecture implements Clean Architecture principles with dependency injection, ensuring loose coupling and high cohesion. The repository pattern is implemented via EF Core, and operations follow a CQRS-like separation with distinct DTOs for create, update, and response operations.

**Section sources**
- [CategoryController.cs](file://src/Inventory.API/Controllers/CategoryController.cs)
- [AppDbContext.cs](file://src/Inventory.API/Models/AppDbContext.cs)
- [AuditService.cs](file://src/Inventory.API/Services/AuditService.cs)

## Architecture Overview
The backend architecture follows a layered approach with clear separation between presentation, business logic, and data access layers. The middleware pipeline handles cross-cutting concerns such as authentication, authorization, audit logging, and global exception handling. Dependency injection is used extensively to manage component lifetimes and promote testability.

```mermaid
graph TD
Client[HTTP Client] --> Middleware[Middleware Pipeline]
Middleware --> Controllers[API Controllers]
Controllers --> Services[Business Services]
Services --> Data[AppDbContext]
Data --> Database[(Database)]
subgraph "Middleware"
AM[AuthenticationMiddleware]
AU[Authorization]
AUD[AuditMiddleware]
GEM[GlobalExceptionMiddleware]
end
subgraph "Services"
NS[NotificationService]
AS[AuditService]
RLS[RateLimitingService]
end
subgraph "Data"
ADC[AppDbContext]
EF[Entity Framework Core]
end
Client --> |Request| Middleware
Middleware --> |Authenticated Request| Controllers
Controllers --> |Business Logic| Services
Services --> |Data Operations| Data
Data --> |SQL| Database
```

**Diagram sources**
- [Program.cs](file://src/Inventory.API/Program.cs)
- [AppDbContext.cs](file://src/Inventory.API/Models/AppDbContext.cs)
- [AuditMiddleware.cs](file://src/Inventory.API/Middleware/AuditMiddleware.cs)

## Detailed Component Analysis

### Controller-Service-DbContext Separation
The system implements a clean separation of concerns between Controllers, Services, and AppDbContext. Controllers handle HTTP interface concerns, Services encapsulate business logic, and AppDbContext manages data access. This separation enables maintainability, testability, and scalability.

```mermaid
classDiagram
class CategoryController {
+GetCategories()
+GetCategory()
+CreateCategory()
+UpdateCategory()
+DeleteCategory()
}
class ProductService {
+GetAllProducts()
+GetProductById()
+CreateProduct()
+UpdateProduct()
+DeleteProduct()
+AdjustStock()
}
class AppDbContext {
+DbSet~Category~ Categories
+DbSet~Product~ Products
+DbSet~User~ Users
+DbSet~AuditLog~ AuditLogs
+OnModelCreating()
}
CategoryController --> AppDbContext : "Uses for data access"
ProductService --> AppDbContext : "Uses for data access"
CategoryController --> ProductService : "Delegates business logic"
```

**Diagram sources**
- [CategoryController.cs](file://src/Inventory.API/Controllers/CategoryController.cs)
- [IProductService.cs](file://src/Inventory.Shared/Interfaces/IProductService.cs)
- [AppDbContext.cs](file://src/Inventory.API/Models/AppDbContext.cs)

#### RESTful API Design with ASP.NET Core Controllers
The RESTful API design follows ASP.NET Core conventions with attribute routing, HTTP method attributes, and proper status code responses. Controllers use dependency injection to receive required services and context. The CategoryController demonstrates proper implementation of CRUD operations with appropriate HTTP methods and response codes.

```mermaid
sequenceDiagram
participant Client
participant Controller
participant Service
participant DbContext
participant Database
Client->>Controller : GET /api/category
Controller->>DbContext : Query Categories
DbContext->>Database : SQL Query
Database-->>DbContext : Category Data
DbContext-->>Controller : IQueryable<Category>
Controller->>Controller : Apply filters, pagination
Controller->>Controller : Map to CategoryDto
Controller-->>Client : 200 OK with PagedResponse
Client->>Controller : POST /api/category
Controller->>Controller : Validate ModelState
Controller->>DbContext : Check parent category
DbContext->>Database : Query Parent Category
Database-->>DbContext : Parent Category
DbContext-->>Controller : Parent Category
Controller->>DbContext : Add new Category
DbContext->>Database : INSERT
Database-->>DbContext : Success
DbContext-->>Controller : Success
Controller->>Controller : Log audit
Controller-->>Client : 201 Created
```

**Diagram sources**
- [CategoryController.cs](file://src/Inventory.API/Controllers/CategoryController.cs)
- [AppDbContext.cs](file://src/Inventory.API/Models/AppDbContext.cs)

#### Dependency Injection and Service Registration
The system uses ASP.NET Core's built-in dependency injection container with scoped, transient, and singleton service registrations. The Program.cs file configures services including database context, identity, authentication, CORS, and custom services. This enables loose coupling and promotes testability.

```mermaid
flowchart TD
A[Program.cs] --> B[Service Registration]
B --> C[AddDbContext<AppDbContext>]
B --> D[AddIdentity<User, Role>]
B --> E[AddAuthentication]
B --> F[AddScoped Services]
B --> G[AddTransient Validators]
B --> H[AddSingleton Configuration]
I[Controller] --> J[Request Services]
J --> K[Resolve AppDbContext]
J --> L[Resolve ILogger]
J --> M[Resolve Services]
N[Service] --> O[Request Services]
O --> P[Resolve Dependencies]
style A fill:#f9f,stroke:#333
style I fill:#bbf,stroke:#333
style N fill:#bbf,stroke:#333
```

**Diagram sources**
- [Program.cs](file://src/Inventory.API/Program.cs)
- [CategoryController.cs](file://src/Inventory.API/Controllers/CategoryController.cs)

#### Middleware Pipeline Analysis
The middleware pipeline processes requests in a specific order, handling cross-cutting concerns before reaching the controllers. The pipeline includes global exception handling, authentication, audit logging, CORS, rate limiting, and authorization. Each middleware component follows the pipeline pattern, either passing control to the next component or terminating the request.

```mermaid
flowchart LR
A[Request] --> B[GlobalExceptionMiddleware]
B --> C[AuthenticationMiddleware]
C --> D[AuditMiddleware]
D --> E[CORS]
E --> F[RateLimiter]
F --> G[Authentication]
G --> H[Authorization]
H --> I[Controllers]
I --> J[Response]
style A fill:#f96,stroke:#333
style J fill:#6f9,stroke:#333
```

**Diagram sources**
- [Program.cs](file://src/Inventory.API/Program.cs)
- [GlobalExceptionMiddleware.cs](file://src/Inventory.API/Middleware/GlobalExceptionMiddleware.cs)
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs)
- [AuditMiddleware.cs](file://src/Inventory.API/Middleware/AuditMiddleware.cs)

### Cross-Cutting Concerns Implementation

#### Authentication and Authorization
The system implements JWT-based authentication with role-based authorization. The AuthenticationMiddleware validates JWT tokens and ensures requests are properly authenticated. The [Authorize] attribute on controllers and actions enforces role-based access control, with Admin roles required for write operations.

```mermaid
sequenceDiagram
participant Client
participant AuthMiddleware
participant JwtHandler
participant Controller
Client->>AuthMiddleware : Request with Bearer Token
AuthMiddleware->>JwtHandler : Validate Token
JwtHandler-->>AuthMiddleware : Valid/Invalid
alt Token Valid
AuthMiddleware->>Controller : Pass Request
Controller->>Controller : Check [Authorize] Roles
alt User Has Required Role
Controller-->>Client : Process Request
else User Lacks Role
Controller-->>Client : 403 Forbidden
end
else Token Invalid
AuthMiddleware-->>Client : 401 Unauthorized
end
```

**Section sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs)
- [CategoryController.cs](file://src/Inventory.API/Controllers/CategoryController.cs)

#### Audit Logging with AuditMiddleware
The AuditMiddleware captures detailed information about HTTP requests and responses, including method, URL, status code, duration, user information, and client details. Audit logs are stored in the database through the AuditService, enabling comprehensive tracking of system activity.

```mermaid
sequenceDiagram
participant Client
participant AuditMiddleware
participant AuditService
participant Database
Client->>AuditMiddleware : HTTP Request
AuditMiddleware->>AuditMiddleware : Start Stopwatch
AuditMiddleware->>AuditMiddleware : Capture Request Details
AuditMiddleware->>AuditService : Log Request Start
AuditMiddleware->>AuditMiddleware : Process Request
AuditMiddleware->>AuditService : Log Response Details
AuditService->>Database : Store Audit Log
Database-->>AuditService : Confirmation
AuditService-->>AuditMiddleware : Confirmation
AuditMiddleware-->>Client : HTTP Response
```

**Section sources**
- [AuditMiddleware.cs](file://src/Inventory.API/Middleware/AuditMiddleware.cs)
- [AuditService.cs](file://src/Inventory.API/Services/AuditService.cs)
- [AppDbContext.cs](file://src/Inventory.API/Models/AppDbContext.cs)

#### Global Exception Handling
The GlobalExceptionMiddleware provides centralized error handling, capturing unhandled exceptions and returning consistent error responses. The middleware logs detailed error information, including stack traces, and returns user-friendly messages while maintaining security by not exposing sensitive information.

```mermaid
flowchart TD
A[Request Processing] --> B{Exception?}
B --> |No| C[Normal Response]
B --> |Yes| D[GlobalExceptionMiddleware]
D --> E[Log Detailed Error]
E --> F[Add to Debug Logs]
F --> G[Create User-Friendly Response]
G --> H[Set Status Code]
H --> I[Return JSON Error Response]
I --> J[Client]
style D fill:#f66,stroke:#333
style I fill:#f66,stroke:#333
```

**Section sources**
- [GlobalExceptionMiddleware.cs](file://src/Inventory.API/Middleware/GlobalExceptionMiddleware.cs)

## Dependency Analysis
The system has a well-defined dependency structure with clear boundaries between components. The API layer depends on the Shared layer for DTOs and interfaces, while the Shared layer contains no dependencies on the API layer. Services depend on AppDbContext for data access, and controllers depend on services for business logic. This dependency structure enables loose coupling and promotes testability.

```mermaid
graph LR
A[Controllers] --> B[Services]
B --> C[AppDbContext]
C --> D[Database]
A --> E[Shared]
B --> E
F[Middleware] --> B
F --> C
G[Validators] --> E
style A fill:#69f,stroke:#333
style B fill:#6f9,stroke:#333
style C fill:#9cf,stroke:#333
```

**Section sources**
- [Program.cs](file://src/Inventory.API/Program.cs)
- [AppDbContext.cs](file://src/Inventory.API/Models/AppDbContext.cs)

## Performance Considerations
The system incorporates several performance optimizations including rate limiting, efficient database queries with pagination, and proper indexing. The rate limiting middleware prevents abuse by limiting request frequency based on user roles. Database queries use pagination to avoid retrieving large datasets, and EF Core's Include method ensures related data is loaded efficiently.

**Section sources**
- [Program.cs](file://src/Inventory.API/Program.cs)
- [CategoryController.cs](file://src/Inventory.API/Controllers/CategoryController.cs)

## Troubleshooting Guide
Common issues in the system typically relate to authentication, authorization, or data validation. Authentication issues may stem from invalid JWT tokens or expired sessions. Authorization issues occur when users lack required roles for specific operations. Data validation issues are handled by FluentValidation with detailed error messages returned to clients.

**Section sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs)
- [GlobalExceptionMiddleware.cs](file://src/Inventory.API/Middleware/GlobalExceptionMiddleware.cs)
- [CreateCategoryDtoValidator.cs](file://src/Inventory.API/Validators/CreateCategoryDtoValidator.cs)