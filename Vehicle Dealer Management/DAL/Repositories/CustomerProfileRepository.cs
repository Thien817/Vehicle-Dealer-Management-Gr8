using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;
using Vehicle_Dealer_Management.DAL.IRepository;

namespace Vehicle_Dealer_Management.DAL.Repositories
{
    public class CustomerProfileRepository : Repository<CustomerProfile>, ICustomerProfileRepository
    {
        private readonly ApplicationDbContext _context;

        public CustomerProfileRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<CustomerProfile?> GetByUserIdAsync(int userId)
        {
            return await _context.CustomerProfiles
                .Include(cp => cp.User)
                .FirstOrDefaultAsync(cp => cp.UserId == userId);
        }

        public async Task<IEnumerable<CustomerProfile>> SearchByNameOrPhoneAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await _context.CustomerProfiles.ToListAsync();
            }

            return await _context.CustomerProfiles
                .Where(cp => cp.FullName.Contains(searchTerm) ||
                            cp.Phone.Contains(searchTerm) ||
                            (cp.Email != null && cp.Email.Contains(searchTerm)))
                .ToListAsync();
        }

        public async Task<bool> ExistsByUserIdAsync(int userId)
        {
            return await _context.CustomerProfiles.AnyAsync(cp => cp.UserId == userId);
        }
    }
}