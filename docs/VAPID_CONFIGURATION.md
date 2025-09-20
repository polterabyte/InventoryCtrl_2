# VAPID Configuration for Web Push Notifications

## Overview

VAPID (Voluntary Application Server Identification) keys are used for Web Push Notifications to authenticate the application server with push services. This document explains how VAPID keys are configured in the Inventory Control System.

## Configuration Structure

### 1. Application Settings

VAPID configuration is defined in the `VapidConfiguration` class:

```csharp
public class VapidConfiguration
{
    public string Subject { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}
```

### 2. Configuration Files

VAPID settings are configured in multiple places:

- `appsettings.json` - Base configuration
- `appsettings.Production.json` - Production overrides
- `appsettings.Development.json` - Development overrides

### 3. Environment Variables

For Docker deployments, VAPID keys are configured via environment variables:

- `VAPID_SUBJECT` - Email or URL identifying the application
- `VAPID_PUBLIC_KEY` - Public key for client-side authentication
- `VAPID_PRIVATE_KEY` - Private key for server-side authentication

## Docker Configuration

### Docker Compose Files

All Docker Compose files include VAPID environment variables:

**Production (`docker-compose.production.yml`):**
```yaml
environment:
  - Vapid__Subject=${VAPID_SUBJECT:-mailto:admin@inventorycontrol.com}
  - Vapid__PublicKey=${VAPID_PUBLIC_KEY}
  - Vapid__PrivateKey=${VAPID_PRIVATE_KEY}
```

**Staging (`docker-compose.staging.yml`):**
```yaml
environment:
  - Vapid__Subject=${VAPID_SUBJECT:-mailto:admin@staging.warehouse.cuby}
  - Vapid__PublicKey=${VAPID_PUBLIC_KEY}
  - Vapid__PrivateKey=${VAPID_PRIVATE_KEY}
```

**Test (`docker-compose.test.yml`):**
```yaml
environment:
  - Vapid__Subject=${VAPID_SUBJECT:-mailto:admin@test.warehouse.cuby}
  - Vapid__PublicKey=${VAPID_PUBLIC_KEY}
  - Vapid__PrivateKey=${VAPID_PRIVATE_KEY}
```

### Environment Files

VAPID configuration is defined in environment files:

- `deploy/env.production` - Production environment
- `deploy/env.staging` - Staging environment  
- `deploy/env.test` - Test environment

Example configuration:
```bash
# VAPID Configuration for Web Push Notifications
VAPID_SUBJECT=mailto:admin@warehouse.cuby
VAPID_PUBLIC_KEY=
VAPID_PRIVATE_KEY=
```

## Key Generation

### Automatic Generation

The system includes scripts for generating VAPID keys:

1. **`scripts/generate-vapid-keys.ps1`** - Generates keys for all environments
2. **`scripts/generate-vapid-production.ps1`** - Generates keys for specific environment

### Manual Generation

To generate VAPID keys manually:

```bash
# Install web-push globally
npm install -g web-push

# Generate keys
node -e "const webpush = require('web-push'); const keys = webpush.generateVAPIDKeys(); console.log(JSON.stringify(keys));"
```

### Production Deployment

The production deployment script automatically checks for VAPID keys and generates them if missing:

```powershell
# Check if VAPID keys are configured
if ($envContent -match "VAPID_PUBLIC_KEY=$" -or $envContent -match "VAPID_PRIVATE_KEY=$") {
    Write-Host "VAPID keys not configured. Generating them..." -ForegroundColor Yellow
    & ".\scripts\generate-vapid-production.ps1" -Environment "production"
}
```

## Security Considerations

### Private Key Security

1. **Never commit private keys to version control**
2. **Use environment variables for production deployments**
3. **Store private keys securely in production environment**
4. **Rotate keys periodically**

### Public Key Usage

1. **Public keys can be safely shared with clients**
2. **Public keys are embedded in client-side code**
3. **Public keys are used for client-side push subscription**

## Client-Side Configuration

### Service Worker

The public key is automatically injected into the service worker:

```javascript
// In sw.js
const VAPID_PUBLIC_KEY = 'YOUR_VAPID_PUBLIC_KEY_HERE';
```

### Push Notifications JavaScript

The public key is also injected into the push notifications JavaScript:

```javascript
// In push-notifications.js
const VAPID_PUBLIC_KEY = 'YOUR_VAPID_PUBLIC_KEY_HERE';
```

## Nginx Configuration

### Does nginx need special configuration for VAPID?

**Answer: NO, nginx does not require any special configuration for VAPID.**

#### Why nginx doesn't need VAPID configuration:

1. **VAPID works at application level** - it's not a network protocol
2. **Push notifications go through external services** (FCM, Mozilla Push Service)
3. **nginx only proxies HTTP requests** to the API
4. **VAPID keys are used in client-side JavaScript code**

#### What nginx does for push notifications:

- **Proxies API requests** to the backend service
- **Serves static files** (including service worker and JavaScript)
- **Handles HTTPS termination** (required for push notifications)
- **Routes requests** to appropriate services

#### Current nginx configuration is sufficient:

```nginx
# API requests are proxied to backend
location /api/ {
    proxy_pass http://inventory-api;
}

# Static files are served directly
location / {
    proxy_pass http://inventory-web;
}
```

## Troubleshooting

### Common Issues

1. **VAPID keys not configured**
   - Run the key generation script
   - Check environment variables are set

2. **Push notifications not working**
   - Verify VAPID keys are correctly configured
   - Check browser console for errors
   - Ensure HTTPS is enabled in production

3. **Keys not updating in Docker**
   - Rebuild Docker containers after updating environment files
   - Check environment variables are properly loaded

4. **Separators removed from environment files**
   - **Fixed in v1.1**: Scripts now preserve separators (like `-------------------------------`)
   - The issue was caused by overly broad regex patterns
   - Updated regex patterns now use `.*$` to match only to end of line

### Verification

To verify VAPID configuration:

1. Check environment variables in running container:
   ```bash
   docker exec inventory-api-prod env | grep VAPID
   ```

2. Check application logs for VAPID configuration:
   ```bash
   docker logs inventory-api-prod | grep -i vapid
   ```

3. Test push notification endpoint:
   ```bash
   curl -X POST https://warehouse.cuby/api/notifications/test
   ```

## Best Practices

1. **Use different VAPID keys for different environments**
2. **Rotate keys periodically for security**
3. **Monitor push notification delivery rates**
4. **Test push notifications in staging before production**
5. **Keep VAPID keys in secure environment variable storage**

## Related Files

- `src/Inventory.API/Configuration/VapidConfiguration.cs`
- `scripts/generate-vapid-keys.ps1`
- `scripts/generate-vapid-production.ps1`
- `deploy-production.ps1`
- `deploy/env.production`
- `deploy/env.staging`
- `deploy/env.test`
- `docker-compose.yml`
- `docker-compose.production.yml`
