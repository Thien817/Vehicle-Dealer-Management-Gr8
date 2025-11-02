using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.BLL.IService
{
    public interface IActivityLogService
    {
        Task LogActivityAsync(
            int userId,
            string action,
            string entityType,
            int? entityId = null,
            string? entityName = null,
            string? description = null,
            string? userRole = null,
            string? ipAddress = null);

        Task<IEnumerable<ActivityLog>> GetLogsByUserIdAsync(int userId);
        Task<IEnumerable<ActivityLog>> GetLogsByActionAsync(string action);
        Task<IEnumerable<ActivityLog>> GetLogsByEntityTypeAsync(string entityType);
        Task<IEnumerable<ActivityLog>> GetRecentLogsAsync(int count = 100);
    }
}

