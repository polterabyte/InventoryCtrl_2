# Build Validation System

A comprehensive build validation and error resolution system for InventoryCtrl_2, implementing the design document specifications for systematic error detection, classification, and resolution.

## Overview

This system provides:

- **Comprehensive Build Validation**: Multi-phase validation including dependencies, compilation, Docker builds, and environment configuration
- **Error Classification**: Automatic categorization and severity assessment of build errors
- **Automated Resolution**: Intelligent error resolution strategies with fallback mechanisms
- **Docker Diagnostics**: Stage-based Docker build validation and failure analysis
- **Environment Validation**: Configuration verification and health checks
- **Runtime Exception Handling**: Structured error responses and recovery mechanisms
- **Testing Framework**: Multi-level testing with error simulation capabilities
- **Monitoring Infrastructure**: Proactive monitoring with alerting and metrics collection

## Architecture

```
BuildValidationOrchestrator
├── BuildValidator (Build validation and dependency analysis)
├── CompilationErrorResolver (Error resolution strategies)
├── DockerBuildDiagnostics (Docker validation and diagnostics)
├── EnvironmentValidator (Environment and configuration validation)
├── RuntimeExceptionHandler (Runtime error handling)
├── TestingFramework (Comprehensive testing suite)
└── MonitoringSystem (Health monitoring and metrics)
```

## Usage
 
### Command Line Interface

```bash
# Complete validation suite
./BuildValidation validate

# Targeted validation
./BuildValidation build
./BuildValidation docker
./BuildValidation environment
./BuildValidation tests
./BuildValidation monitoring

# System health check
./BuildValidation health

# Error diagnosis
./BuildValidation diagnose error.log

# Interactive mode
./BuildValidation
```

### Programmatic Usage

```csharp
var workspaceRoot = "/path/to/InventoryCtrl_2";
var orchestrator = new BuildValidationOrchestrator(workspaceRoot);

// Complete validation
var result = await orchestrator.ExecuteCompleteValidationAsync();

// Targeted validation
var dockerResult = await orchestrator.ExecuteTargetedValidationAsync(ValidationTarget.Docker);

// Error diagnosis
var diagnosis = await orchestrator.DiagnoseAndResolveErrorsAsync(errorLog);
```

## Validation Phases

### 1. Build Validation
- Package dependency analysis
- Project reference validation
- Compilation integrity checks
- Framework compatibility verification

### 2. Docker Validation
- Dockerfile syntax and best practices
- Multi-stage build configuration
- Build context validation
- Container health checks

### 3. Environment Validation
- Required environment variables
- Configuration file validation
- Service connectivity tests
- SSL certificate verification

### 4. Testing Validation
- Unit test execution
- Integration test validation
- Component test verification
- Error simulation scenarios

### 5. Monitoring Setup
- Health endpoint configuration
- Performance metrics collection
- Log aggregation setup
- Alerting configuration

## Error Classification

The system classifies errors into categories with severity levels:

### Error Categories
- **CompilationError**: Syntax and compilation issues
- **PackageReference**: NuGet package conflicts
- **ProjectReference**: Project dependency issues
- **FrameworkCompatibility**: .NET framework mismatches
- **DockerBuild**: Container build failures
- **DatabaseConnectivity**: Database connection issues
- **Authentication**: JWT and security configuration
- **NetworkConnectivity**: Network and service discovery
- **EnvironmentConfiguration**: Environment variable issues
- **RuntimeException**: Application runtime errors

### Severity Levels
- **Critical**: System-down scenarios (immediate response)
- **High**: Feature impact (1-hour response)
- **Medium**: Performance issues (4-hour response)
- **Low**: Minor issues (next business day)

## Configuration

### Build Configuration (`build-config.json`)

```json
{
  "buildConfiguration": {
    "enableParallelBuild": true,
    "buildTimeout": "00:10:00",
    "validateDockerBuilds": true,
    "validateEnvironmentConfig": true
  },
  "errorThresholds": {
    "critical": {
      "responseTime": "00:00:15",
      "escalationTeam": ["on-call", "team-lead"]
    }
  }
}
```

### Environment Variables

**Database Configuration:**
- `ConnectionStrings__DefaultConnection`
- `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`

**Authentication Configuration:**
- `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`

**Networking Configuration:**
- `SERVER_IP`, `DOMAIN`, `ASPNETCORE_URLS`

**SSL Configuration:**
- `SSL_CERT_PATH`, `SSL_KEY_PATH`

## Testing Framework

### Test Types Supported
1. **Unit Tests**: Isolated business logic validation
2. **Integration Tests**: Service-to-service communication
3. **Component Tests**: Blazor component rendering and interaction
4. **Error Simulation**: Failure scenario testing

### Error Simulation Scenarios
- Network failure scenarios
- Database unavailability testing
- Authentication service failures
- Resource exhaustion scenarios
- Configuration error handling
- External dependency failures

## Monitoring and Diagnostics

### Health Monitoring
- Application health endpoints
- Database connectivity checks
- Container health validation
- SSL certificate monitoring

### Performance Metrics
- System metrics (CPU, memory, disk, network)
- Application metrics (requests/sec, response time, error rate)
- Database metrics (connection pool, query time)
- Container metrics (resource usage per container)

### Alerting
- Real-time health status alerts
- Performance threshold monitoring
- Certificate expiration warnings
- System failure notifications

## Resolution Strategies

### Automated Resolution
- Missing using statement injection
- Package version conflict resolution
- Project reference correction
- Configuration validation fixes

### Manual Resolution Guidance
- Detailed error classification
- Resolution recommendations
- Escalation procedures
- Documentation references

## Integration

### ASP.NET Core Integration

```csharp
// Startup.cs
public void Configure(IApplicationBuilder app)
{
    app.UseRuntimeExceptionHandler();
    // ... other middleware
}
```

### Docker Integration
The system validates Docker builds across all stages:
- Base image validation
- Dependency restoration
- Application compilation
- Runtime configuration

### CI/CD Integration
- Build pipeline validation
- Deployment verification
- Health check validation
- Rollback procedures

## Error Response Format

Structured error responses following design document specifications:

```json
{
  "success": false,
  "message": "User-friendly error message",
  "error": "ERROR_CODE",
  "category": "ErrorCategory",
  "timestamp": "2024-01-01T12:00:00Z",
  "traceId": "abc123def456",
  "resolutionSuggestions": ["suggestion1", "suggestion2"]
}
```

## Escalation Matrix

| Severity | Response Time | Team | Escalation Trigger |
|----------|---------------|------|-------------------|
| Critical | Immediate | On-call engineer | 15 minutes |
| High | 1 hour | Development team | 4 hours |
| Medium | 4 hours | Development team | 1 day |
| Low | Next business day | Development team | 3 days |

## Dependencies

- .NET 8.0
- Docker (for container validation)
- PostgreSQL (for database validation)
- System.Text.Json (for configuration)

## Development

### Building the System
```bash
dotnet build scripts/BuildValidation/
```

### Running Tests
```bash
dotnet test test/ --no-build
```

### Configuration Updates
Edit `build-config.json` to customize validation behavior, error thresholds, and monitoring settings.

## Troubleshooting

### Common Issues

1. **Docker not available**: Install Docker and ensure service is running
2. **Environment variables missing**: Check `.env` files and system configuration
3. **Database connectivity**: Verify PostgreSQL service and connection strings
4. **Certificate issues**: Check SSL certificate paths and permissions

### Diagnostic Commands
```bash
# Check system health
./BuildValidation health

# Validate specific components
./BuildValidation docker
./BuildValidation environment

# Analyze error logs
./BuildValidation diagnose build-error.log
```

### Logs and Monitoring
- Application logs: `/app/logs/`
- Build validation logs: Console output and structured logging
- Health check results: Real-time status dashboard
- Performance metrics: Time-series data collection

## Contributing

1. Follow the error classification system defined in `ErrorClassification.cs`
2. Add new validation strategies in respective validator classes
3. Update configuration schema when adding new validation options
4. Include comprehensive error handling and logging
5. Write tests for new validation scenarios

## License

This build validation system is part of the InventoryCtrl_2 project and follows the same licensing terms.