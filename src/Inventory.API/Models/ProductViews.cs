namespace Inventory.API.Models;

public class ProductPendingView
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int PendingQty { get; set; }
    public DateTime? FirstPendingDate { get; set; }
    public DateTime? LastPendingDate { get; set; }
}

public class ProductOnHandView
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int OnHandQty { get; set; }
}

public class ProductInstalledView
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public int InstalledQty { get; set; }
    public DateTime? FirstInstallDate { get; set; }
    public DateTime? LastInstallDate { get; set; }
}


