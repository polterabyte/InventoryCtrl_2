namespace Inventory.Web.Client.Services;

/// <summary>
/// Сервис для построения и валидации API URL
/// </summary>
public interface IUrlBuilderService
{
    /// <summary>
    /// Построить полный API URL для указанного endpoint
    /// </summary>
    Task<string> BuildApiUrlAsync(string endpoint);
    
    /// <summary>
    /// Построить полный URL с валидацией и исправлением
    /// </summary>
    Task<string> BuildFullUrlAsync(string endpoint);
    
    /// <summary>
    /// Валидировать и исправить URL если необходимо
    /// </summary>
    Task<string> ValidateAndFixUrlAsync(string url);
}
