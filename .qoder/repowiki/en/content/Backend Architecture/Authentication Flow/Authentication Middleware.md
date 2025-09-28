# Authentication Middleware

<cite>
**Referenced Files in This Document**   
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs)
- [appsettings.json](file://src/Inventory.API/appsettings.json)
- [Program.cs](file://src/Inventory.API/Program.cs)
- [AuthController.cs](file://src/Inventory.API/Controllers/AuthController.cs)
</cite>

## Table of Contents
1. [Introduction](#introduction)
2. [Core Functionality](#core-functionality)
3. [Authentication Flow](#authentication-flow)
4. [Token Validation Process](#token-validation-process)
5. [Configuration Dependencies](#configuration-dependencies)
6. [Request Handling Logic](#request-handling-logic)
7. [Error Handling and Unauthorized Responses](#error-handling-and-unauthorized-responses)
8. [Logging Practices](#logging-practices)
9. [Integration with Application Pipeline](#integration-with-application-pipeline)
10. [Behavior Examples](#behavior-examples)

## Introduction

The `AuthenticationMiddleware` in InventoryCtrl_2 is a custom middleware component responsible for securing API endpoints by validating Bearer JWT tokens. It intercepts HTTP requests to API endpoints and enforces authentication requirements while allowing unauthenticated access to specific public routes and static resources. The middleware implements a comprehensive security strategy that includes token validation, request filtering, and standardized error responses.

**Section sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L7-L166)

## Core Functionality

The `AuthenticationMiddleware` serves as the primary security gatekeeper for the InventoryCtrl_2 application, implementing a multi-layered approach to request authentication. Its core responsibilities include intercepting incoming HTTP requests, determining whether authentication is required, validating JWT tokens when necessary, and either allowing the request to proceed or returning appropriate unauthorized responses.

The middleware operates within the ASP.NET Core request processing pipeline, positioned after global exception handling but before audit logging and authorization. This strategic placement ensures that only authenticated requests reach the application's business logic layers while maintaining consistent error handling across the system.

```mermaid
flowchart TD
Start([HTTP Request]) --> CheckAPIPath["Check if path starts with /api"]
CheckAPIPath --> |No| ServeBlazor["Serve Blazor UI"]
CheckAPIPath --> |Yes| CheckPublicEndpoint["Check if public endpoint"]
CheckPublicEndpoint --> |Yes| Next["Call next middleware"]
CheckPublicEndpoint --> |No| CheckStaticFile["Check if static file"]
CheckStaticFile --> |Yes| Next
CheckStaticFile --> |No| CheckAuthHeader["Check Authorization header"]
CheckAuthHeader --> |Missing| HandleUnauthorized["Return 401 Unauthorized"]
CheckAuthHeader --> |Valid Bearer| ExtractToken["Extract JWT token"]
ExtractToken --> ValidateToken["Validate token signature, issuer, audience, lifetime"]
ValidateToken --> |Invalid| HandleUnauthorized
ValidateToken --> |Valid| Next
HandleUnauthorized --> SendResponse["Send standardized 401 response"]
Next --> End([Request Processing])
SendResponse --> End
```

**Diagram sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L19-L166)

**Section sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L7-L166)

## Authentication Flow

The authentication flow implemented by the middleware follows a sequential decision-making process to determine whether a request should be authenticated. The flow begins by checking if the request targets an API endpoint (paths starting with "/api"). Requests to non-API routes are assumed to be serving the Blazor WebAssembly UI and are allowed to proceed without authentication.

For API requests, the middleware evaluates three conditions in sequence: public endpoint status, static file status, and authentication requirements. This hierarchical approach ensures that public resources and static assets are accessible without authentication while protecting all other API endpoints.

```mermaid
sequenceDiagram
participant Client
participant Middleware
participant NextHandler
Client->>Middleware : HTTP Request
Middleware->>Middleware : Check path starts with /api?
alt Not API route
Middleware->>NextHandler : Proceed without authentication
else Is API route
Middleware->>Middleware : Check public endpoint?
alt Is public endpoint
Middleware->>NextHandler : Proceed without authentication
else Not public endpoint
Middleware->>Middleware : Check static file?
alt Is static file
Middleware->>NextHandler : Proceed without authentication
else Requires authentication
Middleware->>Middleware : Validate Authorization header
alt Missing or invalid header
Middleware->>Client : Return 401 Unauthorized
else Valid Bearer token
Middleware->>Middleware : Validate JWT token
alt Invalid token
Middleware->>Client : Return 401 Unauthorized
else Valid token
Middleware->>NextHandler : Proceed with authentication
end
end
end
end
end
```

**Diagram sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L19-L166)

**Section sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L19-L166)

## Token Validation Process

The token validation process in `AuthenticationMiddleware` leverages the `JwtSecurityTokenHandler` class from Microsoft.IdentityModel.Tokens to validate JWT tokens against configured security parameters. The validation process checks multiple aspects of the token including issuer, audience, lifetime, and cryptographic signature to ensure its authenticity and validity.

The validation parameters are configured dynamically from the application's configuration, allowing for flexible security policies. The middleware validates that the token is issued by the expected issuer ("InventoryServer"), intended for the correct audience ("InventoryClient"), has not expired, and is signed with the expected cryptographic key. A clock skew of 5 minutes is allowed to accommodate minor time differences between servers.

```mermaid
flowchart TD
Start([Token Validation Start]) --> ExtractConfig["Extract JWT configuration from appsettings"]
ExtractConfig --> CheckConfig["Validate configuration exists"]
CheckConfig --> |Missing| LogError["Log configuration error"]
CheckConfig --> |Valid| CreateHandler["Create JwtSecurityTokenHandler"]
CreateHandler --> CreateParameters["Create TokenValidationParameters"]
CreateParameters --> SetIssuer["Set ValidateIssuer = true"]
CreateParameters --> SetAudience["Set ValidateAudience = true"]
CreateParameters --> SetLifetime["Set ValidateLifetime = true"]
CreateParameters --> SetSigningKey["Set ValidateIssuerSigningKey = true"]
CreateParameters --> SetValidIssuer["Set ValidIssuer from config"]
CreateParameters --> SetValidAudience["Set ValidAudience from config"]
CreateParameters --> SetSigningKeyObj["Set IssuerSigningKey from config key"]
CreateParameters --> SetClockSkew["Set ClockSkew = 5 minutes"]
SetIssuer --> ValidateToken["Call tokenHandler.ValidateToken()"]
SetAudience --> ValidateToken
SetLifetime --> ValidateToken
SetSigningKey --> ValidateToken
SetValidIssuer --> ValidateToken
SetValidAudience --> ValidateToken
SetSigningKeyObj --> ValidateToken
SetClockSkew --> ValidateToken
ValidateToken --> |Success| CheckExpiration["Manually verify expiration claim"]
ValidateToken --> |Exception| HandleException["Catch and log exception"]
HandleException --> ReturnFalse["Return false"]
CheckExpiration --> |Not expired| ReturnTrue["Return true"]
CheckExpiration --> |Expired| LogWarning["Log expiration warning"]
LogWarning --> ReturnFalse
ReturnTrue --> End([Token Valid])
ReturnFalse --> End
```

**Diagram sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L130-L150)
- [appsettings.json](file://src/Inventory.API/appsettings.json#L4-L10)

**Section sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L130-L150)

## Configuration Dependencies

The `AuthenticationMiddleware` depends on JWT configuration settings defined in the application's configuration files. These settings are critical for the token validation process and must be properly configured in all environments. The middleware retrieves these settings from the "Jwt" section of the configuration, requiring three essential parameters: Key, Issuer, and Audience.

The configuration also includes additional JWT-related settings such as token expiration time (ExpireMinutes) and refresh token expiration (RefreshTokenExpireDays), which are used by other components in the authentication system. In non-development environments, the JWT key must be provided through environment variables or secure configuration sources rather than being hardcoded in configuration files.

**Configuration Settings**

| Setting | Value | Purpose | Required |
|--------|-------|---------|----------|
| Jwt:Key | `__SET_IN_ENV__` | Cryptographic key for signing and validating tokens | Yes |
| Jwt:Issuer | `InventoryServer` | Expected issuer of valid tokens | Yes |
| Jwt:Audience | `InventoryClient` | Expected audience for valid tokens | Yes |
| Jwt:ExpireMinutes | `15` | Token expiration time in minutes | No |
| Jwt:RefreshTokenExpireDays | `7` | Refresh token expiration in days | No |

**Section sources**
- [appsettings.json](file://src/Inventory.API/appsettings.json#L4-L10)
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L132-L135)

## Request Handling Logic

The request handling logic in `AuthenticationMiddleware` implements a comprehensive filtering system that determines whether authentication should be applied to incoming requests. The middleware evaluates requests based on their path and type, applying different rules for API endpoints, public routes, and static resources.

The logic follows a specific evaluation order: first checking if the request targets an API endpoint, then determining if it should be exempt from authentication due to being a public endpoint or static file. This approach ensures that the application's UI, documentation, and authentication endpoints remain accessible without requiring prior authentication.

**Public Endpoints Exempt from Authentication**

| Endpoint | Purpose |
|---------|--------|
| `/api/auth/login` | User authentication and token issuance |
| `/api/auth/register` | New user registration |
| `/api/health` | System health check endpoint |
| `/health` | Alternative health check endpoint |
| `/swagger` | API documentation and testing interface |
| `/notificationHub` | SignalR hub for real-time notifications |

**Static File Extensions Exempt from Authentication**

| Extension | File Type |
|---------|---------|
| `.css` | Cascading Style Sheets |
| `.js` | JavaScript files |
| `.png`, `.jpg`, `.jpeg`, `.gif`, `.ico`, `.svg` | Image files |
| `.woff`, `.woff2`, `.ttf`, `.eot` | Web font files |

```mermaid
flowchart TD
Request[Incoming HTTP Request] --> APIRoute{Path starts with /api?}
APIRoute --> |No| BlazorUI[Serve Blazor UI]
APIRoute --> |Yes| PublicEndpoint{Public endpoint?}
PublicEndpoint --> |Yes| Allow[Allow without authentication]
PublicEndpoint --> |No| StaticFile{Static file?}
StaticFile --> |Yes| Allow
StaticFile --> |No| Authenticate[Require authentication]
Authenticate --> ValidateToken[Validate JWT token]
ValidateToken --> |Valid| Process[Process request]
ValidateToken --> |Invalid| Reject[Return 401 Unauthorized]
BlazorUI --> Response
Allow --> Response
Process --> Response
Reject --> Response
```

**Diagram sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L75-L94)

**Section sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L30-L94)

## Error Handling and Unauthorized Responses

The `AuthenticationMiddleware` implements robust error handling to manage various authentication failure scenarios. When authentication fails due to missing or invalid tokens, the middleware generates standardized 401 Unauthorized responses with consistent JSON formatting. This ensures that clients receive predictable error information that can be easily parsed and handled.

The error handling process includes logging mechanisms that record authentication failures at appropriate severity levels. Warning-level logs are generated for missing or invalid tokens, while error-level logs capture unexpected exceptions during the authentication process. This logging strategy provides visibility into authentication attempts while maintaining security by not revealing sensitive information in logs.

**Unauthorized Response Format**

```json
{
  "success": false,
  "message": "Unauthorized access. Please log in.",
  "error": "UNAUTHORIZED"
}
```

The middleware handles several types of authentication failures:

1. **Missing Authorization Header**: When no Authorization header is present in the request
2. **Invalid Header Format**: When the Authorization header doesn't follow the "Bearer {token}" format
3. **Invalid Token**: When the JWT token fails validation checks (signature, issuer, audience, lifetime)
4. **Configuration Errors**: When JWT configuration is missing or incomplete
5. **Unexpected Exceptions**: When errors occur during the authentication process

```mermaid
flowchart TD
Start([Authentication Error]) --> CheckType["Determine error type"]
CheckType --> |Missing Header| LogWarning["Log warning with path"]
CheckType --> |Invalid Format| LogWarning["Log warning with path"]
CheckType --> |Invalid Token| LogWarning["Log warning with path"]
CheckType --> |Configuration Error| LogError["Log configuration error"]
CheckType --> |Unexpected Exception| LogError["Log exception with path"]
LogWarning --> SetStatus["Set response status to 401"]
LogError --> SetStatus
SetStatus --> SetContentType["Set content type to application/json"]
SetContentType --> CreateResponse["Create standardized JSON response"]
CreateResponse --> WriteResponse["Write response to context"]
WriteResponse --> End([Response sent])
```

**Diagram sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L152-L166)

**Section sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L152-L166)

## Logging Practices

The `AuthenticationMiddleware` employs comprehensive logging practices using the `ILogger` interface to provide visibility into authentication activities. The logging strategy balances security requirements with operational monitoring needs, capturing essential information for troubleshooting while avoiding the logging of sensitive data such as complete JWT tokens.

The middleware uses different log levels to categorize events appropriately:
- **Warning Level**: Used for authentication failures (missing headers, invalid tokens)
- **Error Level**: Used for configuration issues and unexpected exceptions
- **Information Level**: Not directly used by middleware but available through framework integration

Log messages include contextual information such as the request path, which helps in identifying patterns of failed authentication attempts and potential security threats. The structured logging approach with named parameters enables effective log querying and analysis in production environments.

**Key Logging Events**

| Event Type | Log Level | Logged Information |
|-----------|---------|-------------------|
| Missing Authorization Header | Warning | Request path |
| Invalid Authorization Header | Warning | Request path |
| Invalid Token | Warning | Request path |
| Token Validation Failed | Error | Exception details, request path |
| JWT Configuration Error | Error | Configuration issue description |
| Unexpected Middleware Error | Error | Full exception details, request path |

**Section sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L38-L42)
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L50-L52)
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L137-L138)
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L159-L161)

## Integration with Application Pipeline

The `AuthenticationMiddleware` is integrated into the ASP.NET Core application pipeline through explicit registration in the `Program.cs` file. It is positioned strategically within the middleware pipeline to ensure proper request processing flow while maintaining compatibility with other middleware components.

The middleware is registered using the `UseMiddleware<T>` extension method, which adds it to the application's request processing pipeline. It is placed after the global exception handler but before the audit middleware, ensuring that only authenticated requests are logged for auditing purposes.

**Middleware Pipeline Order**

1. Swagger (development only)
2. Database initialization
3. Static files
4. Blazor framework files
5. HTTPS redirection
6. Global exception handling
7. **Authentication middleware**
8. Audit middleware
9. CORS configuration
10. Rate limiting
11. Authentication services
12. Authorization services
13. Controllers
14. SignalR hubs
15. Blazor fallback

This ordering ensures that:
- Development tools like Swagger are accessible without authentication
- Static resources for the Blazor UI are served without authentication
- Authentication is enforced before audit logging
- Rate limiting applies to authenticated requests
- Standard ASP.NET Core authentication and authorization can coexist with custom middleware

**Section sources**
- [Program.cs](file://src/Inventory.API/Program.cs#L398-L401)
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L7-L166)

## Behavior Examples

The following examples illustrate the behavior of `AuthenticationMiddleware` under various scenarios:

### Example 1: Authentication Bypass for Public Endpoints

**Request**
```
GET /swagger HTTP/1.1
Host: inventory.example.com
```

**Processing Flow**
1. Request path does not start with "/api" → Skip authentication
2. Request proceeds to next middleware
3. Swagger UI is served to the client

**Section sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L30-L34)

### Example 2: Successful API Authentication

**Request**
```
GET /api/products HTTP/1.1
Host: inventory.example.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Processing Flow**
1. Request path starts with "/api" → Continue authentication
2. Path is not a public endpoint → Continue authentication
3. Path is not a static file → Continue authentication
4. Authorization header present and valid format → Extract token
5. Token validation succeeds → Request proceeds to next middleware
6. Product data is returned to the client

**Section sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L36-L73)

### Example 3: Unauthorized Request Due to Missing Token

**Request**
```
GET /api/products HTTP/1.1
Host: inventory.example.com
```

**Processing Flow**
1. Request path starts with "/api" → Continue authentication
2. Path is not a public endpoint → Continue authentication
3. Path is not a static file → Continue authentication
4. Authorization header missing → Log warning
5. Return 401 Unauthorized with standardized response
6. Request processing terminates

**Section sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L38-L42)
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L152-L166)

### Example 4: Authentication Bypass for Static Files

**Request**
```
GET /css/app.css HTTP/1.1
Host: inventory.example.com
```

**Processing Flow**
1. Request path does not start with "/api" → Skip authentication
2. Request proceeds to next middleware
3. CSS file is served to the client

**Section sources**
- [AuthenticationMiddleware.cs](file://src/Inventory.API/Middleware/AuthenticationMiddleware.cs#L30-L34)