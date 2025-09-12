namespace Inventory.Shared.DTOs;

public class ManufacturerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContactInfo { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateManufacturerDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContactInfo { get; set; }
    public string? Website { get; set; }
}

public class UpdateManufacturerDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContactInfo { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; } = true;
}
