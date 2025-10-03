namespace Inventory.Shared.Models
{
    public class TokenManagementOptions
    {
        public int TokenExpirationMinutes { get; set; } = 15;
        public int RefreshTokenExpirationDays { get; set; } = 7;
        public int EarlyRefreshTokenMinutes { get; set; } = 2;
        public int RefreshThresholdMinutes { get; set; } = 5;
        public bool EnableLogging { get; set; } = true;
        public int MaxRefreshRetries { get; set; } = 3;
        public int RefreshRetryDelayMs { get; set; } = 1000;
    }
}
