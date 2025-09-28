
# API Clients

<cite>
**Referenced Files in This Document**   
- [WebApiServiceBase.cs](file://src/Inventory.Web.Client/Services/WebApiServiceBase.cs)
- [ResilientApiService.cs](file://src/Inventory.Web.Client/Services/ResilientApiService.cs)
- [InterceptedHttpClient.cs](file://src/Inventory.Web.Client/Services/InterceptedHttpClient.cs)
- [JwtHttpInterceptor.cs](file://src/Inventory.Web.Client/Services/JwtHttpInterceptor.cs)
- [ApiErrorHandler.cs](file://src/Inventory.Web.Client/Services/ApiErrorHandler.cs)
- [WebManufacturerApiService.cs](file://src/Inventory.Web.Client/Services/WebManufacturerApiService.cs)
- [WebProductApiService.cs](file://src/Inventory.Web.Client/Services/WebProductApiService.cs)
- [Program.cs](file://src/Inventory.Web.Client/Program.cs)
</cite>

## Table of Contents
1. [Introduction](#introduction)
2. [Core Architecture Overview](#core-architecture-overview)
3. [WebApiServiceBase: Generic Entity Service Implementation](#webapiservicebase-generic-entity-service-implementation)
4. [ResilientApiService: Fault Tolerance and Retry Logic](#resilientapiservice-fault-tolerance-and-retry-logic)
5. [InterceptedHttpClient and JwtHttpInterceptor: Authentication Management](#interceptedhttpclient-and-jwthttpinterceptor-authentication-management)
6. [ApiErrorHandler: Centralized Error Handling](#apierrorhandler-centralized-error-handling)
7. [Service Implementation Examples](#service-implementation-examples)
8. [Configuration and Dependency Injection](#configuration-and-dependency-injection)
9. [Performance and Debugging Considerations](#performance-and-debugging-considerations)
10. [Conclusion](#conclusion)

## Introduction
The API client architecture in InventoryCtrl_2 provides a robust, type-safe, and resilient communication layer between the Blazor WebAssembly frontend and the backend API. This documentation details the implementation of key components that enable efficient, secure, and fault-tolerant API interactions. The architecture emphasizes code reuse through generic base classes, implements comprehensive error handling and retry mechanisms, and ensures secure authentication through JWT token management. The design supports CRUD operations, pagination, and type safety across all entity services while providing extensibility for custom business logic.

## Core Architecture Overview

```mermaid
graph TD
A[WebApiServiceBase<TEntity, TCreateDto, TUpdateDto>] --> B[WebBaseApiService]
B --> C[IResilientApiService]
B --> D[IApiErrorHandler]
B --> E[IRequestValidator]
B --> F[IUrlBuilderService]
C --> G[ApiHealthService]
C --> H[RetryConfig]
D --> I[TokenManagementService]
D --> J[UINotificationService]
K[InterceptedHttpClient] --> L[JwtHttpInterceptor]
L --> M[TokenManagementService]
N[HttpClient] --> K
O[WebManufacturerApiService] --> A
P[WebProductApiService] --> B
Q[WebCategoryApiService] --> A
style A fill:#f9f,stroke:#333
style K fill:#f9f,stroke:#333
style C fill:#f9f,stroke:#333
style D fill:#f9f,stroke:#333
```

**Diagram sources**
- [WebApiServiceBase.cs](file://src/Inventory.Web.Client/Services/WebApiServiceBase.cs)
- [ResilientApiService.cs](file://src/Inventory.Web.Client/Services/ResilientApiService.cs)
- [InterceptedHttpClient.cs](file://src/Inventory.Web.Client/Services/InterceptedHttpClient.cs)
- [JwtHttpInterceptor.cs](file://src/Inventory.Web.Client/Services/JwtHttpInterceptor.cs)
- [ApiErrorHandler.cs](file://src/Inventory.Web.Client/Services/ApiErrorHandler.cs)

**Section sources**
- [WebApiServiceBase.cs](file://src/Inventory.Web.Client/Services/WebApiServiceBase.cs)
- [ResilientApiService.cs](file://src/Inventory.Web.Client/Services/ResilientApiService.cs)
- [InterceptedHttpClient.cs](file://src/Inventory.Web.Client/Services/InterceptedHttpClient.cs)
- [JwtHttpInterceptor.cs](file://src/Inventory.Web.Client/Services/JwtHttpInterceptor.cs)
- [ApiErrorHandler.cs](file://src/Inventory.Web.Client/Services/ApiErrorHandler.cs)

## WebApiServiceBase: Generic Entity Service Implementation

The `WebApiServiceBase` class serves as a generic foundation for all entity-specific API services, providing standardized CRUD operations and pagination functionality. This implementation ensures type safety through generic parameters and promotes code reuse across different entity types.

```mermaid
classDiagram
class WebApiServiceBase~TEntity, TCreateDto, TUpdateDto~ {
+HttpClient HttpClient
+IUrlBuilderService UrlBuilderService
+IResilientApiService ResilientApiService
+IApiErrorHandler ErrorHandler
+IRequestValidator RequestValidator
+ILogger Logger
+abstract string BaseEndpoint
+Task~TEntity[]~ GetAllAsync()
+Task~TEntity?~ GetByIdAsync(int id)
+Task~TEntity~ CreateAsync(TCreateDto createDto)
+Task~TEntity?~ UpdateAsync(int id, TUpdateDto updateDto)
+Task~bool~ DeleteAsync(int id)
+Task~PagedApiResponse~TEntity~~ GetPagedAsync(int page, int pageSize, string? search)
+Task~PagedApiResponse~TEntity~~ GetPagedAsync(string endpoint)
}
class WebBaseApiService {
+HttpClient HttpClient
+IUrlBuilderService UrlBuilderService
+IResilientApiService ResilientApiService
+IApiErrorHandler ErrorHandler
+IRequestValidator RequestValidator
+ILogger Logger
+Task~ApiResponse~T~~ GetAsync~T~(string endpoint)
+Task~ApiResponse~T~~ PostAsync~T~(string endpoint, object data)
+Task~ApiResponse~T~~ PutAsync~T~(string endpoint, object data)
+Task~ApiResponse~bool~~ DeleteAsync(string endpoint)
+Task~PagedApiResponse~T~~ GetPagedAsync~T~(string endpoint)
}
WebApiServiceBase --|> WebBaseApiService
WebApiServiceBase <|-- WebManufacturerApiService
WebApiServiceBase <|-- WebCategoryApiService
WebApiServiceBase <|-- WebProductGroupApiService
WebApiServiceBase <|-- WebProductModelApiService
WebApiServiceBase <|-- WebUnitOfMeasureApiService
WebApiServiceBase <|-- WebWarehouseApiService
```

**Diagram sources**
- [WebApiServiceBase.cs](file://src/Inventory.Web.Client/Services/WebApiServiceBase.cs)

**Section sources**
- [WebApiServiceBase.cs](file://src/Inventory.Web.Client/Services/WebApiServiceBase.cs)

## ResilientApiService: Fault Tolerance and Retry Logic

The `ResilientApiService` implements a comprehensive fault tolerance strategy with exponential backoff and jitter for retry operations. It integrates API health checks before execution and provides configurable retry parameters through the application configuration system.

```mermaid
sequenceDiagram
participant Client
participant ResilientApiService
participant HealthService
participant API
Client->>ResilientApiService : ExecuteWithRetryAsync(operation)
ResilientApiService->>HealthService : IsApiAvailableAsync()
HealthService-->>ResilientApiService : Availability status
alt API Available
ResilientApiService->>API : Execute operation
API-->>ResilientApiService : Response
ResilientApiService-->>Client : Operation result
else API Unavailable or Failure
ResilientApiService->>ResilientApiService : CalculateDelay(attempt)
ResilientApiService->>ResilientApiService : Task.Delay(delay)
ResilientApiService->>API : Retry operation
alt Success after retry
ResilientApiService-->>Client : Operation result
else Max retries exceeded
ResilientApiService-->>Client : InvalidOperationException
end
end
Note over ResilientApiService : Exponential backoff with jitter : <br/>delay = baseDelay * 2^(attempt-1) * (1 ± 0.25)
```

**Diagram sources**
- [ResilientApiService.cs](file://src/Inventory.Web.Client/Services/ResilientApiService.cs)

**Section sources**
- [ResilientApiService.cs](file://src/Inventory.Web.Client/Services/ResilientApiService.cs)

## InterceptedHttpClient and JwtHttpInterceptor: Authentication Management

The authentication infrastructure uses a decorator pattern with `InterceptedHttpClient` wrapping the standard `HttpClient` and delegating to `JwtHttpInterceptor` for JWT token injection and request interception. This ensures that all outgoing requests automatically include the appropriate authentication headers.

```mermaid
sequenceDiagram
participant Component
participant HttpClient
participant InterceptedHttpClient
participant JwtHttpInterceptor
participant TokenService
Component->>HttpClient : SendAsync(request)
HttpClient->>InterceptedHttpClient : SendAsync(request)
InterceptedHttpClient->>JwtHttpInterceptor : InterceptAsync(request, next)
JwtHttpInterceptor->>TokenService : GetCurrentToken()
TokenService-->>JwtHttpInterceptor : JWT Token
JwtHttpInterceptor->>HttpClient : Add Authorization header
HttpClient->>HttpClient : base.SendAsync(request)
HttpClient-->>JwtHttpInterceptor : Response
JwtHttpInterceptor-->>InterceptedHttpClient : Response
InterceptedHttpClient-->>HttpClient : Response
HttpClient-->>Component : Response
```

**Diagram sources**
- [InterceptedHttpClient.cs](file://src/Inventory.Web.Client/Services/InterceptedHttpClient.cs)
- [JwtHttpInterceptor.cs](file://src/Inventory.Web.Client/Services/JwtHttpInterceptor.cs)

**Section sources**
- [InterceptedHttpClient.cs](file://src/Inventory.Web.Client/Services/InterceptedHttpClient.cs)
- [JwtHttpInterceptor.cs](file://src/Inventory.Web.Client/Services/JwtHttpInterceptor.cs)

## ApiErrorHandler: Centralized Error Handling

The `ApiErrorHandler` provides centralized error handling for API responses, with specialized processing for different HTTP status codes and exception types. It handles authentication errors by attempting token refresh and manages user notifications through the `UINotificationService`.

```mermaid
flowchart TD
A[HandleResponseAsync] --> B{IsSuccessStatusCode?}
B --> |Yes| C[Deserialize Response Data]
C --> D[Return Success Response]
B --> |No| E[Get Error Message]
E --> F{Status Code}
F --> |Unauthorized| G[HandleUnauthorizedResponse]
F --> |BadRequest| H[Show Warning Notification]
F --> |Forbidden| I[Show Error Notification]
F --> |NotFound| J[Show Warning Notification]
F --> |Conflict| K[Show Warning Notification]
F --> |InternalServerError| L[Show Error Notification]
G --> M{Has Refresh Token?}
M --> |Yes| N[TryRefreshTokenAsync]
N --> O{Refresh Success?}
O --> |Yes| P[Return TOKEN_REFRESHED]
O --> |No| Q[RedirectToLogin]
M --> |No| Q
Q --> R[Clear Tokens]
R --> S[Show Error Notification]
S --> T[Redirect to Login Page]
H --> U[Return Error Response]
I --> U
J --> U
K --> U
L --> U
P --> U
T --> U
```

**Diagram sources**
- [ApiErrorHandler.cs](file://src/Inventory.Web.Client/Services/ApiErrorHandler.cs)

**Section sources**
- [ApiErrorHandler.cs](file://src/Inventory.Web.Client/Services/ApiErrorHandler.cs)

## Service Implementation Examples

### WebManufacturerApiService Implementation
The `WebManufacturerApiService` demonstrates how to extend `WebApiServiceBase` for a specific entity type, implementing the `IManufacturerService` interface while inheriting all CRUD operations.

```mermaid
classDiagram
class WebManufacturerApiService {
+HttpClient HttpClient
+IUrlBuilderService UrlBuilderService
+IResilientApiService ResilientApiService
+IApiErrorHandler ErrorHandler
+IRequestValidator RequestValidator
+ILogger Logger
+string BaseEndpoint
+Task~ManufacturerDto[]~ GetAllManufacturersAsync()
+Task~ManufacturerDto?~ GetManufacturerByIdAsync(int id)
+Task~ManufacturerDto~ CreateManufacturerAsync(CreateManufacturerDto dto)
+Task~ManufacturerDto?~ UpdateManufacturerAsync(int id, UpdateManufacturerDto dto)
+Task~bool~ DeleteManufacturerAsync(int id)
}
WebManufacturerApiService --|> WebApiServiceBase~ManufacturerDto, CreateManufacturerDto, UpdateManufacturerDto~
WebApiServiceBase~ManufacturerDto, CreateManufacturerDto, UpdateManufacturerDto~ --|> WebBaseApiService
WebManufacturerApiService ..|> IManufacturerService
```

**Diagram sources**
- [WebManufacturerApiService.cs](file://src/Inventory.Web.Client/Services/WebManufacturerApiService.cs)

**Section sources**
- [WebManufacturerApiService.cs](file://src/Inventory.Web.Client/Services/WebManufacturerApiService.cs)

### WebProductApiService Implementation
The `WebProductApiService` extends `WebBaseApiService` directly for more complex operations that go beyond standard CRUD, demonstrating custom endpoint handling and specialized business logic.

```mermaid
classDiagram
class WebProductApiService {
+HttpClient HttpClient
+IUrlBuilderService UrlBuilderService
+IResilientApiService ResilientApiService
+IApiErrorHandler ErrorHandler
+IRequestValidator RequestValidator
+ILogger Logger
+Task~ProductDto[]~ GetAllProductsAsync()
+Task~ProductDto?~ GetProductByIdAsync(int id)
+Task~ProductDto?~ GetProductBySkuAsync(string sku)
+Task~ProductDto~ CreateProductAsync(CreateProductDto dto)
+Task~ProductDto?~ UpdateProductAsync(int id, UpdateProductDto dto)
+Task~bool~ DeleteProductAsync(int id)
+Task~bool~ AdjustStockAsync(ProductStockAdjustmentDto dto)
+Task~ProductDto[]~ GetProductsByCategoryAsync(int categoryId)
+Task~ProductDto[]~ GetLowStockProductsAsync()
+Task~ProductDto[]~ SearchProductsAsync(string searchTerm)
}
WebProductApiService --|> WebBaseApiService
WebProductApiService ..|> IProductService
```

**Diagram sources**
- [WebProductApiService.cs](file://src/Inventory.Web.Client/Services/WebProductApiService.cs)

**Section sources**
- [WebProductApiService.cs](file://src/Inventory.Web.Client/Services/WebProductApi