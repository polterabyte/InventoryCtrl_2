namespace Inventory.Shared.Constants;

/// <summary>
/// Константы для специальных кодов ответа API
/// </summary>
public static class ApiResponseCodes
{
    /// <summary>
    /// Токен был успешно обновлен, требуется повторить запрос
    /// </summary>
    public const string TokenRefreshed = "TOKEN_REFRESHED";
}