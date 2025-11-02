using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.IRepository;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.DAL.Repositories
{
    public class ActivityLogRepository : Repository<ActivityLog>, IActivityLogRepository
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ActivityLog>> GetLogsByUserIdAsync(int userId)
        {
            return await _dbSet
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivityLog>> GetLogsByActionAsync(string action)
        {
            return await _dbSet
                .Where(l => l.Action == action)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivityLog>> GetLogsByEntityTypeAsync(string entityType)
        {
            return await _dbSet
                .Where(l => l.EntityType == entityType)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivityLog>> GetRecentLogsAsync(int count = 100)
        {
            return await _dbSet
                .OrderByDescending(l => l.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}

