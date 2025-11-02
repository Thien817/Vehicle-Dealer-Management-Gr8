using Vehicle_Dealer_Management.DAL.IRepository;
using Vehicle_Dealer_Management.DAL.Models;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.BLL.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IActivityLogRepository _activityLogRepository;

        public ActivityLogService(IActivityLogRepository activityLogRepository)
        {
            _activityLogRepository = activityLogRepository;
        }

        public async Task LogActivityAsync(
            int userId,
            string action,
            string entityType,
            int? entityId = null,
            string? entityName = null,
            string? description = null,
            string? userRole = null,
            string? ipAddress = null)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Description = description,
                UserRole = userRole,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            await _activityLogRepository.AddAsync(log);
        }

        public async Task<IEnumerable<ActivityLog>> GetLogsByUserIdAsync(int userId)
        {
            return await _activityLogRepository.GetLogsByUserIdAsync(userId);
        }

        public async Task<IEnumerable<ActivityLog>> GetLogsByActionAsync(string action)
        {
            return await _activityLogRepository.GetLogsByActionAsync(action);
        }

        public async Task<IEnumerable<ActivityLog>> GetLogsByEntityTypeAsync(string entityType)
        {
            return await _activityLogRepository.GetLogsByEntityTypeAsync(entityType);
        }

        public async Task<IEnumerable<ActivityLog>> GetRecentLogsAsync(int count = 100)
        {
            return await _activityLogRepository.GetRecentLogsAsync(count);
        }
    }
}

