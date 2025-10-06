using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.DTOs;

/// <summary>
/// DTO for UserWarehouse entity representing user-warehouse assignment information
/// </summary>
public class UserWarehouseDto
{
    public string UserId { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string AccessLevel { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for assigning a warehouse to a user
/// </summary>
public class AssignWarehouseDto
{
    [Required(ErrorMessage = "Warehouse ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Warehouse ID must be a positive number")]
    public int WarehouseId { get; set; }

    [Required(ErrorMessage = "Access level is required")]
    [StringLength(20, ErrorMessage = "Access level must not exceed 20 characters")]
    [RegularExpression("^(Full|ReadOnly|Limited)$", ErrorMessage = "Access level must be Full, ReadOnly, or Limited")]
    public string AccessLevel { get; set; } = "Full";

    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// DTO for bulk assignment of users to a warehouse
/// </summary>
public class BulkAssignWarehousesDto
{
    [Required(ErrorMessage = "At least one user ID is required")]
    [MinLength(1, ErrorMessage = "At least one user ID must be provided")]
    public List<string> UserIds { get; set; } = new();

    [Required(ErrorMessage = "Warehouse ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Warehouse ID must be a positive number")]
    public int WarehouseId { get; set; }

    [Required(ErrorMessage = "Access level is required")]
    [StringLength(20, ErrorMessage = "Access level must not exceed 20 characters")]
    [RegularExpression("^(Full|ReadOnly|Limited)$", ErrorMessage = "Access level must be Full, ReadOnly, or Limited")]
    public string AccessLevel { get; set; } = "Full";

    public bool SetAsDefault { get; set; } = false;
}

/// <summary>
/// DTO for updating warehouse assignment details
/// </summary>
public class UpdateWarehouseAssignmentDto
{
    [Required(ErrorMessage = "Access level is required")]
    [StringLength(20, ErrorMessage = "Access level must not exceed 20 characters")]
    [RegularExpression("^(Full|ReadOnly|Limited)$", ErrorMessage = "Access level must be Full, ReadOnly, or Limited")]
    public string AccessLevel { get; set; } = "Full";

    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// DTO for warehouse assignment request with validation
/// </summary>
public class WarehouseAssignmentRequest
{
    [Required(ErrorMessage = "User ID is required")]
    [StringLength(450, ErrorMessage = "User ID must not exceed 450 characters")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Warehouse ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Warehouse ID must be a positive number")]
    public int WarehouseId { get; set; }

    [Required(ErrorMessage = "Access level is required")]
    [StringLength(20, ErrorMessage = "Access level must not exceed 20 characters")]
    [RegularExpression("^(Full|ReadOnly|Limited)$", ErrorMessage = "Access level must be Full, ReadOnly, or Limited")]
    public string AccessLevel { get; set; } = "Full";

    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// DTO for setting default warehouse
/// </summary>
public class SetDefaultWarehouseDto
{
    [Required(ErrorMessage = "Warehouse ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Warehouse ID must be a positive number")]
    public int WarehouseId { get; set; }
}