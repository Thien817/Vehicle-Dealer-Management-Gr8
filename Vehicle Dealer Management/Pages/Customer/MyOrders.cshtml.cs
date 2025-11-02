using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.Customer
{
    public class MyOrdersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MyOrdersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<OrderViewModel> Orders { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Get customer profile
            var customerProfile = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == int.Parse(userId));

            if (customerProfile == null)
            {
                return Page();
            }

            // Get orders
            var orders = await _context.SalesDocuments
                .Where(s => s.CustomerId == customerProfile.Id && s.Type == "ORDER")
                .Include(s => s.Lines)
                .Include(s => s.Payments)
                .Include(s => s.Delivery)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            Orders = orders.Select(o => new OrderViewModel
            {
                Id = o.Id,
                CreatedAt = o.CreatedAt,
                VehicleCount = (int)(o.Lines?.Sum(l => (decimal?)l.Qty) ?? 0),
                TotalAmount = o.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0,
                PaidAmount = o.Payments?.Sum(p => p.Amount) ?? 0,
                RemainingAmount = (o.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0) - (o.Payments?.Sum(p => p.Amount) ?? 0),
                Status = o.Status,
                DeliveryDate = o.Delivery?.ScheduledDate
            }).ToList();

            return Page();
        }

        public class OrderViewModel
        {
            public int Id { get; set; }
            public DateTime CreatedAt { get; set; }
            public int VehicleCount { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal RemainingAmount { get; set; }
            public string Status { get; set; } = "";
            public DateTime? DeliveryDate { get; set; }
        }
    }
}

