using System;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Исключение, которое выбрасывается когда токен был обновлен и запрос нужно повторить
/// </summary>
public class TokenRefreshedException : Exception
{
    public TokenRefreshedException(string message) : base(message)
    {
    }

    public TokenRefreshedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
