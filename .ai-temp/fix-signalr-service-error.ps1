# Fix SignalR Service Error
Write-Host "Fixing SignalR service error..." -ForegroundColor Green

# Update api-config.js to add better error handling
Write-Host "Updating api-config.js with better error handling..." -ForegroundColor Yellow
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

# Update RealTimeNotificationComponent to wait for services
Write-Host "Updating RealTimeNotificationComponent..." -ForegroundColor Yellow
$componentContent = @"
@using Microsoft.JSInterop
@using Inventory.Shared.DTOs
@using System.Text.Json
@using Microsoft.Extensions.Logging
@using Blazored.LocalStorage
@using Microsoft.AspNetCore.Components.Authorization
@inject IJSRuntime JSRuntime
@inject ILogger<RealTimeNotificationComponent> Logger
@inject ILocalStorageService LocalStorage
@inject AuthenticationStateProvider AuthStateProvider

<div class="real-time-notifications">
    @if (IsConnected)
    {
        <div class="connection-status connected">
            <i class="fas fa-circle text-success"></i>
            <span>Real-time notifications active</span>
        </div>
    }
    else if (IsReconnecting)
    {
        <div class="connection-status reconnecting">
            <i class="fas fa-circle text-warning"></i>
            <span>Reconnecting...</span>
        </div>
    }
    else
    {
        <div class="connection-status disconnected">
            <i class="fas fa-circle text-danger"></i>
            <span>@(string.IsNullOrEmpty(accessToken) ? "Sign in to enable notifications" : "Real-time notifications offline")</span>
        </div>
    }

    @if (ShowNotifications)
    {
        <div class="notifications-container">
            @foreach (var notification in Notifications.Take(MaxVisibleNotifications))
            {
                <div class="notification-item @GetNotificationClass(notification.Type)" 
                     data-notification-id="@notification.Id">
                    <div class="notification-content">
                        <div class="notification-header">
                            <h6 class="notification-title">@notification.Title</h6>
                            <button class="btn-close" @onclick="() => DismissNotification(notification.Id)"></button>
                        </div>
                        <p class="notification-message">@notification.Message</p>
                        @if (!string.IsNullOrEmpty(notification.ActionUrl) && !string.IsNullOrEmpty(notification.ActionText))
                        {
                            <a href="@notification.ActionUrl" class="btn btn-sm btn-outline-primary notification-action">
                                @notification.ActionText
                            </a>
                        }
                    </div>
                    <div class="notification-meta">
                        <small class="text-muted">@notification.CreatedAt.ToString("HH:mm")</small>
                    </div>
                </div>
            }
        </div>
    }
</div>

@code {
    [Parameter] public bool ShowNotifications { get; set; } = true;
    [Parameter] public int MaxVisibleNotifications { get; set; } = 5;
    [Parameter] public int AutoDismissDelay { get; set; } = 5000; // 5 seconds
    [Parameter] public EventCallback<NotificationDto> OnNotificationReceived { get; set; }
    [Parameter] public EventCallback<string> OnConnectionStateChanged { get; set; }

    private List<NotificationDto> Notifications { get; set; } = new();
    private bool IsConnected { get; set; } = false;
    private bool IsReconnecting { get; set; } = false;
    private string ConnectionState { get; set; } = "Disconnected";
    private string? accessToken { get; set; }
    private DotNetObjectReference<RealTimeNotificationComponent>? dotNetRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            dotNetRef = DotNetObjectReference.Create(this);
            // Wait for JavaScript services to be available
            await WaitForJavaScriptServices();
            await InitializeSignalR();
        }
    }

    private async Task WaitForJavaScriptServices()
    {
        var maxAttempts = 10;
        var attempt = 0;
        
        while (attempt < maxAttempts)
        {
            try
            {
                // Check if all required functions are available
                var isGetApiBaseUrlAvailable = await JSRuntime.InvokeAsync<bool>("eval", "typeof window.getApiBaseUrl === 'function'");
                var isSignalRServiceAvailable = await JSRuntime.InvokeAsync<bool>("eval", "typeof window.signalRNotificationService === 'object'");
                var isInitializeFunctionAvailable = await JSRuntime.InvokeAsync<bool>("eval", "typeof window.initializeSignalRConnection === 'function'");
                
                if (isGetApiBaseUrlAvailable && isSignalRServiceAvailable && isInitializeFunctionAvailable)
                {
                    Logger.LogInformation("All JavaScript services are available");
                    return;
                }
                
                Logger.LogInformation("Waiting for JavaScript services... Attempt {Attempt}/{MaxAttempts}", attempt + 1, maxAttempts);
                await Task.Delay(200); // Wait 200ms before next attempt
                attempt++;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error checking JavaScript services availability");
                await Task.Delay(200);
                attempt++;
            }
        }
        
        Logger.LogWarning("JavaScript services not available after {MaxAttempts} attempts", maxAttempts);
    }

    private async Task InitializeSignalR()
    {
        try
        {
            // Get API base URL from JavaScript
            var apiBaseUrl = await JSRuntime.InvokeAsync<string>("getApiBaseUrl");
            
            // Get access token from LocalStorage (same key as CustomAuthenticationStateProvider uses)
            accessToken = await LocalStorage.GetItemAsStringAsync("authToken");

            if (string.IsNullOrEmpty(apiBaseUrl) || string.IsNullOrEmpty(accessToken))
            {
                Logger.LogWarning("API base URL or access token not available for SignalR connection. ApiBaseUrl: {ApiBaseUrl}, HasToken: {HasToken}", 
                    apiBaseUrl, !string.IsNullOrEmpty(accessToken));
                StateHasChanged(); // Update UI to show appropriate message
                return;
            }

            // Initialize SignalR connection
            var success = await JSRuntime.InvokeAsync<bool>("initializeSignalRConnection", apiBaseUrl, accessToken, dotNetRef);
            
            if (success)
            {
                Logger.LogInformation("SignalR connection initialized successfully");
            }
            else
            {
                Logger.LogError("Failed to initialize SignalR connection");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing SignalR connection");
        }
    }

    // Listen for authentication state changes to reconnect SignalR
    protected override async Task OnInitializedAsync()
    {
        AuthStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> authStateTask)
    {
        var authState = await authStateTask;
        var isAuthenticated = authState.User.Identity?.IsAuthenticated == true;
        
        if (isAuthenticated)
        {
            // User logged in, try to initialize SignalR
            await InitializeSignalR();
        }
        else
        {
            // User logged out, disconnect SignalR
            await DisconnectSignalR();
        }
        
        StateHasChanged();
    }

    private async Task DisconnectSignalR()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("disconnectSignalR");
            IsConnected = false;
            ConnectionState = "Disconnected";
            accessToken = null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disconnecting SignalR");
        }
    }

    [JSInvokable]
    public void OnNotificationReceivedJS(NotificationDto notification)
    {
        try
        {
            Notifications.Insert(0, notification);
            
            // Keep only the maximum number of notifications
            if (Notifications.Count > MaxVisibleNotifications * 2)
            {
                Notifications = Notifications.Take(MaxVisibleNotifications * 2).ToList();
            }

            // Auto-dismiss after delay
            if (AutoDismissDelay > 0)
            {
                _ = Task.Delay(AutoDismissDelay).ContinueWith(_ => 
                {
                    InvokeAsync(() => DismissNotification(notification.Id));
                });
            }

            // Notify parent component
            OnNotificationReceived.InvokeAsync(notification);
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling received notification");
        }
    }

    [JSInvokable]
    public void OnConnectionStateChangedJS(string state, string? error = null)
    {
        try
        {
            ConnectionState = state;
            IsConnected = state == "Connected";
            IsReconnecting = state == "Reconnecting";
            
            OnConnectionStateChanged.InvokeAsync(state);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling connection state change");
        }
    }

    private void DismissNotification(int notificationId)
    {
        Notifications.RemoveAll(n => n.Id == notificationId);
        StateHasChanged();
    }

    private string GetNotificationClass(string type)
    {
        return type?.ToUpper() switch
        {
            "SUCCESS" => "notification-success",
            "WARNING" => "notification-warning",
            "ERROR" => "notification-error",
            "INFO" => "notification-info",
            _ => "notification-info"
        };
    }

    public async Task SubscribeToNotificationType(string notificationType)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("subscribeToNotificationType", notificationType);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error subscribing to notification type: {NotificationType}", notificationType);
        }
    }

    public async Task UnsubscribeFromNotificationType(string notificationType)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("unsubscribeFromNotificationType", notificationType);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error unsubscribing from notification type: {NotificationType}", notificationType);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            AuthStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
            await JSRuntime.InvokeVoidAsync("disconnectSignalR");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disconnecting SignalR");
        }
        finally
        {
            dotNetRef?.Dispose();
        }
    }
}
"@

Set-Content "src\Inventory.UI\Components\RealTimeNotificationComponent.razor" $componentContent

Write-Host "Fixed SignalR service error!" -ForegroundColor Green
Write-Host ""
Write-Host "Changes made:" -ForegroundColor Cyan
Write-Host "1. Added check for signalRNotificationService availability" -ForegroundColor White
Write-Host "2. Added WaitForJavaScriptServices() method" -ForegroundColor White
Write-Host "3. Better error handling in all JavaScript functions" -ForegroundColor White
Write-Host "4. Improved logging for debugging" -ForegroundColor White
Write-Host ""
Write-Host "This should fix the 'Cannot read properties of undefined' error" -ForegroundColor Green
