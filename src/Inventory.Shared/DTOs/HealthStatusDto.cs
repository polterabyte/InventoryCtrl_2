namespace Inventory.Shared.DTOs;

public class HealthStatusDto
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
