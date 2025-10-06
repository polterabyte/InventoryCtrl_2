# Agent Guidelines for Inventory Control System

## Build/Test Commands
- **Run all tests**: `.\test\run-tests.ps1`
- **Run specific test type**: `.\test\run-tests.ps1 -Project unit|integration|component`
- **Run single test**: `dotnet test --filter "FullyQualifiedName~TestClass.TestMethod"`
- **Build project**: `dotnet build`
- **Quick deploy**: `.\deploy\quick-deploy.ps1`

## Code Style Guidelines

### C# Conventions
- Use primary constructors for dependency injection
- Async/await for all database operations
- PascalCase for classes/methods/properties, camelCase for parameters
- Use `var` for implicit typing where clear
- XML documentation comments for public APIs

### Architecture Patterns
- **Controllers**: Return `ApiResponse<T>` or `ApiResponse<T>.Error()`
- **Services**: Constructor injection, Serilog logging with contextual info
- **Validation**: FluentValidation with custom validators
- **Database**: EF Core with async operations, PostgreSQL
- **Authentication**: JWT + refresh tokens, role-based rate limiting

### Error Handling
```csharp
// API responses
return ApiResponse<T>.Success(data);
return ApiResponse<T>.Error("Error message");

// Logging
_logger.Information("Action {Action} by user {UserId}", action, userId);
```

### Naming Conventions
- Interfaces: `I{ServiceName}` (e.g., `IProductService`)
- DTOs: `{Entity}Dto` (e.g., `ProductDto`)
- Controllers: `{Entity}Controller`
- Services: `{Entity}Service`

### Security & Best Practices
- Never store secrets in code - use User Secrets/Environment variables
- Validate all inputs with FluentValidation
- Use rate limiting middleware
- Log security events with user context

## Copilot Instructions
See `.github/copilot-instructions.md` for detailed project context, SignalR patterns, and deployment workflows.