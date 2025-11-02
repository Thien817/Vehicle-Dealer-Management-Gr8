using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.Pages.Customer
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string CustomerName { get; set; } = "";
        public int QuotesCount { get; set; }
        public int OrdersCount { get; set; }
        public int TestDrivesCount { get; set; }
        public int AvailableVehicles { get; set; }
        public List<OrderViewModel> RecentOrders { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            CustomerName = user.FullName;

            // Get customer profile
            var customerProfile = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customerProfile != null)
            {
                // Mock data for now (will be replaced with real queries later)
                QuotesCount = await _context.SalesDocuments
                    .Where(s => s.CustomerId == customerProfile.Id && s.Type == "QUOTE")
                    .CountAsync();

                OrdersCount = await _context.SalesDocuments
                    .Where(s => s.CustomerId == customerProfile.Id && s.Type == "ORDER")
                    .CountAsync();

                TestDrivesCount = await _context.TestDrives
                    .Where(t => t.CustomerId == customerProfile.Id)
                    .CountAsync();

                // Get recent orders (mock 3 orders for demo)
                var recentOrders = await _context.SalesDocuments
                    .Where(s => s.CustomerId == customerProfile.Id && s.Type == "ORDER")
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                RecentOrders = recentOrders.Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status,
                    TotalAmount = 1500000000 // Mock amount
                }).ToList();
            }

            AvailableVehicles = await _context.Vehicles.CountAsync(v => v.Status == "AVAILABLE");

            return Page();
        }

        public class OrderViewModel
        {
            public int Id { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; } = "";
            public decimal TotalAmount { get; set; }
        }
    }
}

