using Inventory.Shared.DTOs;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Сервис для автоматического обновления токена и повторения запросов
/// </summary>
public interface IAutoTokenRefreshService
{
    /// <summary>
    /// Выполняет HTTP запрос с автоматическим обновлением токена при необходимости
    /// </summary>
    /// <typeparam name="T">Тип результата</typeparam>
    /// <param name="apiCall">Делегат для выполнения API запроса</param>
    /// <returns>Результат запроса после всех необходимых повторов</returns>
    Task<ApiResponse<T>> ExecuteWithAutoRefreshAsync<T>(Func<Task<ApiResponse<T>>> apiCall);

    /// <summary>
    /// Выполняет HTTP запрос с пагинацией с автоматическим обновлением токена при необходимости
    /// </summary>
    /// <typeparam name="T">Тип результата</typeparam>
    /// <param name="apiCall">Делегат для выполнения API запроса</param>
    /// <returns>Результат запроса после всех необходимых повторов</returns>
    Task<PagedApiResponse<T>> ExecutePagedWithAutoRefreshAsync<T>(Func<Task<PagedApiResponse<T>>> apiCall);
}