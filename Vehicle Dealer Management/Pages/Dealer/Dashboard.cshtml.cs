using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.Dealer
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserName { get; set; } = "";
        public string DealerName { get; set; } = "";
        public decimal TodaySales { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int TotalQuotes { get; set; }
        public int PendingQuotes { get; set; }
        public int TotalCustomers { get; set; }

        public List<OrderViewModel> RecentOrders { get; set; } = new();
        public List<TestDriveViewModel> TodayTestDrives { get; set; } = new();
        public List<QuoteViewModel> PendingQuotesList { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var dealerId = HttpContext.Session.GetString("DealerId");
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _context.Users
                .Include(u => u.Dealer)
                .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));

            if (user == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            UserName = user.FullName;
            DealerName = user.Dealer?.Name ?? "Đại lý";

            var dealerIdInt = int.Parse(dealerId);

            // Get statistics
            var allOrders = await _context.SalesDocuments
                .Where(s => s.DealerId == dealerIdInt && s.Type == "ORDER")
                .ToListAsync();

            TotalOrders = allOrders.Count;
            PendingOrders = allOrders.Count(o => o.Status == "OPEN" || o.Status == "PENDING");
            TodaySales = 125000000; // Mock data

            var allQuotes = await _context.SalesDocuments
                .Where(s => s.DealerId == dealerIdInt && s.Type == "QUOTE")
                .ToListAsync();

            TotalQuotes = allQuotes.Count;
            PendingQuotes = allQuotes.Count(q => q.Status == "DRAFT");

            // Get recent orders
            var recentOrders = await _context.SalesDocuments
                .Where(s => s.DealerId == dealerIdInt && s.Type == "ORDER")
                .Include(s => s.Customer)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToListAsync();

            RecentOrders = recentOrders.Select(o => new OrderViewModel
            {
                Id = o.Id,
                CustomerName = o.Customer?.FullName ?? "N/A",
                CreatedAt = o.CreatedAt,
                Status = o.Status,
                TotalAmount = 1500000000 // Mock
            }).ToList();

            // Get today's test drives
            var today = DateTime.Today;
            var testDrives = await _context.TestDrives
                .Where(t => t.DealerId == dealerIdInt && 
                            t.ScheduleTime.Date == today)
                .Include(t => t.Customer)
                .Include(t => t.Vehicle)
                .OrderBy(t => t.ScheduleTime)
                .ToListAsync();

            TodayTestDrives = testDrives.Select(t => new TestDriveViewModel
            {
                CustomerName = t.Customer?.FullName ?? "N/A",
                VehicleName = $"{t.Vehicle?.ModelName} {t.Vehicle?.VariantName}",
                Time = t.ScheduleTime.ToString("HH:mm"),
                Status = t.Status
            }).ToList();

            // Get pending quotes
            var pendingQuotes = await _context.SalesDocuments
                .Where(s => s.DealerId == dealerIdInt && s.Type == "QUOTE" && s.Status == "DRAFT")
                .Include(s => s.Customer)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToListAsync();

            PendingQuotesList = pendingQuotes.Select(q => new QuoteViewModel
            {
                Id = q.Id,
                CustomerName = q.Customer?.FullName ?? "N/A",
                CreatedAt = q.CreatedAt
            }).ToList();

            // Mock total customers
            TotalCustomers = 156;

            return Page();
        }

        public class OrderViewModel
        {
            public int Id { get; set; }
            public string CustomerName { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; } = "";
            public decimal TotalAmount { get; set; }
        }

        public class TestDriveViewModel
        {
            public string CustomerName { get; set; } = "";
            public string VehicleName { get; set; } = "";
            public string Time { get; set; } = "";
            public string Status { get; set; } = "";
        }

        public class QuoteViewModel
        {
            public int Id { get; set; }
            public string CustomerName { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }
    }
}

