# Fix SignalR HTTPS Configuration
Write-Host "Fixing SignalR HTTPS configuration..." -ForegroundColor Green

# Update api-config.js to use HTTPS
Write-Host "Updating api-config.js to use HTTPS..." -ForegroundColor Yellow
$apiConfigContent = @"
// API Configuration for SignalR
console.log('Loading API configuration...');

// Define getApiBaseUrl function immediately
window.getApiBaseUrl = function() {
    const origin = window.location.origin;
    const port = window.location.port;
    
    // Use HTTPS for API connection
    if (origin.startsWith('https://')) {
        // If web client is HTTPS, use HTTPS for API
        if (port) {
            return origin.replace(port, '7000'); // Use HTTPS port 7000
        } else {
            return origin + ':7000';
        }
    } else {
        // If web client is HTTP, use HTTP for API
        if (port) {
            return origin.replace(port, '5000'); // Use HTTP port 5000
        } else {
            return origin + ':5000';
        }
    }
};

// Define other SignalR functions
window.initializeSignalRConnection = async function(apiBaseUrl, accessToken, dotNetRef) {
    try {
        console.log('Initializing SignalR connection...', { apiBaseUrl, hasToken: !!accessToken });
        
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
        await window.signalRNotificationService.subscribeToNotifications(notificationType);
    } catch (error) {
        console.error('Error subscribing to notification type:', error);
    }
};

window.unsubscribeFromNotificationType = async function(notificationType) {
    try {
        await window.signalRNotificationService.unsubscribeFromNotifications(notificationType);
    } catch (error) {
        console.error('Error unsubscribing from notification type:', error);
    }
};

window.disconnectSignalR = async function() {
    try {
        await window.signalRNotificationService.disconnect();
    } catch (error) {
        console.error('Error disconnecting SignalR:', error);
    }
};

console.log('API configuration loaded successfully');
console.log('getApiBaseUrl function available:', typeof window.getApiBaseUrl === 'function');
"@

Set-Content "src\Inventory.UI\wwwroot\js\api-config.js" $apiConfigContent
Set-Content "src\Inventory.Web.Client\wwwroot\js\api-config.js" $apiConfigContent

Write-Host "Updated api-config.js for both projects" -ForegroundColor Green

# Also update signalr-notifications.js to handle HTTPS properly
Write-Host "Updating signalr-notifications.js for HTTPS..." -ForegroundColor Yellow
$signalrContent = @"
// SignalR Notification Service
class SignalRNotificationService {
    constructor() {
        this.connection = null;
        this.connectionState = 'Disconnected';
        this.eventHandlers = new Map();
    }

    // Load SignalR library from CDN
    async loadSignalRLibrary() {
        try {
            // Check if SignalR is already loaded
            if (window.signalR && window.signalR.HubConnectionBuilder) {
                console.log('SignalR already loaded globally');
                return true;
            }

            // SignalR should be loaded via script tag in HTML
            console.log('SignalR library should be loaded via script tag');
            return true;
        } catch (error) {
            console.error('Error loading SignalR library:', error);
            return false;
        }
    }

    // Initialize SignalR connection
    async initialize(apiBaseUrl, accessToken) {
        try {
            if (this.connection) {
                await this.disconnect();
            }

            // Load SignalR library first
            const libraryLoaded = await this.loadSignalRLibrary();
            if (!libraryLoaded) {
                throw new Error('Failed to load SignalR library');
            }

            // Get SignalR from global scope
            const signalR = window.signalR;
            if (!signalR || !signalR.HubConnectionBuilder) {
                throw new Error('SignalR library not available after loading');
            }

            console.log('Creating SignalR connection...');
            console.log('API Base URL:', apiBaseUrl);
            console.log('Access Token:', accessToken ? 'Present' : 'Missing');
            
            // Ensure we use the correct protocol (ws:// for http, wss:// for https)
            const hubUrl = `${apiBaseUrl}/notificationHub?access_token=${accessToken}`;
            console.log('Hub URL:', hubUrl);

            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(hubUrl, {
                    skipNegotiation: true,
                    transport: signalR.HttpTransportType.WebSockets
                })
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: (retryContext) => {
                        if (retryContext.previousRetryCount < 3) {
                            return 2000; // 2 seconds for first 3 attempts
                        } else if (retryContext.previousRetryCount < 10) {
                            return 10000; // 10 seconds for next 7 attempts
                        } else {
                            return 30000; // 30 seconds for remaining attempts
                        }
                    }
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Set up event handlers
            this.connection.onclose((error) => {
                console.log('SignalR connection closed:', error);
                this.connectionState = 'Disconnected';
                this.emit('connectionStateChanged', { state: 'Disconnected', error: error?.message });
            });

            this.connection.onreconnecting((error) => {
                console.log('SignalR reconnecting:', error);
                this.connectionState = 'Reconnecting';
                this.emit('connectionStateChanged', { state: 'Reconnecting', error: error?.message });
            });

            this.connection.onreconnected((connectionId) => {
                console.log('SignalR reconnected:', connectionId);
                this.connectionState = 'Connected';
                this.emit('connectionStateChanged', { state: 'Connected' });
            });

            // Start the connection
            console.log('Starting SignalR connection...');
            await this.connection.start();
            
            console.log('SignalR connection started successfully');
            this.connectionState = 'Connected';
            this.emit('connectionStateChanged', { state: 'Connected' });
            
            return true;
        } catch (error) {
            console.error('Failed to initialize SignalR connection:', error);
            this.connectionState = 'Failed';
            this.emit('connectionStateChanged', { state: 'Failed', error: error.message });
            return false;
        }
    }

    // Disconnect SignalR
    async disconnect() {
        try {
            if (this.connection) {
                await this.connection.stop();
                this.connection = null;
                this.connectionState = 'Disconnected';
                this.emit('connectionStateChanged', { state: 'Disconnected' });
            }
        } catch (error) {
            console.error('Error disconnecting SignalR:', error);
        }
    }

    // Subscribe to notifications
    async subscribeToNotifications(notificationType) {
        try {
            if (this.connection && this.connectionState === 'Connected') {
                await this.connection.invoke('SubscribeToNotifications', notificationType);
                console.log(`Subscribed to ${notificationType} notifications`);
            }
        } catch (error) {
            console.error(`Error subscribing to ${notificationType} notifications:`, error);
        }
    }

    // Unsubscribe from notifications
    async unsubscribeFromNotifications(notificationType) {
        try {
            if (this.connection && this.connectionState === 'Connected') {
                await this.connection.invoke('UnsubscribeFromNotifications', notificationType);
                console.log(`Unsubscribed from ${notificationType} notifications`);
            }
        } catch (error) {
            console.error(`Error unsubscribing from ${notificationType} notifications:`, error);
        }
    }

    // Event handling
    on(event, handler) {
        if (!this.eventHandlers.has(event)) {
            this.eventHandlers.set(event, []);
        }
        this.eventHandlers.get(event).push(handler);
    }

    off(event, handler) {
        if (this.eventHandlers.has(event)) {
            const handlers = this.eventHandlers.get(event);
            const index = handlers.indexOf(handler);
            if (index > -1) {
                handlers.splice(index, 1);
            }
        }
    }

    emit(event, data) {
        if (this.eventHandlers.has(event)) {
            this.eventHandlers.get(event).forEach(handler => {
                try {
                    handler(data);
                } catch (error) {
                    console.error(`Error in event handler for ${event}:`, error);
                }
            });
        }
    }
}

// Create global instance
window.signalRNotificationService = new SignalRNotificationService();
"@

Set-Content "src\Inventory.UI\wwwroot\js\signalr-notifications.js" $signalrContent
Set-Content "src\Inventory.Web.Client\wwwroot\js\signalr-notifications.js" $signalrContent

Write-Host "Updated signalr-notifications.js for both projects" -ForegroundColor Green

Write-Host ""
Write-Host "Changes made:" -ForegroundColor Cyan
Write-Host "1. Updated getApiBaseUrl to use HTTPS port 7000 when web client is HTTPS" -ForegroundColor White
Write-Host "2. Updated getApiBaseUrl to use HTTP port 5000 when web client is HTTP" -ForegroundColor White
Write-Host "3. Improved SignalR connection logging" -ForegroundColor White
Write-Host "4. Better error handling for WebSocket connections" -ForegroundColor White
Write-Host ""
Write-Host "This should fix the WebSocket connection issues" -ForegroundColor Green
Write-Host "The app will now use the correct protocol (HTTP/HTTPS) for SignalR" -ForegroundColor Green
