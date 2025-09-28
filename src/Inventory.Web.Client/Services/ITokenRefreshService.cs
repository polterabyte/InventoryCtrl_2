using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Интерфейс для обновления JWT токенов без циклических зависимостей
/// </summary>
public interface ITokenRefreshService
{
    /// <summary>
    /// Обновляет JWT токен используя refresh токен
    /// </summary>
    /// <param name="refreshToken">Refresh токен</param>
    /// <returns>Результат обновления токена</returns>
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
}
