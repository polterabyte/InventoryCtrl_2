// Test SignalR loading
console.log('Testing SignalR loading...');

// Check various ways SignalR might be available
console.log('window.signalR:', window.signalR);
console.log('window.SignalR:', window.SignalR);
console.log('window["@microsoft/signalr"]:', window['@microsoft/signalr']);

// Check if it's available as a global
if (typeof signalR !== 'undefined') {
    console.log('signalR global:', signalR);
}

// Check all window properties that might contain SignalR
const signalProps = Object.keys(window).filter(key => 
    key.toLowerCase().includes('signal') || 
    key.includes('microsoft') ||
    key.includes('hub')
);
console.log('Signal-related properties:', signalProps);

// Try to access SignalR from different possible locations
try {
    const signalR = window.signalR || window.SignalR || window['@microsoft/signalr'] || signalR;
    if (signalR) {
        console.log('Found SignalR:', signalR);
        console.log('HubConnectionBuilder available:', typeof signalR.HubConnectionBuilder);
        console.log('LogLevel available:', typeof signalR.LogLevel);
    } else {
        console.log('SignalR not found in any expected location');
    }
} catch (error) {
    console.error('Error accessing SignalR:', error);
}
