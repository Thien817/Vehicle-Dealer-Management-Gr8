using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.IRepository;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.DAL.Repositories
{
    public class FeedbackRepository : Repository<Feedback>, IFeedbackRepository
    {
        public FeedbackRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Feedback>> GetFeedbacksByCustomerIdAsync(int customerId)
        {
            return await _context.Feedbacks
                .Where(f => f.CustomerId == customerId)
                .Include(f => f.Dealer)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Feedback>> GetFeedbacksByDealerIdAsync(int dealerId)
        {
            return await _context.Feedbacks
                .Where(f => f.DealerId == dealerId)
                .Include(f => f.Customer)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Feedback>> GetFeedbacksByTypeAsync(string type, int? dealerId = null)
        {
            var query = _context.Feedbacks.Where(f => f.Type == type);
            
            if (dealerId.HasValue)
            {
                query = query.Where(f => f.DealerId == dealerId.Value);
            }

            return await query
                .Include(f => f.Customer)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Feedback>> GetFeedbacksByStatusAsync(string status, int? dealerId = null)
        {
            var query = _context.Feedbacks.Where(f => f.Status == status);
            
            if (dealerId.HasValue)
            {
                query = query.Where(f => f.DealerId == dealerId.Value);
            }

            return await query
                .Include(f => f.Customer)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<Feedback?> GetReviewByOrderIdAsync(int orderId)
        {
            return await _context.Feedbacks
                .Where(f => f.Type == "REVIEW" && f.OrderId == orderId)
                .Include(f => f.Order)
                .Include(f => f.Customer)
                .Include(f => f.Dealer)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Feedback>> GetReviewsByDealerIdAsync(int dealerId)
        {
            return await _context.Feedbacks
                .Where(f => f.Type == "REVIEW" && f.DealerId == dealerId)
                .Include(f => f.Order)
                .Include(f => f.Customer)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<double> GetAverageRatingByDealerIdAsync(int dealerId)
        {
            var reviews = await _context.Feedbacks
                .Where(f => f.Type == "REVIEW" && f.DealerId == dealerId && f.Rating.HasValue)
                .Select(f => f.Rating!.Value)
                .ToListAsync();
            
            if (reviews.Count == 0)
                return 0;
            
            return reviews.Average();
        }
    }
}

