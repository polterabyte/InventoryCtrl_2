// SignalR Notification Service - Fixed Version
class SignalRNotificationService {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 1000; // Start with 1 second
        this.maxReconnectDelay = 30000; // Max 30 seconds
        this.eventHandlers = new Map();
        this.connectionState = 'Disconnected';
        this.signalRLoaded = false;
    }

    // Load SignalR library
    async loadSignalRLibrary() {
        if (this.signalRLoaded) {
            return true;
        }

        try {
            // Check if SignalR is already loaded globally
            if (window.signalR && window.signalR.HubConnectionBuilder) {
                console.log('SignalR already loaded globally');
                this.signalRLoaded = true;
                return true;
            }

            // Try to load from CDN
            console.log('Loading SignalR from CDN...');
            const script = document.createElement('script');
            script.src = 'https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js';
            script.async = true;
            
            return new Promise((resolve, reject) => {
                script.onload = () => {
                    console.log('SignalR library loaded from CDN');
                    this.signalRLoaded = true;
                    resolve(true);
                };
                script.onerror = (error) => {
                    console.error('Failed to load SignalR from CDN:', error);
                    reject(error);
                };
                document.head.appendChild(script);
            });
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
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(`${apiBaseUrl}/notificationHub`, {
                    accessTokenFactory: () => accessToken,
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

            this.setupEventHandlers();
            await this.start();
            
            return true;
        } catch (error) {
            console.error('Failed to initialize SignalR connection:', error);
            this.connectionState = 'Failed';
            return false;
        }
    }

    // Setup event handlers
    setupEventHandlers() {
        // Connection events
        this.connection.onclose((error) => {
            console.log('SignalR connection closed:', error);
            this.isConnected = false;
            this.connectionState = 'Disconnected';
            this.emit('connectionStateChanged', { state: 'Disconnected', error });
        });

        this.connection.onreconnecting((error) => {
            console.log('SignalR reconnecting:', error);
            this.connectionState = 'Reconnecting';
            this.emit('connectionStateChanged', { state: 'Reconnecting', error });
        });

        this.connection.onreconnected((connectionId) => {
            console.log('SignalR reconnected:', connectionId);
            this.isConnected = true;
            this.connectionState = 'Connected';
            this.reconnectAttempts = 0;
            this.reconnectDelay = 1000;
            this.emit('connectionStateChanged', { state: 'Connected', connectionId });
        });

        // Notification events
        this.connection.on('ReceiveNotification', (notification) => {
            console.log('Received notification:', notification);
            this.emit('notificationReceived', notification);
        });

        this.connection.on('ConnectionEstablished', (data) => {
            console.log('Connection established:', data);
            this.isConnected = true;
            this.connectionState = 'Connected';
            this.emit('connectionEstablished', data);
        });
    }

    // Start connection
    async start() {
        try {
            await this.connection.start();
            this.isConnected = true;
            this.connectionState = 'Connected';
            this.reconnectAttempts = 0;
            this.reconnectDelay = 1000;
            console.log('SignalR connection started');
            this.emit('connectionStateChanged', { state: 'Connected' });
        } catch (error) {
            console.error('Error starting SignalR connection:', error);
            this.connectionState = 'Failed';
            this.emit('connectionStateChanged', { state: 'Failed', error });
            throw error;
        }
    }

    // Disconnect
    async disconnect() {
        if (this.connection) {
            try {
                await this.connection.stop();
                this.isConnected = false;
                this.connectionState = 'Disconnected';
                console.log('SignalR connection stopped');
                this.emit('connectionStateChanged', { state: 'Disconnected' });
            } catch (error) {
                console.error('Error stopping SignalR connection:', error);
            }
        }
    }

    // Subscribe to notification types
    async subscribeToNotifications(notificationType) {
        if (this.connection && this.isConnected) {
            try {
                await this.connection.invoke('SubscribeToNotifications', notificationType);
                console.log(`Subscribed to ${notificationType} notifications`);
            } catch (error) {
                console.error(`Failed to subscribe to ${notificationType} notifications:`, error);
            }
        }
    }

    // Unsubscribe from notification types
    async unsubscribeFromNotifications(notificationType) {
        if (this.connection && this.isConnected) {
            try {
                await this.connection.invoke('UnsubscribeFromNotifications', notificationType);
                console.log(`Unsubscribed from ${notificationType} notifications`);
            } catch (error) {
                console.error(`Failed to unsubscribe from ${notificationType} notifications:`, error);
            }
        }
    }

    // Join a group
    async joinGroup(groupName) {
        if (this.connection && this.isConnected) {
            try {
                await this.connection.invoke('JoinGroup', groupName);
                console.log(`Joined group: ${groupName}`);
            } catch (error) {
                console.error(`Failed to join group ${groupName}:`, error);
            }
        }
    }

    // Leave a group
    async leaveGroup(groupName) {
        if (this.connection && this.isConnected) {
            try {
                await this.connection.invoke('LeaveGroup', groupName);
                console.log(`Left group: ${groupName}`);
            } catch (error) {
                console.error(`Failed to leave group ${groupName}:`, error);
            }
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

    // Get connection state
    getConnectionState() {
        return {
            isConnected: this.isConnected,
            state: this.connectionState,
            connectionId: this.connection?.connectionId
        };
    }

    // Check if connected
    isConnectionActive() {
        return this.isConnected && this.connection?.state === 1; // Connected state
    }
}

// Global instance
window.signalRNotificationService = new SignalRNotificationService();

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SignalRNotificationService;
}
