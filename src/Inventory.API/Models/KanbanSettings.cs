namespace Inventory.API.Models;

public class KanbanSettings
{
    public int Id { get; set; }
    public int DefaultMinThreshold { get; set; }
    public int DefaultMaxThreshold { get; set; }
    public DateTime UpdatedAt { get; set; }
}
