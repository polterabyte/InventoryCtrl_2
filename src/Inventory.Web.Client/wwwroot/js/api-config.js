// API Configuration for SignalR
console.log('Loading API configuration...');

// Simplified API URL function - now delegates to C# service
window.getApiBaseUrl = function() {
    // This function is now primarily used as a fallback
    // The main logic is handled by ApiUrlService in C#
    const origin = window.location.origin;
    const port = window.location.port;
    
    // Simple fallback logic for development
    if (origin.includes('localhost')) {
        return origin.replace(port || '5001', '7000');
    }
    
    // For production, return relative path (will be handled by C# service)
    return '/api';
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
