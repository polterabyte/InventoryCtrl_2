# Deployment Scripts Documentation

## Overview

The deployment system has been refactored to eliminate code duplication and provide a unified approach to deploying different environments.

## Scripts

### 1. Universal Deployment Script (`deploy/deploy.ps1`)

The main deployment script that handles all environments with flexible configuration.

**Usage:**
```powershell
.\deploy\deploy.ps1 -Environment <environment> [options]
```

**Parameters:**
- `Environment` (Mandatory): The target environment
  - `staging` - Deploy to staging environment
  - `production` - Deploy to production environment  
  - `test` - Deploy to test environment
- `EnvFile` (Optional): Custom environment file path
- `ComposeFile` (Optional): Custom Docker Compose file path
- `Domain` (Optional): Custom domain name
- `BaseDomain` (Optional): Base domain for environment subdomains (default: "warehouse.cuby")
- `SkipVapidCheck` (Optional): Skip VAPID keys generation check
- `HealthCheckTimeout` (Optional): Health check timeout in seconds (default: 30)

**Examples:**
```powershell
# Basic usage
.\deploy\deploy.ps1 -Environment staging

# Custom configuration
.\deploy\deploy.ps1 -Environment staging -EnvFile "custom.env" -ComposeFile "custom-compose.yml"

# Custom domain
.\deploy\deploy.ps1 -Environment production -Domain "myapp.example.com" -BaseDomain "example.com"

# Skip VAPID check and custom timeout
.\deploy\deploy.ps1 -Environment test -SkipVapidCheck -HealthCheckTimeout 60
```

### 2. Environment-Specific Scripts

These are wrapper scripts that call the universal deployment script with all the same parameters:

- `deploy/deploy-staging.ps1` - Deploys to staging environment
- `deploy/deploy-production.ps1` - Deploys to production environment
- `deploy/deploy-test.ps1` - Deploys to test environment

**Usage:**
```powershell
# Basic usage
.\deploy-staging.ps1
.\deploy-production.ps1
.\deploy-test.ps1

# With custom parameters
.\deploy-staging.ps1 -EnvFile "custom.env" -Domain "staging.myapp.com"
.\deploy-production.ps1 -SkipVapidCheck -HealthCheckTimeout 60
.\deploy-test.ps1 -ComposeFile "test-compose.yml" -BaseDomain "test.example.com"
```

### 3. Deploy All Script (`deploy/deploy-all.ps1`)

Deploy multiple environments or all environments at once.

**Usage:**
```powershell
.\deploy/deploy-all.ps1 -Environment <environment> [options]
```

**Parameters:**
- `Environment` (Optional): Target environment(s)
  - `staging` - Deploy only staging
  - `production` - Deploy only production
  - `test` - Deploy only test
  - `all` - Deploy all environments (default)
- `SkipHealthCheck` (Optional): Skip health checks during deployment
- `BaseDomain` (Optional): Base domain for environment subdomains (default: "warehouse.cuby")
- `SkipVapidCheck` (Optional): Skip VAPID keys generation check
- `HealthCheckTimeout` (Optional): Health check timeout in seconds (default: 30)

**Examples:**
```powershell
# Deploy all environments
.\deploy-all.ps1

# Deploy only staging
.\deploy-all.ps1 -Environment staging

# Deploy all with custom base domain
.\deploy-all.ps1 -BaseDomain "myapp.com"

# Deploy all with custom settings
.\deploy-all.ps1 -SkipVapidCheck -HealthCheckTimeout 60
```

### 4. Custom Deployment Script (`deploy/deploy-custom.ps1`)

Deploy with completely custom file names and domains.

**Usage:**
```powershell
.\deploy/deploy-custom.ps1 -Environment <environment> [options]
```

**Parameters:**
- `Environment` (Mandatory): The target environment
- `EnvFile` (Optional): Custom environment file path
- `ComposeFile` (Optional): Custom Docker Compose file path
- `Domain` (Optional): Custom domain name
- `BaseDomain` (Optional): Base domain for environment subdomains (default: "warehouse.cuby")
- `SkipVapidCheck` (Optional): Skip VAPID keys generation check
- `HealthCheckTimeout` (Optional): Health check timeout in seconds (default: 30)
- `DryRun` (Optional): Show what would be executed without running

**Examples:**
```powershell
# Custom files
.\deploy-custom.ps1 -Environment staging -EnvFile "custom.env" -ComposeFile "custom-compose.yml"

# Custom domain
.\deploy-custom.ps1 -Environment production -Domain "myapp.example.com" -BaseDomain "example.com"

# Dry run to see what would happen
.\deploy-custom.ps1 -Environment test -EnvFile "test.env" -DryRun
```

## Environment Configuration

Each environment follows a standardized naming pattern:

| Environment | Domain | Env File | Compose File |
|-------------|--------|----------|--------------|
| staging | staging.warehouse.cuby | deploy/env.staging | docker-compose.staging.yml |
| production | warehouse.cuby | deploy/env.production | docker-compose.production.yml |
| test | test.warehouse.cuby | deploy/env.test | docker-compose.test.yml |

### Standardized Naming Convention

All environment files follow a consistent pattern:
- **Environment files**: `deploy/env.{environment}`
- **Docker Compose files**: `docker-compose.{environment}.yml`
- **Domains**: `{environment}.{base-domain}` (except production which uses just `{base-domain}`)

This standardization makes it easy to:
- Add new environments
- Understand file purposes at a glance
- Maintain consistency across the project

### Required Environment Variables

В `.env` для деплоя задаются ключевые переменные (пример — см. `deploy/env.example`):

- `ConnectionStrings__DefaultConnection` — строка подключения PostgreSQL
- `Jwt__Key` — секрет JWT (обязателен вне Development)
- `CORS_ALLOWED_ORIGINS` — список разрешённых Origin (через запятую)
- `ADMIN_EMAIL`, `ADMIN_USERNAME`, `ADMIN_PASSWORD` — первичный админ
- `ApiUrl` — базовый URL API (используется сервером для сервисов, напр. `LocationApiService`)

Также docker-compose передаёт `ApiUrl` и `CORS_ALLOWED_ORIGINS` в контейнер `inventory-api`.

## Deployment Process

The universal deployment script follows this process:

1. **VAPID Keys Check**: Verifies and generates VAPID keys if needed
2. **Environment File Validation**: Checks if the environment file exists
3. **Container Cleanup**: Stops existing containers
4. **Service Deployment**: Builds and starts services using Docker Compose
5. **Health Check**: Waits for services and verifies health
6. **Status Display**: Shows running containers and deployment summary

## Error Handling

- The script includes comprehensive error handling
- Failed deployments return appropriate exit codes
- Health check failures are reported with clear messages
- All errors are logged with descriptive messages

## Benefits of Refactoring

1. **Eliminated Code Duplication**: Reduced ~150 lines of duplicated code to ~10 lines per environment script
2. **Centralized Logic**: All deployment logic is in one place, making maintenance easier
3. **Consistent Behavior**: All environments follow the same deployment process
4. **Easy Extension**: Adding new environments requires only configuration changes
5. **Better Error Handling**: Unified error handling across all environments
6. **Improved Testing**: Single script to test deployment logic

## Migration Notes

- Existing individual scripts (`deploy-staging.ps1`, `deploy-production.ps1`, `deploy-test.ps1`) are now wrappers
- They maintain backward compatibility
- The universal script (`deploy/deploy.ps1`) can be used directly
- All existing functionality is preserved

## Troubleshooting

### Common Issues

1. **VAPID Keys Not Generated**: The script automatically generates VAPID keys if they're missing
2. **Environment File Missing**: The script will warn if environment files are missing
3. **Health Check Fails**: Check Docker logs and ensure services are running properly
4. **Permission Issues**: Ensure the script has proper execution permissions

### Debug Mode

To debug deployment issues, you can run the script with verbose output:

```powershell
$VerbosePreference = "Continue"
.\deploy.ps1 -Environment staging
```
