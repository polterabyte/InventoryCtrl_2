namespace Inventory.Shared.Models;

public class PushSubscriptionInfo
{
    public string Endpoint { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string SubscribedAt { get; set; } = string.Empty;
}
