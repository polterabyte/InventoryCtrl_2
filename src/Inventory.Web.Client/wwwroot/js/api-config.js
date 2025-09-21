// API Configuration for SignalR
console.log('Loading API configuration...');

// Define getApiBaseUrl function immediately
window.getApiBaseUrl = function() {
    const origin = window.location.origin;
    
    // In Docker environment, API is served through nginx on the same port
    // So we use relative URLs which will work correctly
    return origin + '/api';
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
