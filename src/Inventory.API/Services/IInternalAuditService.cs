using Inventory.API.Enums;
using System.Threading.Tasks;

namespace Inventory.API.Services
{
    public interface IInternalAuditService
    {
        Task LogDetailedChangeAsync(
            string entityName,
            string entityId,
            string action,
            ActionType actionType,
            string entityType,
            object? changes = null,
            string? requestId = null,
            string? description = null,
            string severity = "INFO",
            bool isSuccess = true,
            string? errorMessage = null);
    }
}
