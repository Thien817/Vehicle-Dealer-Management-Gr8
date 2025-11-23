using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.IRepository;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.DAL.Repositories
{
    public class TestDriveRepository : Repository<TestDrive>, ITestDriveRepository
    {
        public TestDriveRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TestDrive>> GetTestDrivesByCustomerIdAsync(int customerId)
        {
            return await _context.TestDrives
                .Include(t => t.Customer)
                .Include(t => t.Dealer)
                .Include(t => t.Vehicle)
                .Include(t => t.ParentSlot) // Include parent slot if exists
                .Where(t => t.CustomerId == customerId && !t.IsSlot) // Only bookings
                .OrderByDescending(t => t.ScheduleTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<TestDrive>> GetTestDrivesByDealerIdAsync(int dealerId)
        {
            return await _context.TestDrives
                .Include(t => t.Customer)
                .Include(t => t.Dealer)
                .Include(t => t.Vehicle)
                .Include(t => t.ParentSlot) // Include parent slot if exists
                .Where(t => t.DealerId == dealerId && !t.IsSlot) // Only bookings
                .OrderByDescending(t => t.ScheduleTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<TestDrive>> GetTestDrivesByDealerAndDateAsync(int dealerId, DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            return await _context.TestDrives
                .Include(t => t.Customer)
                .Include(t => t.Dealer)
                .Include(t => t.Vehicle)
                .Include(t => t.ParentSlot) // Include parent slot if exists
                .Where(t => t.DealerId == dealerId 
                    && !t.IsSlot // Only bookings
                    && t.ScheduleTime >= startDate 
                    && t.ScheduleTime < endDate)
                .OrderBy(t => t.ScheduleTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<TestDrive>> GetTestDrivesByStatusAsync(string status, int? dealerId = null)
        {
            var query = _context.TestDrives
                .Include(t => t.Customer)
                .Include(t => t.Dealer)
                .Include(t => t.Vehicle)
                .Include(t => t.ParentSlot) // Include parent slot if exists
                .Where(t => t.Status == status && !t.IsSlot); // Only bookings

            if (dealerId.HasValue)
            {
                query = query.Where(t => t.DealerId == dealerId.Value);
            }

            return await query
                .OrderByDescending(t => t.ScheduleTime)
                .ToListAsync();
        }

        // ============ NEW: Slot Management Methods ============

        public async Task<IEnumerable<TestDrive>> GetSlotsByDealerAndDateAsync(int dealerId, DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            return await _context.TestDrives
                .Include(t => t.Dealer)
                .Where(t => t.DealerId == dealerId 
                    && t.IsSlot // Only slots
                    && t.ScheduleTime >= startDate 
                    && t.ScheduleTime < endDate)
                .OrderBy(t => t.SlotStartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<TestDrive>> GetAvailableSlotsByDealerAndDateAsync(int dealerId, DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);
            var now = DateTime.UtcNow;

            var slots = await _context.TestDrives
                .Include(t => t.Dealer)
                .Where(t => t.DealerId == dealerId 
                    && t.IsSlot // Only slots
                    && t.ScheduleTime >= startDate 
                    && t.ScheduleTime < endDate
                    && t.ScheduleTime >= now) // Only future slots
                .OrderBy(t => t.SlotStartTime)
                .ToListAsync();

            // Load booking counts for each slot
            foreach (var slot in slots)
            {
                slot.CurrentBookings = await GetSlotBookingCountAsync(slot.Id);
            }

            // Filter out full slots
            return slots.Where(s => !s.IsFull).ToList();
        }

        public async Task<TestDrive?> GetSlotByIdAsync(int slotId)
        {
            var slot = await _context.TestDrives
                .Include(t => t.Dealer)
                .FirstOrDefaultAsync(t => t.Id == slotId && t.IsSlot);

            if (slot != null)
            {
                slot.CurrentBookings = await GetSlotBookingCountAsync(slotId);
            }

            return slot;
        }

        public async Task<int> GetSlotBookingCountAsync(int slotId)
        {
            return await _context.TestDrives
                .Where(t => t.ParentSlotId == slotId && t.Status != "CANCELLED")
                .CountAsync();
        }

        public async Task<IEnumerable<TestDrive>> GetBookingsBySlotIdAsync(int slotId)
        {
            return await _context.TestDrives
                .Include(t => t.Customer)
                .Include(t => t.Vehicle)
                .Where(t => t.ParentSlotId == slotId)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}

