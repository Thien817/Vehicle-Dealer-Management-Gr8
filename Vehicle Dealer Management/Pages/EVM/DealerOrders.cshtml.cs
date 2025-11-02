using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.EVM
{
    public class DealerOrdersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DealerOrdersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string StatusFilter { get; set; } = "SUBMITTED";
        public int SubmittedCount { get; set; }
        public int ApprovedCount { get; set; }
        public int FulfillingCount { get; set; }

        public List<DealerOrderViewModel> Orders { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string? status)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            StatusFilter = status ?? "SUBMITTED";

            // Get counts for tabs
            SubmittedCount = await _context.DealerOrders.CountAsync(o => o.Status == "SUBMITTED");
            ApprovedCount = await _context.DealerOrders.CountAsync(o => o.Status == "APPROVED");
            FulfillingCount = await _context.DealerOrders.CountAsync(o => o.Status == "FULFILLING");

            // Get orders based on filter
            var query = _context.DealerOrders
                .Include(o => o.Dealer)
                .Include(o => o.CreatedByUser)
                .AsQueryable();

            if (StatusFilter != "ALL")
            {
                query = query.Where(o => o.Status == StatusFilter);
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            Orders = orders.Select(o => new DealerOrderViewModel
            {
                Id = o.Id,
                DealerName = o.Dealer?.Name ?? "N/A",
                DealerCode = o.Dealer?.Code ?? "N/A",
                CreatedAt = o.CreatedAt,
                VehicleCount = 3, // Mock - parse from ItemsJson
                EstimatedValue = 500000000m, // Mock
                Status = o.Status,
                CreatedByName = o.CreatedByUser?.FullName ?? "N/A"
            }).ToList();

            return Page();
        }

        public class DealerOrderViewModel
        {
            public int Id { get; set; }
            public string DealerName { get; set; } = "";
            public string DealerCode { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public int VehicleCount { get; set; }
            public decimal EstimatedValue { get; set; }
            public string Status { get; set; } = "";
            public string CreatedByName { get; set; } = "";
        }
    }
}

