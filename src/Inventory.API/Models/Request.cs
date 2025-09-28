namespace Inventory.API.Models;

public enum RequestStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    InProgress = 3,
    ItemsReceived = 4,
    ItemsInstalled = 5,
    Completed = 6,
    Cancelled = 7,
    Rejected = 8
}

public class Request
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Draft;

    public string CreatedByUserId { get; set; } = string.Empty;
    public string? ApprovedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<RequestHistory> History { get; set; } = new List<RequestHistory>();
}

