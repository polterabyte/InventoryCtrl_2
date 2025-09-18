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
            // Always disconnect first to ensure clean state
            if (this.connection) {
                console.log('Disconnecting existing SignalR connection...');
                await this.disconnect();
                // Wait a bit to ensure clean disconnection
                await new Promise(resolve => setTimeout(resolve, 100));
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
                console.log('Disconnecting SignalR connection...');
                // Check if connection is in a state that can be stopped
                if (this.connection.state === signalR.HubConnectionState.Connected || 
                    this.connection.state === signalR.HubConnectionState.Connecting) {
                    await this.connection.stop();
                }
                this.connection = null;
                this.connectionState = 'Disconnected';
                this.emit('connectionStateChanged', { state: 'Disconnected' });
                console.log('SignalR connection disconnected successfully');
            }
        } catch (error) {
            console.error('Error disconnecting SignalR:', error);
            // Force cleanup even if stop() fails
            this.connection = null;
            this.connectionState = 'Disconnected';
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

    // Check if connected
    isConnected() {
        return this.connection && this.connectionState === 'Connected';
    }

    // Get connection state
    getConnectionState() {
        return this.connectionState;
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
