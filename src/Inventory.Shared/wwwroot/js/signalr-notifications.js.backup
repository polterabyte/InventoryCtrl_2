// SignalR Notification Service
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
    }

    // Initialize SignalR connection
    async initialize(apiBaseUrl, accessToken) {
        try {
            if (this.connection) {
                await this.disconnect();
            }

            // Debug: Log what's available in window
            console.log('Window signalR:', window.signalR);
            console.log('Window SignalR:', window.SignalR);
            console.log('Window @microsoft/signalr:', window['@microsoft/signalr']);
            console.log('Available window properties:', Object.keys(window).filter(key => key.toLowerCase().includes('signal')));

            let HubConnectionBuilder = null;
            let LogLevel = null;

            // Try to import SignalR dynamically
            try {
                console.log('Attempting dynamic import of SignalR...');
                const signalRModule = await import('https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.js');
                console.log('SignalR module imported:', signalRModule);
                
                HubConnectionBuilder = signalRModule.HubConnectionBuilder;
                LogLevel = signalRModule.LogLevel;
                
                console.log('HubConnectionBuilder type:', typeof HubConnectionBuilder);
                console.log('LogLevel type:', typeof LogLevel);
            } catch (importError) {
                console.error('Dynamic import failed:', importError);
                
                // Fallback to global access
                const signalR = window.signalR || window.SignalR || window['@microsoft/signalr'];
                if (signalR) {
                    HubConnectionBuilder = signalR.HubConnectionBuilder;
                    LogLevel = signalR.LogLevel;
                    console.log('Using global SignalR');
                }
            }

            if (HubConnectionBuilder && typeof HubConnectionBuilder === 'function') {
                console.log('Creating HubConnectionBuilder...');
                this.connection = new HubConnectionBuilder()
                    .withUrl(`${apiBaseUrl}/notificationHub?access_token=${accessToken}`, {
                        skipNegotiation: true,
                        transport: 1 // WebSockets
                    })
            } else {
                console.error('HubConnectionBuilder not found or not a function:', HubConnectionBuilder);
                throw new Error('HubConnectionBuilder not found. SignalR library may not be loaded correctly.');
            }
            
            this.connection = this.connection
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
                .configureLogging(LogLevel ? LogLevel.Information : 1)
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
