using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.DAL.IRepository
{
    public interface IActivityLogRepository : IRepository<ActivityLog>
    {
        Task<IEnumerable<ActivityLog>> GetLogsByUserIdAsync(int userId);
        Task<IEnumerable<ActivityLog>> GetLogsByActionAsync(string action);
        Task<IEnumerable<ActivityLog>> GetLogsByEntityTypeAsync(string entityType);
        Task<IEnumerable<ActivityLog>> GetRecentLogsAsync(int count = 100);
    }
}

