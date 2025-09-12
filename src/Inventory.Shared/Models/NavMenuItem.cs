using Microsoft.AspNetCore.Components.Routing;

namespace Inventory.Shared.Models;

public class NavMenuItem
{
    public string Text { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;
}
