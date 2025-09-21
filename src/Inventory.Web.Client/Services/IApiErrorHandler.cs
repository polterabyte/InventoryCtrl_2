using Inventory.Shared.DTOs;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Интерфейс для централизованной обработки ошибок API
/// </summary>
public interface IApiErrorHandler
{
    /// <summary>
    /// Обработать HTTP ответ и вернуть ApiResponse
    /// </summary>
    Task<ApiResponse<T>> HandleResponseAsync<T>(HttpResponseMessage response);
    
    /// <summary>
    /// Обработать HTTP ответ и вернуть PagedApiResponse
    /// </summary>
    Task<PagedApiResponse<T>> HandlePagedResponseAsync<T>(HttpResponseMessage response);
    
    /// <summary>
    /// Обработать исключение и вернуть ApiResponse
    /// </summary>
    Task<ApiResponse<T>> HandleExceptionAsync<T>(Exception exception, string operation);
    
    /// <summary>
    /// Обработать исключение и вернуть PagedApiResponse
    /// </summary>
    Task<PagedApiResponse<T>> HandlePagedExceptionAsync<T>(Exception exception, string operation);
}
