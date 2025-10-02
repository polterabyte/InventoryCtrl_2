using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;

namespace Inventory.Web.Client.Extensions;

/// <summary>
/// Методы расширения для работы с ответами API
/// </summary>
public static class ApiResponseExtensions
{
    /// <summary>
    /// Проверяет, был ли токен обновлен и требуется ли повторить запрос
    /// </summary>
    public static bool IsTokenRefreshed<T>(this ApiResponse<T> response)
    {
        return !response.Success && response.ErrorMessage == ApiResponseCodes.TokenRefreshed;
    }

    /// <summary>
    /// Проверяет, был ли токен обновлен и требуется ли повторить запрос
    /// </summary>
    public static bool IsTokenRefreshed<T>(this PagedApiResponse<T> response)
    {
        return !response.Success && response.ErrorMessage == ApiResponseCodes.TokenRefreshed;
    }
}