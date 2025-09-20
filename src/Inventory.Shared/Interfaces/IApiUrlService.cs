namespace Inventory.Shared.Interfaces;

public interface IApiUrlService
{
    Task<string> GetApiBaseUrlAsync();
    Task<string> GetApiUrlAsync(string endpoint);
    Task<string> GetSignalRUrlAsync();
    Task<ApiUrlInfo> GetApiUrlInfoAsync();
}

public class ApiUrlInfo
{
    public string ApiUrl { get; set; } = string.Empty;
    public string SignalRUrl { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public bool IsDevelopment { get; set; }
    public DateTime Timestamp { get; set; }
}
