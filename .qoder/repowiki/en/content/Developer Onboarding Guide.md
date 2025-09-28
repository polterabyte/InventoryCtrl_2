# Developer Onboarding Guide

<cite>
**Referenced Files in This Document**   
- [README.md](file://README.md)
- [quick-setup.ps1](file://deploy/quick-setup.ps1)
- [setup-local-dns.ps1](file://deploy/setup-local-dns.ps1)
- [init-db.sql](file://scripts/init-db.sql)
- [docker-compose.yml](file://docker-compose.yml)
- [Program.cs](file://src/Inventory.API/Program.cs)
- [Program.cs](file://src/Inventory.Web.Client/Program.cs)
- [launchSettings.json](file://src/Inventory.API/Properties/launchSettings.json)
- [launchSettings.json](file://src/Inventory.Web.Client/Properties/launchSettings.json)
- [Dockerfile](file://src/Inventory.API/Dockerfile)
- [Dockerfile](file://src/Inventory.Web.Client/Dockerfile)
- [appsettings.json](file://src/Inventory.API/appsettings.json)
- [appsettings.json](file://src/Inventory.Web.Client/appsettings.json)
</cite>

## Table of Contents
1. [Introduction](#introduction)
2. [Prerequisites](#prerequisites)
3. [Repository Setup](#repository-setup)
4. [Local DNS Configuration](#local-dns-configuration)
5. [Database Initialization](#database-initialization)
6. [Service Orchestration with Docker Compose](#service-orchestration-with-docker-compose)
7. [Backend API Debugging](#backend-api-debugging)
8. [Blazor Frontend Debugging](#blazor-frontend-debugging)
9. [Troubleshooting Common Issues](#troubleshooting-common-issues)
10. [Making First Code Changes](#making-first-code-changes)
11. [Running Tests](#running-tests)
12. [Platform-Specific Considerations](#platform-specific-considerations)

## Introduction
This guide provides comprehensive instructions for onboarding developers to the InventoryCtrl_2 project. It covers the complete setup process for a local development environment, including prerequisites, repository cloning, DNS configuration, database initialization, and service orchestration. The document also includes debugging guidance for both backend and frontend components, troubleshooting solutions for common issues, and practical examples for making code changes and running tests. Special attention is given to Windows-specific considerations for the development environment.

## Prerequisites
Before beginning the setup process, ensure the following prerequisites are installed on your development machine:

- **.NET 8.0 SDK**: Required for building and running both the backend API and Blazor WebAssembly client. Download from the official Microsoft website.
- **PostgreSQL 14+**: The database system used by the application. Install PostgreSQL and ensure the service is running.
- **Docker Desktop**: Required for containerized deployment and service orchestration. Install Docker Desktop for Windows with WSL 2 backend enabled.
- **PowerShell 7+**: Required for executing deployment and setup scripts. Install PowerShell Core if not already available.

Verify installations by running the following commands in PowerShell:
```powershell
dotnet --version
docker --version
docker-compose --version
psql --version
```

**Section sources**
- [README.md](file://README.md#L1-L270)

## Repository Setup
Clone the InventoryCtrl_2 repository to your local machine using Git:

```powershell
git clone https://github.com/your-organization/InventoryCtrl_2.git
cd InventoryCtrl_2
```

After cloning, execute the quick-setup.ps1 script to initialize the development environment:

```powershell
.\deploy\quick-setup.ps1
```

This script performs several critical setup tasks:
- Creates necessary directories for deployment, logs, and backups
- Sets up environment variables by copying from environment-specific templates
- Checks for the presence of SSL certificates and provides guidance for generation
- Validates Docker and Docker Compose installation
- Verifies required ports (80 and 443) are available
- Creates systemd service files for Linux deployment
- Generates backup scripts for production environments

The script supports parameters for customizing the environment and domain:
```powershell
.\deploy\quick-setup.ps1 -Environment staging -Domain staging.warehouse.cuby
```

**Section sources**
- [quick-setup.ps1](file://deploy/quick-setup.ps1#L1-L175)
- [README.md](file://README.md#L1-L270)

## Local DNS Configuration
To enable access to the application via domain names rather than IP addresses, configure local DNS resolution using the setup-local-dns.ps1 script:

```powershell
.\deploy\setup-local-dns.ps1 -ServerIP "192.168.139.96"
```

This script modifies the Windows hosts file to map the following domains to the specified server IP:
- warehouse.cuby
- staging.warehouse.cuby
- test.warehouse.cuby

**Important**: This script requires administrator privileges to modify the hosts file. Run PowerShell as Administrator before executing the command.

To remove the DNS entries, use the Remove switch:
```powershell
.\deploy\setup-local-dns.ps1 -Remove
```

After successful configuration, you can access the application at:
- Production: https://warehouse.cuby
- Staging: https://staging.warehouse.cuby
- Test: https://test.warehouse.cuby

The changes take effect immediately without requiring a system restart.

**Section sources**
- [setup-local-dns.ps1](file://deploy/setup-local-dns.ps1#L1-L89)

## Database Initialization
The database is initialized automatically when the PostgreSQL container starts for the first time. The init-db.sql script is executed during container initialization and contains the following commands:

```sql
-- Set timezone
SET timezone = 'UTC';

-- Create extensions if needed
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
```

The actual database schema is created by Entity Framework Core migrations when the API service starts. The init-db.sql script is mounted to the PostgreSQL container via docker-compose.yml and executed automatically.

Database connection settings are configured through environment variables in the docker-compose.yml file:
- POSTGRES_DB: inventorydb
- POSTGRES_USER: postgres
- POSTGRES_PASSWORD: postgres (should be changed in production)

The database volume is persisted using a named volume "postgres_data" to preserve data between container restarts.

**Section sources**
- [init-db.sql](file://scripts/init-db.sql#L1-L15)
- [docker-compose.yml](file://docker-compose.yml#L1-L110)

## Service Orchestration with Docker Compose
The application services are orchestrated using Docker Compose. The primary docker-compose.yml file defines four services:

```yaml
services:
  postgres: # PostgreSQL database
  inventory-api: # ASP.NET Core Web API
  inventory-web: # Blazor WebAssembly client
  nginx-proxy: # Nginx reverse proxy
```

Start all services using Docker Compose:
```powershell
docker-compose up -d
```

This command builds and starts all containers in detached mode. The services are configured with proper dependencies:
- inventory-api depends on postgres (with health check)
- inventory-web depends on inventory-api (with health check)
- nginx-proxy depends on both inventory-api and inventory-web

Stop all services with:
```powershell
docker-compose down
```

For development, you can also use the quick-deploy.ps1 script:
```powershell
.\deploy\quick-deploy.ps1
```

**Section sources**
- [docker-compose.yml](file://docker-compose.yml#L1-L110)
- [README.md](file://README.md#L1-L270)

## Backend API Debugging
The backend API can be debugged using Visual Studio or VS Code with the following launch configurations:

### Visual Studio Configuration
The API project includes two launch profiles in launchSettings.json:
- **http**: Runs on http://localhost:5000
- **https**: Runs on https://localhost:7000 (with HTTP fallback on port 5000)

To debug:
1. Open the solution in Visual Studio
2. Set Inventory.API as the startup project
3. Select the desired launch profile (http or https)
4. Press F5 to start debugging

### VS Code Configuration
Create a launch.json configuration:
```json
{
    "name": "Launch API",
    "type": "coreclr",
    "request": "launch",
    "preLaunchTask": "build",
    "program": "dotnet",
    "args": ["run", "--project", "src/Inventory.API"],
    "cwd": "${workspaceFolder}",
    "stopAtEntry": false,
    "console": "internalConsole"
}
```

Key debugging features in the API:
- Serilog for structured logging
- Global exception handling middleware
- Audit middleware for tracking requests
- JWT authentication with detailed logging
- Rate limiting with rejection logging

**Section sources**
- [Program.cs](file://src/Inventory.API/Program.cs#L1-L450)
- [launchSettings.json](file://src/Inventory.API/Properties/launchSettings.json#L1-L23)
- [appsettings.json](file://src/Inventory.API/appsettings.json#L1-L75)

## Blazor Frontend Debugging
The Blazor WebAssembly client can be debugged using Visual Studio or VS Code with the following configurations:

### Visual Studio Configuration
The client project includes two launch profiles:
- **http**: Runs on http://localhost:5001
- **https**: Runs on https://localhost:7001 (with HTTP fallback on port 5001)

To debug:
1. Set Inventory.Web.Client as the startup project
2. Select the desired launch profile
3. Press F5 to start debugging with browser launch

### VS Code Configuration
Create a launch.json configuration:
```json
{
    "name": "Launch Client",
    "type": "blazorwasm",
    "request": "launch",
    "browser": "edge"
}
```

Key debugging features in the client:
- Custom authentication state provider
- HTTP interceptor for JWT token management
- Resilient API service with retry logic
- Token refresh service with error handling
- SignalR service for real-time notifications
- Comprehensive logging and error handling

The client uses relative paths for API calls, making it portable across environments.

**Section sources**
- [Program.cs](file://src/Inventory.Web.Client/Program.cs#L1-L147)
- [launchSettings.json](file://src/Inventory.Web.Client/Properties/launchSettings.json#L1-L25)
- [appsettings.json](file://src/Inventory.Web.Client/appsettings.json#L1-L42)

## Troubleshooting Common Issues
This section addresses common setup and runtime issues with their solutions.

### Database Connectivity Issues
**Symptoms**: API fails to start, database connection errors in logs.

**Solutions**:
1. Verify PostgreSQL service is running
2. Check connection string in environment variables
3. Ensure ports are not blocked by firewall
4. Validate database credentials
5. Check docker-compose logs: `docker-compose logs postgres`

### SSL Certificate Errors
**Symptoms**: Browser security warnings, HTTPS connection failures.

**Solutions**:
1. Generate SSL certificates using: `.\generate-ssl-warehouse.ps1`
2. Ensure certificates are placed in deploy/nginx/ssl directory
3. Verify certificate file permissions
4. Check nginx configuration for correct certificate paths
5. Clear browser cache and certificate store

### CORS Problems
**Symptoms**: API requests blocked by browser, CORS errors in console.

**Solutions**:
1. Verify CORS_ALLOWED_ORIGINS environment variable includes your domain
2. Check appsettings.json for CORS configuration
3. Ensure UseCors is called in the correct order in Program.cs
4. Validate that SignalRPolicy is properly configured
5. Test with different browsers to rule out caching issues

### Docker Container Issues
**Symptoms**: Containers fail to start, health checks failing.

**Solutions**:
1. Check container logs: `docker-compose logs <service-name>`
2. Verify port availability (80, 443, 5432, 5000, 5001)
3. Ensure sufficient disk space for Docker
4. Restart Docker Desktop
5. Clean and rebuild: `docker-compose down --volumes && docker-compose up --build`

**Section sources**
- [README.md](file://README.md#L1-L270)
- [Program.cs](file://src/Inventory.API/Program.cs#L1-L450)
- [docker-compose.yml](file://docker-compose.yml#L1-L110)

## Making First Code Changes
To make your first code changes and verify the development environment:

### Backend Changes
1. Navigate to src/Inventory.API/Controllers
2. Open any controller (e.g., ProductController.cs)
3. Add a simple endpoint:
```csharp
[HttpGet("test")]
public IActionResult TestEndpoint() => Ok("Hello from InventoryCtrl_2!");
```
4. Build and run the API
5. Test at https://localhost:7000/api/product/test

### Frontend Changes
1. Navigate to src/Inventory.Web.Client/Pages
2. Open Home.razor
3. Add a test component:
```razor
<div class="test-section">
    <h3>Test Connection</h3>
    <p>@testMessage</p>
</div>

@code {
    private string testMessage = "Connected to backend!";
}
```
4. Debug the client application
5. Verify the changes in the browser

### Configuration Changes
Update environment-specific settings in:
- src/Inventory.API/appsettings.json
- src/Inventory.Web.Client/appsettings.json
- .env file for Docker deployment

**Section sources**
- [Program.cs](file://src/Inventory.API/Program.cs#L1-L450)
- [Program.cs](file://src/Inventory.Web.Client/Program.cs#L1-L147)

## Running Tests
The project includes comprehensive testing at multiple levels:

### Test Types
- **Unit Tests**: Business logic and service methods
- **Integration Tests**: API controllers and database interactions
- **Component Tests**: Blazor UI components

### Running Tests
Execute all tests using the test script:
```powershell
.\test\run-tests.ps1
```

Run specific test types:
```powershell
# Unit tests
.\test\run-tests.ps1 -Project unit

# Integration tests  
.\test\run-tests.ps1 -Project integration

# Component tests
.\test\run-tests.ps1 -Project component

# With code coverage
.\test\run-tests.ps1 -Coverage
```

### Test Structure
- **Inventory.UnitTests**: Isolated unit tests using xUnit
- **Inventory.IntegrationTests**: Integration tests with test database
- **Inventory.ComponentTests**: Blazor component tests using bUnit

Test configuration is managed in appsettings.Test.json files.

**Section sources**
- [README.md](file://README.md#L1-L270)
- [run-tests.ps1](file://test/run-tests.ps1)

## Platform-Specific Considerations
This section addresses Windows-specific considerations for the development environment.

### PowerShell Execution Policy
The deployment scripts require appropriate execution policy:
```powershell
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### File Path Limitations
Windows has a 260-character path limit. To avoid issues:
- Clone the repository to a short path (e.g., C:\src\InventoryCtrl_2)
- Enable long paths in Windows 10+:
```powershell
New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" -Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force
```

### Docker Desktop Configuration
Optimize Docker Desktop for Windows:
- Allocate sufficient memory (at least 4GB)
- Enable WSL 2 backend
- Configure file sharing for the repository directory
- Set DNS servers if behind corporate firewall

### Hosts File Permissions
Modifying the hosts file requires administrator privileges. Always run PowerShell as Administrator when using setup-local-dns.ps1.

### Antivirus Interference
Some antivirus software may interfere with:
- Docker container networking
- Port binding (80, 443)
- File system monitoring
- Development server hot reload

Consider adding exclusions for:
- The repository directory
- Docker Desktop processes
- dotnet.exe

**Section sources**
- [setup-local-dns.ps1](file://deploy/setup-local-dns.ps1#L1-L89)
- [quick-setup.ps1](file://deploy/quick-setup.ps1#L1-L175)