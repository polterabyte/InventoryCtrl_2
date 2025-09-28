namespace Inventory.API.Models;

public class RequestHistory
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public Request Request { get; set; } = null!;

    public RequestStatus OldStatus { get; set; }
    public RequestStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public string? Comment { get; set; }
}

