// Push Notifications Client
class PushNotificationClient {
    constructor() {
        this.registration = null;
        this.subscription = null;
        this.isSupported = 'serviceWorker' in navigator && 'PushManager' in window;
        this.vapidPublicKey = null;
    }

    // Initialize push notifications
    async initialize(vapidPublicKey) {
        if (!this.isSupported) {
            console.warn('Push notifications are not supported in this browser');
            return false;
        }

        try {
            this.vapidPublicKey = vapidPublicKey;
            
            // Register service worker
            this.registration = await navigator.serviceWorker.register('/sw.js');
            console.log('Service Worker registered successfully');

            // Wait for service worker to be ready
            await navigator.serviceWorker.ready;
            console.log('Service Worker is ready');

            return true;
        } catch (error) {
            console.error('Failed to initialize push notifications:', error);
            return false;
        }
    }

    // Check if push notifications are supported
    isPushSupported() {
        return this.isSupported;
    }

    // Check if user has granted permission
    async getPermissionState() {
        if (!this.isSupported) return 'denied';
        
        try {
            const permission = await Notification.requestPermission();
            return permission;
        } catch (error) {
            console.error('Error checking notification permission:', error);
            return 'denied';
        }
    }

    // Request notification permission
    async requestPermission() {
        if (!this.isSupported) {
            throw new Error('Push notifications are not supported');
        }

        try {
            const permission = await Notification.requestPermission();
            return permission === 'granted';
        } catch (error) {
            console.error('Error requesting notification permission:', error);
            return false;
        }
    }

    // Subscribe to push notifications
    async subscribe() {
        if (!this.isSupported || !this.registration) {
            throw new Error('Push notifications not supported or not initialized');
        }

        try {
            // Check if already subscribed
            this.subscription = await this.registration.pushManager.getSubscription();
            
            if (this.subscription) {
                console.log('Already subscribed to push notifications');
                return this.subscription;
            }

            // Convert VAPID key to Uint8Array
            const applicationServerKey = this.urlBase64ToUint8Array(this.vapidPublicKey);

            // Subscribe to push notifications
            this.subscription = await this.registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: applicationServerKey
            });

            console.log('Successfully subscribed to push notifications');
            return this.subscription;
        } catch (error) {
            console.error('Failed to subscribe to push notifications:', error);
            throw error;
        }
    }

    // Unsubscribe from push notifications
    async unsubscribe() {
        if (!this.subscription) {
            console.log('No active subscription to unsubscribe');
            return true;
        }

        try {
            const result = await this.subscription.unsubscribe();
            this.subscription = null;
            console.log('Successfully unsubscribed from push notifications');
            return result;
        } catch (error) {
            console.error('Failed to unsubscribe from push notifications:', error);
            throw error;
        }
    }

    // Send subscription to server
    async sendSubscriptionToServer(subscription, apiBaseUrl, accessToken) {
        try {
            const response = await fetch(`${apiBaseUrl}/api/pushnotification/subscribe`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${accessToken}`
                },
                body: JSON.stringify({
                    endpoint: subscription.endpoint,
                    p256dh: this.arrayBufferToBase64(subscription.getKey('p256dh')),
                    auth: this.arrayBufferToBase64(subscription.getKey('auth'))
                })
            });

            if (response.ok) {
                console.log('Subscription sent to server successfully');
                return true;
            } else {
                console.error('Failed to send subscription to server:', response.statusText);
                return false;
            }
        } catch (error) {
            console.error('Error sending subscription to server:', error);
            return false;
        }
    }

    // Remove subscription from server
    async removeSubscriptionFromServer(endpoint, apiBaseUrl, accessToken) {
        try {
            const response = await fetch(`${apiBaseUrl}/api/pushnotification/unsubscribe`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${accessToken}`
                },
                body: JSON.stringify({ endpoint })
            });

            if (response.ok) {
                console.log('Subscription removed from server successfully');
                return true;
            } else {
                console.error('Failed to remove subscription from server:', response.statusText);
                return false;
            }
        } catch (error) {
            console.error('Error removing subscription from server:', error);
            return false;
        }
    }

    // Get current subscription
    async getCurrentSubscription() {
        if (!this.registration) return null;
        
        try {
            return await this.registration.pushManager.getSubscription();
        } catch (error) {
            console.error('Error getting current subscription:', error);
            return null;
        }
    }

    // Check if user is subscribed
    async isSubscribed() {
        const subscription = await this.getCurrentSubscription();
        return subscription !== null;
    }

    // Get subscription info for display
    async getSubscriptionInfo() {
        const subscription = await this.getCurrentSubscription();
        if (!subscription) return null;

        return {
            endpoint: subscription.endpoint,
            isActive: true,
            subscribedAt: new Date().toISOString()
        };
    }

    // Utility function to convert VAPID key
    urlBase64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding)
            .replace(/\-/g, '+')
            .replace(/_/g, '/');

        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);

        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    }

    // Utility function to convert ArrayBuffer to Base64
    arrayBufferToBase64(buffer) {
        const bytes = new Uint8Array(buffer);
        let binary = '';
        for (let i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return window.btoa(binary);
    }

    // Test push notification (for development)
    async testNotification() {
        if (!this.registration) {
            console.error('Service Worker not registered');
            return false;
        }

        try {
            // Send a test message to the service worker
            this.registration.active.postMessage({
                type: 'TEST_NOTIFICATION',
                data: {
                    title: 'Test Notification',
                    body: 'This is a test push notification',
                    icon: '/favicon.png'
                }
            });
            return true;
        } catch (error) {
            console.error('Error sending test notification:', error);
            return false;
        }
    }
}

// Create global instance
window.pushNotificationClient = new PushNotificationClient();

// Global functions for easy access
window.initializePushNotifications = async function(vapidPublicKey) {
    return await window.pushNotificationClient.initialize(vapidPublicKey);
};

window.subscribeToPushNotifications = async function(apiBaseUrl, accessToken) {
    const client = window.pushNotificationClient;
    
    // Request permission first
    const hasPermission = await client.requestPermission();
    if (!hasPermission) {
        throw new Error('Notification permission denied');
    }

    // Subscribe to push notifications
    const subscription = await client.subscribe();
    
    // Send subscription to server
    const success = await client.sendSubscriptionToServer(subscription, apiBaseUrl, accessToken);
    
    return success;
};

window.unsubscribeFromPushNotifications = async function(apiBaseUrl, accessToken) {
    const client = window.pushNotificationClient;
    
    // Get current subscription
    const subscription = await client.getCurrentSubscription();
    if (!subscription) {
        console.log('No active subscription to unsubscribe');
        return true;
    }

    // Remove from server first
    const serverSuccess = await client.removeSubscriptionFromServer(subscription.endpoint, apiBaseUrl, accessToken);
    
    // Then unsubscribe locally
    const localSuccess = await client.unsubscribe();
    
    return serverSuccess && localSuccess;
};

window.isPushNotificationSupported = function() {
    return window.pushNotificationClient.isPushSupported();
};

window.getPushNotificationPermission = async function() {
    return await window.pushNotificationClient.getPermissionState();
};

window.isPushNotificationSubscribed = async function() {
    return await window.pushNotificationClient.isSubscribed();
};

window.getPushSubscriptionInfo = async function() {
    return await window.pushNotificationClient.getSubscriptionInfo();
};

window.testPushNotification = async function() {
    return await window.pushNotificationClient.testNotification();
};

console.log('Push Notifications Client loaded');


