using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.IRepository;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.DAL.Repositories
{
    public class SalesDocumentRepository : Repository<SalesDocument>, ISalesDocumentRepository
    {
        private readonly ApplicationDbContext _context;

        public SalesDocumentRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<SalesDocument?> GetSalesDocumentWithDetailsAsync(int id)
        {
            return await _context.SalesDocuments
                .Include(s => s.Customer)
                .Include(s => s.Dealer)
                .Include(s => s.Promotion)
                .Include(s => s.CreatedByUser)
                .Include(s => s.Lines!)
                    .ThenInclude(l => l.Vehicle)
                .Include(s => s.Payments)
                .Include(s => s.Delivery)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<SalesDocument>> GetSalesDocumentsByDealerIdAsync(int dealerId, string? type = null, string? status = null)
        {
            var query = _context.SalesDocuments
                .Where(s => s.DealerId == dealerId)
                .Include(s => s.Customer)
                .Include(s => s.Lines!)
                    .ThenInclude(l => l.Vehicle)
                .Include(s => s.Payments)
                .Include(s => s.Delivery)
                .AsQueryable();

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(s => s.Type == type);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status == status);
            }

            return await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<SalesDocument>> GetSalesDocumentsByCustomerIdAsync(int customerId, string? type = null)
        {
            var query = _context.SalesDocuments
                .Where(s => s.CustomerId == customerId)
                .Include(s => s.Dealer)
                .Include(s => s.Lines!)
                    .ThenInclude(l => l.Vehicle)
                .Include(s => s.Payments)
                .Include(s => s.Delivery)
                .AsQueryable();

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(s => s.Type == type);
            }

            return await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<SalesDocument>> GetSalesDocumentsByDateRangeAsync(DateTime startDate, DateTime endDate, string? type = null)
        {
            var query = _context.SalesDocuments
                .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
                .Include(s => s.Customer)
                .Include(s => s.Dealer)
                .Include(s => s.Lines!)
                    .ThenInclude(l => l.Vehicle)
                .AsQueryable();

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(s => s.Type == type);
            }

            return await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> HasSalesDocumentLinesAsync(int vehicleId)
        {
            return await _context.SalesDocumentLines
                .AnyAsync(l => l.VehicleId == vehicleId);
        }
    }
}

