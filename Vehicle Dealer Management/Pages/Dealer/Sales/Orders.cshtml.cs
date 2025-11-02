using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.Dealer.Sales
{
    public class OrdersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public OrdersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string StatusFilter { get; set; } = "all";
        public int TotalOrders { get; set; }
        public int OpenOrders { get; set; }
        public int PaidOrders { get; set; }
        public int DeliveredOrders { get; set; }

        public List<OrderViewModel> Orders { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string? status)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Set UserRole from Session for proper navigation
            ViewData["UserRole"] = HttpContext.Session.GetString("UserRole") ?? "DEALER_STAFF";
            ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "User";

            StatusFilter = status ?? "all";
            var dealerIdInt = int.Parse(dealerId);

            // Get orders
            var query = _context.SalesDocuments
                .Where(s => s.DealerId == dealerIdInt && s.Type == "ORDER")
                .Include(s => s.Customer)
                .Include(s => s.Lines)
                .Include(s => s.Payments)
                .AsQueryable();

            if (StatusFilter != "all")
            {
                query = query.Where(s => s.Status == StatusFilter);
            }

            var orders = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            // Calculate counts
            TotalOrders = await _context.SalesDocuments
                .CountAsync(s => s.DealerId == dealerIdInt && s.Type == "ORDER");
            OpenOrders = await _context.SalesDocuments
                .CountAsync(s => s.DealerId == dealerIdInt && s.Type == "ORDER" && s.Status == "OPEN");
            PaidOrders = await _context.SalesDocuments
                .CountAsync(s => s.DealerId == dealerIdInt && s.Type == "ORDER" && s.Status == "PAID");
            DeliveredOrders = await _context.SalesDocuments
                .CountAsync(s => s.DealerId == dealerIdInt && s.Type == "ORDER" && s.Status == "DELIVERED");

            Orders = orders.Select(o => new OrderViewModel
            {
                Id = o.Id,
                CustomerName = o.Customer?.FullName ?? "N/A",
                CustomerPhone = o.Customer?.Phone ?? "N/A",
                CreatedAt = o.CreatedAt,
                VehicleCount = (int)(o.Lines?.Sum(l => (decimal?)l.Qty) ?? 0),
                TotalAmount = o.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0,
                PaidAmount = o.Payments?.Sum(p => p.Amount) ?? 0,
                RemainingAmount = (o.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0) - (o.Payments?.Sum(p => p.Amount) ?? 0),
                Status = o.Status
            }).ToList();

            return Page();
        }

        public class OrderViewModel
        {
            public int Id { get; set; }
            public string CustomerName { get; set; } = "";
            public string CustomerPhone { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public int VehicleCount { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal RemainingAmount { get; set; }
            public string Status { get; set; } = "";
        }
    }
}

