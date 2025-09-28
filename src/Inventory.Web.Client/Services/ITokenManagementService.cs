using System.Threading.Tasks;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Сервис для управления JWT токенами
/// </summary>
public interface ITokenManagementService
{
    /// <summary>
    /// Проверяет, истекает ли токен в ближайшее время
    /// </summary>
    /// <returns>True, если токен истекает в течение настроенного времени</returns>
    Task<bool> IsTokenExpiringSoonAsync();

    /// <summary>
    /// Пытается обновить access токен используя refresh токен
    /// </summary>
    /// <returns>True, если обновление прошло успешно</returns>
    Task<bool> TryRefreshTokenAsync();

    /// <summary>
    /// Очищает все сохраненные токены
    /// </summary>
    Task ClearTokensAsync();

    /// <summary>
    /// Получает сохраненный access токен
    /// </summary>
    /// <returns>Access токен или null, если не найден</returns>
    Task<string?> GetStoredTokenAsync();

    /// <summary>
    /// Сохраняет новые токены после успешного обновления
    /// </summary>
    /// <param name="accessToken">Новый access токен</param>
    /// <param name="refreshToken">Новый refresh токен</param>
    Task SaveTokensAsync(string accessToken, string refreshToken);

    /// <summary>
    /// Проверяет, есть ли валидный refresh токен
    /// </summary>
    /// <returns>True, если refresh токен существует и не истек</returns>
    Task<bool> HasValidRefreshTokenAsync();
}
