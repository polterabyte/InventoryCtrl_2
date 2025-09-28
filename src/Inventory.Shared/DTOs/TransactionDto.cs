using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.DTOs;

public class InventoryTransactionDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}

public class CreateInventoryTransactionDto
{
    [Required(ErrorMessage = "Product ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Product ID must be greater than 0")]
    public int ProductId { get; set; }
    
    [Required(ErrorMessage = "Warehouse ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Warehouse ID must be greater than 0")]
    public int WarehouseId { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Location ID must be greater than 0")]
    public int? LocationId { get; set; }
    
    [Required(ErrorMessage = "Transaction type is required")]
    [RegularExpression("^(Pending|Income|Outcome|Install)$", ErrorMessage = "Transaction type must be Pending, Income, Outcome, or Install")]
    public string Type { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }
    
    public DateTime? Date { get; set; }
    
    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters")]
    public string? Description { get; set; }
}

public class UpdateInventoryTransactionDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Location ID must be greater than 0")]
    public int? LocationId { get; set; }
    
    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters")]
    public string? Description { get; set; }
}
