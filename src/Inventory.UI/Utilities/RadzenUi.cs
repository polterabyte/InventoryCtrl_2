using Radzen;
using Inventory.Shared.Models;

namespace Inventory.UI.Utilities
{
    public static class RadzenUi
    {
        // Spacing defaults
        public const string GapSmall = "0.5rem";
        public const string Gap = "1rem";
        public const string GapLarge = "1.5rem";

        public static NotificationSeverity MapSeverity(NotificationType type) => type switch
        {
            NotificationType.Success => NotificationSeverity.Success,
            NotificationType.Error => NotificationSeverity.Error,
            NotificationType.Warning => NotificationSeverity.Warning,
            NotificationType.Info => NotificationSeverity.Info,
            _ => NotificationSeverity.Info
        };
    }
}
