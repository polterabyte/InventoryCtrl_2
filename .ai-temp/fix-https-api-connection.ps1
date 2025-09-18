# Fix HTTPS API Connection
Write-Host "Fixing HTTPS API connection..." -ForegroundColor Green

# Update api-config.js to always use HTTPS for API
Write-Host "Updating api-config.js to always use HTTPS for API..." -ForegroundColor Yellow
$apiConfigContent = @"
// API Configuration for SignalR
console.log('Loading API configuration...');

// Define getApiBaseUrl function immediately
window.getApiBaseUrl = function() {
    const origin = window.location.origin;
    const port = window.location.port;
    
    // Always use HTTPS for API connection (more secure and required for SignalR)
    if (origin.startsWith('https://')) {
        // If web client is HTTPS, use HTTPS for API
        if (port) {
            return origin.replace(port, '7000'); // Use HTTPS port 7000
        } else {
            return origin + ':7000';
        }
    } else {
        // Even if web client is HTTP, use HTTPS for API (localhost development)
        return 'https://localhost:7000';
    }
};

// Define other SignalR functions
window.initializeSignalRConnection = async function(apiBaseUrl, accessToken, dotNetRef) {
    try {
        console.log('Initializing SignalR connection...', { apiBaseUrl, hasToken: !!accessToken });
        
        // Check if signalRNotificationService is available
        if (!window.signalRNotificationService) {
            console.error('signalRNotificationService is not available');
            return false;
        }
        
        const success = await window.signalRNotificationService.initialize(apiBaseUrl, accessToken);
        
        if (success) {
            // Set up event handlers
            window.signalRNotificationService.on('notificationReceived', (notification) => {
                dotNetRef.invokeMethodAsync('OnNotificationReceivedJS', notification);
            });

            window.signalRNotificationService.on('connectionStateChanged', (data) => {
                dotNetRef.invokeMethodAsync('OnConnectionStateChangedJS', data.state, data.error);
            });

            // Subscribe to common notification types
            await window.signalRNotificationService.subscribeToNotifications('STOCK');
            await window.signalRNotificationService.subscribeToNotifications('TRANSACTION');
            await window.signalRNotificationService.subscribeToNotifications('SYSTEM');
        }
        
        return success;
    } catch (error) {
        console.error('Error initializing SignalR connection:', error);
        return false;
    }
};

window.subscribeToNotificationType = async function(notificationType) {
    try {
        if (window.signalRNotificationService) {
            await window.signalRNotificationService.subscribeToNotifications(notificationType);
        }
    } catch (error) {
        console.error('Error subscribing to notification type:', error);
    }
};

window.unsubscribeFromNotificationType = async function(notificationType) {
    try {
        if (window.signalRNotificationService) {
            await window.signalRNotificationService.unsubscribeFromNotifications(notificationType);
        }
    } catch (error) {
        console.error('Error unsubscribing from notification type:', error);
    }
};

window.disconnectSignalR = async function() {
    try {
        if (window.signalRNotificationService) {
            await window.signalRNotificationService.disconnect();
        }
    } catch (error) {
        console.error('Error disconnecting SignalR:', error);
    }
};

console.log('API configuration loaded successfully');
console.log('getApiBaseUrl function available:', typeof window.getApiBaseUrl === 'function');
"@

Set-Content "src\Inventory.UI\wwwroot\js\api-config.js" $apiConfigContent
Set-Content "src\Inventory.Web.Client\wwwroot\js\api-config.js" $apiConfigContent

# Also check if signalr-notifications.js is properly loaded
Write-Host "Checking signalr-notifications.js..." -ForegroundColor Yellow
$signalrPath = "src\Inventory.UI\wwwroot\js\signalr-notifications.js"
if (Test-Path $signalrPath) {
    $signalrContent = Get-Content $signalrPath -Raw
    if ($signalrContent -match "window\.signalRNotificationService = new SignalRNotificationService\(\)") {
        Write-Host "✅ signalr-notifications.js looks correct" -ForegroundColor Green
    } else {
        Write-Host "❌ signalr-notifications.js missing global instance" -ForegroundColor Red
    }
} else {
    Write-Host "❌ signalr-notifications.js not found" -ForegroundColor Red
}

Write-Host "Fixed HTTPS API connection!" -ForegroundColor Green
Write-Host ""
Write-Host "Changes made:" -ForegroundColor Cyan
Write-Host "1. Updated getApiBaseUrl to always use HTTPS for API (https://localhost:7000)" -ForegroundColor White
Write-Host "2. Even HTTP web client now connects to HTTPS API" -ForegroundColor White
Write-Host "3. This should fix the 'ApiBaseUrl: http://localhost:5000' issue" -ForegroundColor White
Write-Host ""
Write-Host "Now the web client will connect to HTTPS API regardless of its own protocol" -ForegroundColor Green
