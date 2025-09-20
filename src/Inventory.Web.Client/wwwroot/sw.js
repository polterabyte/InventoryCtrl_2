// Service Worker for Push Notifications
const CACHE_NAME = 'inventory-push-v1';
const VAPID_PUBLIC_KEY = 'YOUR_VAPID_PUBLIC_KEY_HERE'; // This will be replaced with actual key

// Install event
self.addEventListener('install', (event) => {
    console.log('Service Worker installing...');
    self.skipWaiting();
});

// Activate event
self.addEventListener('activate', (event) => {
    console.log('Service Worker activating...');
    event.waitUntil(self.clients.claim());
});

// Push event - handle incoming push notifications
self.addEventListener('push', (event) => {
    console.log('Push event received:', event);
    
    let notificationData = {
        title: 'Inventory Control System',
        body: 'You have a new notification',
        icon: '/favicon.png',
        badge: '/favicon.png',
        tag: 'inventory-notification',
        requireInteraction: false,
        data: {}
    };

    // Parse push data if available
    if (event.data) {
        try {
            const pushData = event.data.json();
            notificationData = {
                ...notificationData,
                ...pushData
            };
        } catch (error) {
            console.error('Error parsing push data:', error);
            notificationData.body = event.data.text() || notificationData.body;
        }
    }

    // Show notification
    event.waitUntil(
        self.registration.showNotification(notificationData.title, {
            body: notificationData.body,
            icon: notificationData.icon || '/favicon.png',
            badge: notificationData.badge || '/favicon.png',
            tag: notificationData.tag || 'inventory-notification',
            requireInteraction: notificationData.requireInteraction || false,
            data: notificationData.data || {},
            actions: notificationData.actions || [],
            url: notificationData.url
        })
    );
});

// Notification click event
self.addEventListener('notificationclick', (event) => {
    console.log('Notification clicked:', event);
    
    event.notification.close();
    
    // Handle notification click
    if (event.action) {
        // Handle specific action
        console.log('Action clicked:', event.action);
        handleNotificationAction(event.action, event.notification.data);
    } else {
        // Default click behavior
        const urlToOpen = event.notification.data?.url || '/';
        event.waitUntil(
            clients.matchAll({ type: 'window', includeUncontrolled: true })
                .then((clientList) => {
                    // Check if there's already a window/tab open with the target URL
                    for (const client of clientList) {
                        if (client.url.includes(urlToOpen) && 'focus' in client) {
                            return client.focus();
                        }
                    }
                    // If no existing window, open a new one
                    if (clients.openWindow) {
                        return clients.openWindow(urlToOpen);
                    }
                })
        );
    }
});

// Notification close event
self.addEventListener('notificationclose', (event) => {
    console.log('Notification closed:', event);
    // Track notification dismissal if needed
});

// Background sync event (if needed)
self.addEventListener('sync', (event) => {
    console.log('Background sync event:', event);
    if (event.tag === 'push-subscription-sync') {
        event.waitUntil(syncPushSubscription());
    }
});

// Handle notification actions
function handleNotificationAction(action, data) {
    switch (action) {
        case 'view':
            if (data?.url) {
                clients.openWindow(data.url);
            }
            break;
        case 'dismiss':
            // Notification already closed
            break;
        default:
            console.log('Unknown action:', action);
    }
}

// Sync push subscription (if needed)
async function syncPushSubscription() {
    try {
        // Implement subscription sync logic if needed
        console.log('Syncing push subscription...');
    } catch (error) {
        console.error('Error syncing push subscription:', error);
    }
}

// Message event - handle messages from main thread
self.addEventListener('message', (event) => {
    console.log('Service Worker received message:', event.data);
    
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});

// Error handling
self.addEventListener('error', (event) => {
    console.error('Service Worker error:', event.error);
});

self.addEventListener('unhandledrejection', (event) => {
    console.error('Service Worker unhandled rejection:', event.reason);
});
