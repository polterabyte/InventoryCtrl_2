# Agent Guidelines for Inventory Control System

## Build/Test Commands
- **Run all tests**: `powershell -File .\test\run-tests.ps1`
- **Run link and build project**: `powershell -File .\scripts\quick-lint.ps1`
- **Run specific test type**: `powershell -File .\test\run-tests.ps1 -Project unit|integration|component`
- **Run single test**: `dotnet test --filter "FullyQualifiedName~TestClass.TestMethod"`
- **Build project**: `dotnet build`
- **Deploy**: `powershell -File .\deploy\quick-staging.ps1`

## Code Style Guidelines
- **C#**: Primary constructors for DI, async/await for DB ops, PascalCase for types, camelCase for params
- **Architecture**: Controllers return `ApiResponse<T>`, Services use constructor injection, FluentValidation
- **Error Handling**: `ApiResponse<T>.Success(data)` or `ApiResponse<T>.Error("msg")`
- **Naming**: Interfaces `I{ServiceName}`, DTOs `{Entity}Dto`, Controllers `{Entity}Controller`
- **Security**: Never store secrets in code, use User Secrets/ENV vars, validate inputs

## Additional Resources
See `.github/copilot-instructions.md` for detailed project context, SignalR patterns, and deployment workflows.