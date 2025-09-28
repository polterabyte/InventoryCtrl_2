namespace Inventory.Shared.DTOs;

public record RequestDto
(
    int Id,
    string Title,
    string? Description,
    string Status,
    DateTime CreatedAt
);

public class RequestDetailsDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public List<TransactionRow> Transactions { get; set; } = new();
    public List<HistoryRow> History { get; set; } = new();
}

public class TransactionRow
{
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public int? ProductId { get; set; }
    public int? WarehouseId { get; set; }
}

public class HistoryRow
{
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public string? Comment { get; set; }
}
