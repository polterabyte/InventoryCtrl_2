
# Frontend Architecture

<cite>
**Referenced Files in This Document**   
- [WebApiServiceBase.cs](file://src/Inventory.Web.Client/Services/WebApiServiceBase.cs)
- [ResilientApiService.cs](file://src/Inventory.Web.Client/Services/ResilientApiService.cs)
- [InterceptedHttpClient.cs](file://src/Inventory.Web.Client/Services/InterceptedHttpClient.cs)
- [CustomAuthenticationStateProvider.cs](file://src/Inventory.Web.Client/CustomAuthenticationStateProvider.cs)
- [RequestValidator.cs](file://src/Inventory.Web.Client/Services/RequestValidator.cs)
- [UserManagementService.cs](file://src/Inventory.Web.Client/Services/UserManagementService.cs)
- [NotificationApiService.cs](file://src/Inventory.Shared/Services/NotificationApiService.cs)
- [LoggingService.cs](file://src/Inventory.Shared/Services/LoggingService.cs)
- [WebBaseApiService.cs](file://src/Inventory.Web.Client/Services/WebBaseApiService.cs)
- [ApiErrorHandler.cs](file://src/Inventory.Web.Client/Services/ApiErrorHandler.cs)
- [TokenManagementService.cs](file://src/Inventory.Web.Client/Services/TokenManagementService.cs)
- [TokenRefreshService.cs](file://src/Inventory.Web.Client/Services/TokenRefreshService.cs)
- [JwtHttpInterceptor.cs](file://src/Inventory.Web.Client/Services/JwtHttpInterceptor.cs)
- [Program.cs](file://src/Inventory.Web.Client/Program.cs)
- [MainLayout.razor.css](file://src/Inventory.Web.Client/Layout/MainLayout.razor.css)
- [NavMenu.razor.css](file://src/Inventory.Web.Client/Layout/NavMenu.razor.css)
- [app.css](file://src/Inventory.Web.Assets/wwwroot/css/app.css)
- [light.css](file://src/Inventory.Web.Assets/wwwroot/css/themes/light.css)
- [dark.css](file://src/Inventory.Web.Assets/wwwroot/css/themes/dark.css)
</cite>

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
This document provides comprehensive architectural documentation for the frontend implementation of InventoryCtrl_2, a Blazor WebAssembly application. The system follows a component-based UI design with robust integration to backend services through API clients. The architecture emphasizes resilience, security, and maintainability through layered service abstractions, centralized error handling, and JWT-based authentication. The frontend leverages Blazor's component model for UI development, implements comprehensive state management through service classes, and uses a modular approach to HTTP communication with automatic token refresh and retry mechanisms.

## Project Structure
The frontend architecture is organized into several key directories within the Inventory.Web.Client project. The Services directory contains API clients and utility services, while Layout components define the application's structural UI elements. The application uses a separate Web.Assets project for shared CSS resources and themes. The Program.cs file configures dependency injection and service registration, establishing the foundation for the application's modular architecture.

```mermaid
graph TB
subgraph "Inventory.Web.Client"
A[Services] --> B[API Clients]
A --> C[Utilities]
A --> D[Interceptors]
E[Layout] --> F[MainLayout]
E --> G[NavMenu]
H[Program.cs] --> I[Service Registration]
end
subgraph "Inventory.Web.Assets"
J[wwwroot/css] --> K[Themes]
J --> L[Components]
J --> M[Application]
end
I --> A
I --> E
J --> A
```

**Diagram sources**
- [Program.cs](file://src/Inventory.Web.Client/Program.cs)
- [MainLayout.razor.css](file://src/Inventory.Web.Client/Layout/MainLayout.razor.css)
- [app.css](file://src/Inventory.Web.Assets/wwwroot/css/app.css)

**Section sources**
- [Program.cs](file://src/Inventory.Web.Client/Program.cs)
- [MainLayout.razor.css](file://src/Inventory.Web.Client/Layout/MainLayout.razor.css)
- [app.css](file://src/Inventory.Web.Assets/wwwroot/css/app.css)

## Core Components
The frontend architecture is built around several core components that provide the foundation for API communication, authentication, and state management. The WebApiServiceBase class implements a generic CRUD pattern for entity operations, while ResilientApiService provides retry mechanisms for unreliable network conditions. The InterceptedHttpClient enables request interception for authentication header management, and CustomAuthenticationStateProvider handles user authentication state across the application. RequestValidator ensures data integrity through validation rules, and specialized services like UserManagementService and NotificationApiService provide domain-specific functionality.

**Section sources**
- [WebApiServiceBase.cs](file://src/Inventory.Web.Client/Services/WebApiServiceBase.cs)
- [ResilientApiService.cs](file://src/Inventory.Web.Client/Services/ResilientApiService.cs)
- [InterceptedHttpClient.cs](file://src/Inventory.Web.Client/Services/InterceptedHttpClient.cs)
- [CustomAuthenticationStateProvider.cs](file://src/Inventory.Web.Client/CustomAuthenticationStateProvider.cs)
- [RequestValidator.cs](file://src/Inventory.Web.Client/Services/RequestValidator.cs)

## Architecture Overview
The frontend architecture follows a layered approach with clear separation of concerns. At the foundation is the Blazor WebAssembly runtime, which hosts the application and provides component rendering. Above this layer, service classes handle business logic and API communication, while components manage UI presentation and user interaction. The architecture implements a dependency injection pattern for service resolution, with services registered in Program.cs. HTTP requests flow through a chain of responsibility that includes validation, retry logic, authentication header injection, and error handling. The state management approach combines Blazor's built-in authentication state with custom services that maintain application-specific state.

```mermaid
graph TD
A[Blazor WebAssembly Runtime] --> B[UI Components]
A --> C[Authentication State]
B --> D[Service Layer]
D --> E[API Communication]
E --> F[ResilientApiService]
E --> G[InterceptedHttpClient]
G --> H[JwtHttpInterceptor]
F --> I[ApiHealthService]
D --> J[State Management Services]
J --> K[UserManagementService]
J --> L[NotificationApiService]
D --> M[Validation Service]
M --> N[RequestValidator]
C --> O[CustomAuthenticationStateProvider]
O --> P[TokenManagementService]
P --> Q[TokenRefreshService]
```

**Diagram sources**
- [WebApiServiceBase.cs](file://src/Inventory.Web.Client/Services/WebApiServiceBase.cs)
- [ResilientApiService.cs](file://src/Inventory.Web.Client/Services/ResilientApiService.cs)
- [InterceptedHttpClient.cs](file://src/Inventory.Web.Client/Services/InterceptedHttpClient.cs)
- [CustomAuthenticationStateProvider.cs](file://src/Inventory.Web.Client/CustomAuthenticationStateProvider.cs)
- [RequestValidator.cs](file://src/Inventory.Web.Client/Services/RequestValidator.cs)
- [UserManagementService.cs](file://src/Inventory.Web.Client/Services/UserManagementService.cs)
- [NotificationApiService.cs](file://src/Inventory.Shared/Services/NotificationApiService.cs)
- [TokenManagementService.cs](file://src/Inventory.Web.Client/Services/TokenManagementService.cs)
- [TokenRefreshService.cs](file://src/Inventory.Web.Client/Services/TokenRefreshService.cs)

## Detailed Component Analysis

### WebApiServiceBase Analysis
The WebApiServiceBase class provides a generic implementation of CRUD operations for entity types, serving as a base for specific API service implementations. It follows the repository pattern, abstracting HTTP communication details from consuming components. The class uses type parameters to ensure type safety across operations and implements common functionality like pagination and error handling. Derived classes only need to specify the base endpoint and can immediately use all CRUD methods.

```mermaid
classDiagram
class WebApiServiceBase~TEntity, TCreateDto, TUpdateDto~ {
+HttpClient httpClient
+IUrlBuilderService urlBuilderService
+IResilientApiService resilientApiService
+IApiErrorHandler errorHandler
+IRequestValidator requestValidator
+ILogger logger
+abstract string BaseEndpoint
+Task~TEntity[]~ GetAllAsync()
+Task~TEntity?~ GetByIdAsync(int id)
+Task~TEntity~ CreateAsync(TCreateDto createDto)
+Task~TEntity?~ UpdateAsync(int id, TUpdateDto updateDto)
+Task~bool~ DeleteAsync(int id)
+Task~PagedApiResponse~TEntity~~ GetPagedAsync(int page, int pageSize, string? search)
}
class WebBaseApiService {
+HttpClient HttpClient
+IUrlBuilderService UrlBuilderService
+IResilientApiService ResilientApiService
+IApiErrorHandler ErrorHandler
+IRequestValidator RequestValidator
+ILogger Logger
+Task~string~ GetApiUrlAsync()
+Task~ValidationResult~ ValidateRequestAsync~T~(T request)
+Task~T~ ExecuteHttpRequestAsync~T~(HttpMethod method, string endpoint, object? data, Func~HttpResponseMessage, Task~T~~? customResponseHandler)
+Task~string~ BuildFullUrlAsync(string endpoint)
+Task~T~ HandleStandardResponseAsync~T~(HttpResponseMessage response)
+Task~ApiResponse~T~~ GetAsync~T~(string endpoint)
+Task~PagedApiResponse~T~~ GetPagedAsync~T~(string endpoint)
+Task~ApiResponse~T~~ PostAsync~T~(string endpoint, object data)
+Task~ApiResponse~T~~ PutAsync~T~(string endpoint, object data)
+Task~ApiResponse~bool~~ DeleteAsync(string endpoint)
}
WebApiServiceBase <|-- WebBaseApiService : inherits
WebApiServiceBase <|-- WebCategoryApiService : implements
WebApiServiceBase <|-- WebManufacturerApiService : implements
WebApiServiceBase <|-- WebProductApiService : implements
WebApiServiceBase <|-- WebWarehouseApiService : implements
```

**Diagram sources**
- [WebApiServiceBase.cs](file://src/Inventory.Web.Client/Services/WebApiServiceBase.cs)
- [WebBaseApiService.cs](file://src/Inventory.Web.Client/Services/WebBaseApiService.cs)

**Section sources**
- [WebApiServiceBase.cs](file://src/Inventory.Web.Client/Services/WebApiServiceBase.cs)
- [WebBaseApiService.cs](file://src/Inventory.Web.Client/Services/WebBaseApiService.cs)

### ResilientApiService Analysis
The ResilientApiService implements a retry pattern with exponential backoff and jitter to handle transient failures in API communication. It integrates with the ApiHealthService to check API availability before making requests, preventing unnecessary calls during service outages. The service uses configurable retry settings, allowing adjustment of maximum retries, base delay, and maximum delay. The implementation includes logging at various levels to aid in troubleshooting and monitoring of retry behavior.

```mermaid
flowchart TD
Start([ExecuteWithRetryAsync]) --> CheckAttempt{"attempt <= MaxRetries?"}
CheckAttempt --> |No| ThrowError["Throw InvalidOperationException"]
CheckAttempt --> |Yes| CheckApiAvailability["Check API Availability"]
CheckApiAvailability --> IsApiAvailable{"API Available?"}
IsApiAvailable --> |No| ThrowUnavailable["Throw InvalidOperationException"]
IsApiAvailable --> |Yes| ExecuteOperation["Execute Operation"]
ExecuteOperation --> OperationSuccess{"Operation Successful?"}
OperationSuccess --> |Yes| ReturnResult["Return Result"]
OperationSuccess --> |No| CalculateDelay["Calculate Delay with Exponential Backoff and Jitter"]
CalculateDelay --> Wait["Wait Delay Period"]
Wait --> IncrementAttempt["attempt++"]
IncrementAttempt --> CheckAttempt
style Start fill:#f9f,stroke:#333
style ThrowError fill:#f96,stroke:#333
style ReturnResult fill:#9f9,stroke:#333
```

**Diagram sources**
- [ResilientApiService.cs](file://src/Inventory.Web.Client/Services/ResilientApiService.cs)

**Section sources**
- [ResilientApiService.cs](file://src/Inventory.Web.Client/Services/ResilientApiService.cs)

### InterceptedHttpClient Analysis
The InterceptedHttpClient extends the standard HttpClient to support request interception through the IHttpInterceptor interface. This allows cross-cutting concerns like authentication header injection to be implemented without modifying individual service calls. The implementation uses a functional approach, passing the next delegate to allow chain-of-responsibility pattern implementation. This design enables multiple interceptors to be composed together, though the current implementation uses a single JwtHttpInterceptor for authentication purposes.

```mermaid
sequenceDiagram
participant Component as "UI Component"
participant Service as "API Service"
participant Client as "InterceptedHttpClient"
participant Interceptor as "JwtHttpInterceptor"
participant BaseClient as "HttpClient"
Component->>Service : Call API Method
Service->>Client : SendAsync(request)
Client->>Interceptor : InterceptAsync(request, next)
Interceptor->>BaseClient : next() - Execute Request
BaseClient-->>Interceptor : HttpResponseMessage
Interceptor-->>Client : HttpResponseMessage
Client-->>Service : HttpResponseMessage
Service-->>Component : Process Response
```

**Diagram sources**
- [InterceptedHttpClient.cs](file://src/Inventory.Web.Client/Services/InterceptedHttpClient.cs)
- [JwtHttpInterceptor.cs](file://src/Inventory.Web.Client/Services/JwtHttpInterceptor.cs)

**Section sources**
- [InterceptedHttpClient.cs](file://src/Inventory.Web.Client/Services/InterceptedHttpClient.cs)
- [JwtHttpInterceptor.cs](file://src/Inventory.Web.Client/Services/JwtHttpInterceptor.cs)

### Authentication State Management
The authentication architecture centers around the CustomAuthenticationStateProvider, which extends Blazor's built-in AuthenticationStateProvider. It integrates with TokenManagementService to handle JWT token storage, refresh, and expiration checking. The implementation automatically checks for token expiration before each authentication state request and attempts silent refresh when necessary. This ensures users maintain their session without manual re-authentication while maintaining security through timely token renewal.

```mermaid
sequenceDiagram
participant Component as "UI Component"
participant AuthProvider as "CustomAuthenticationStateProvider"
participant TokenService as "TokenManagementService"
participant RefreshService as "TokenRefreshService"
participant HttpClient as "HttpClient"
Component->>AuthProvider : GetAuthenticationStateAsync()
AuthProvider->>TokenService : GetStoredTokenAsync()
TokenService-->>AuthProvider : token
AuthProvider->>TokenService : IsTokenExpiringSoonAsync()
TokenService-->>AuthProvider : true
AuthProvider->>TokenService : TryRefreshTokenAsync()
TokenService->>RefreshService : RefreshTokenAsync(refreshToken)
RefreshService-->>TokenService : new token
TokenService->>HttpClient : Set Authorization Header
TokenService-->>AuthProvider : success
AuthProvider->>HttpClient : Set Authorization Header
AuthProvider-->>Component : AuthenticationState
```

**Diagram sources**
- [CustomAuthenticationStateProvider.cs](file://src/Inventory.Web.Client/CustomAuthenticationStateProvider.cs)
- [TokenManagementService.cs](file://src/Inventory.Web.Client/Services/TokenManagementService.cs)
- [TokenRefreshService.cs](file://src/Inventory.Web.Client/Services/TokenRefreshService.cs)

**Section sources**
- [CustomAuthenticationStateProvider.cs](file://src/Inventory.Web.Client/CustomAuthenticationStateProvider.cs)
- [TokenManagementService.cs](file://src/Inventory.Web.Client/Services/TokenManagementService.cs)
- [TokenRefreshService.cs](file://src/Inventory.Web.Client/Services/TokenRefreshService.cs)

### Request Validation Analysis
The RequestValidator service provides a flexible validation framework that supports both DataAnnotations and custom validation logic. It maintains a dictionary of registered validators for different request types, allowing domain-specific validation rules to be injected. The implementation includes comprehensive logging to track validation attempts and results. For types without custom validators, it falls back to reflection-based validation using DataAnnotations attributes, providing a consistent validation experience across the application.

```mermaid
flowchart TD
Start([ValidateAsync]) --> NullCheck{"Request Null?"}
NullCheck --> |Yes| ReturnInvalid["Return Invalid Result"]
NullCheck --> |No| LogDebug["Log Debug Message"]
LogDebug --> CheckCustomValidator["Check for Custom Validator"]
CheckCustomValidator --> HasCustom{"Has Custom Validator?"}
HasCustom --> |Yes| ExecuteCustom["Execute Custom Validator"]
HasCustom --> |No| ExecuteDataAnnotations["Execute DataAnnotations Validation"]
ExecuteCustom --> LogResult["Log Validation Result"]
ExecuteDataAnnotations --> LogResult
LogResult --> ReturnResult["Return ValidationResult"]
style Start fill:#f9f,stroke:#333
style ReturnInvalid fill:#f96,stroke:#333
style ReturnResult fill:#9f9,stroke:#333
```

**Diagram sources**
- [RequestValidator.cs](file://src/Inventory.Web.Client/Services/RequestValidator.cs)

**Section sources**
- [RequestValidator.cs](file://src/Inventory.Web.Client/Services/RequestValidator.cs)

### State Management Services
State management in the application is handled through specialized service classes that encapsulate domain-specific state and business logic. The UserManagementService provides CRUD operations for user entities with additional functionality like export capabilities. The NotificationApiService handles notification-related operations with methods for retrieval, marking as read, and preference management. These services follow a consistent pattern of using HttpClient for communication, with error handling and logging integrated throughout.

```mermaid
classDiagram
    class UserManagementService {
        +HttpClient _httpClient
        +IAuthenticationService _authService
        +IJSRuntime _jsRuntime
        +IUrlBuilderService _urlBuilderService
        +ILogger<UserManagementService> _logger
        +Task~PagedApiResponse~UserDto~~ GetUsersAsync(int page, int pageSize, string? search, string? role)
        +Task~ApiResponse~UserDto~~ GetUserAsync(string id)
        +Task~ApiResponse~UserDto