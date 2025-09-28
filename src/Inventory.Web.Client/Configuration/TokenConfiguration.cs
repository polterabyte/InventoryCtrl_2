namespace Inventory.Web.Client.Configuration;

/// <summary>
/// Конфигурация для управления JWT токенами
/// </summary>
public class TokenConfiguration
{
    public const string SectionName = "TokenSettings";

    /// <summary>
    /// За сколько минут до истечения токена начинать обновление
    /// </summary>
    public int RefreshThresholdMinutes { get; set; } = 5;

    /// <summary>
    /// Максимальное количество попыток обновления токена
    /// </summary>
    public int MaxRefreshRetries { get; set; } = 3;

    /// <summary>
    /// Задержка между попытками обновления в миллисекундах
    /// </summary>
    public int RefreshRetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Максимальное время ожидания обновления токена в миллисекундах
    /// </summary>
    public int RefreshTimeoutMs { get; set; } = 10000;

    /// <summary>
    /// Включить автоматическое обновление токенов
    /// </summary>
    public bool EnableAutoRefresh { get; set; } = true;

    /// <summary>
    /// Включить логирование операций с токенами
    /// </summary>
    public bool EnableLogging { get; set; } = true;
}
