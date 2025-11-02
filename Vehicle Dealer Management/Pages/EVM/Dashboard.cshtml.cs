using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.EVM
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserName { get; set; } = "";
        public int TotalVehicles { get; set; }
        public int AvailableVehicles { get; set; }
        public int TotalStock { get; set; }
        public int TotalDealers { get; set; }
        public int ActiveDealers { get; set; }
        public int PendingDealerOrders { get; set; }

        public List<DealerOrderViewModel> DealerOrders { get; set; } = new();
        public List<StockViewModel> StockSummary { get; set; } = new();
        public List<DealerViewModel> ActiveDealersList { get; set; } = new();

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

            UserName = user.FullName;

            // Get vehicles
            TotalVehicles = await _context.Vehicles.CountAsync();
            AvailableVehicles = await _context.Vehicles.CountAsync(v => v.Status == "AVAILABLE");

            // Get EVM stock
            var evmStocks = await _context.Stocks
                .Where(s => s.OwnerType == "EVM")
                .Include(s => s.Vehicle)
                .ToListAsync();

            TotalStock = (int)evmStocks.Sum(s => (long)s.Qty);

            StockSummary = evmStocks
                .OrderByDescending(s => s.Qty)
                .Take(10)
                .Select(s => new StockViewModel
                {
                    VehicleName = $"{s.Vehicle?.ModelName} {s.Vehicle?.VariantName}",
                    Color = s.ColorCode,
                    Qty = (int)s.Qty
                })
                .ToList();

            // Get dealers
            TotalDealers = await _context.Dealers.CountAsync();
            ActiveDealers = await _context.Dealers.CountAsync(d => d.Status == "ACTIVE");

            var activeDealers = await _context.Dealers
                .Where(d => d.Status == "ACTIVE")
                .Take(5)
                .ToListAsync();

            ActiveDealersList = activeDealers.Select(d => new DealerViewModel
            {
                Name = d.Name,
                Address = d.Address,
                Status = d.Status
            }).ToList();

            // Get pending dealer orders
            var pendingOrders = await _context.DealerOrders
                .Where(o => o.Status == "SUBMITTED")
                .Include(o => o.Dealer)
                .OrderBy(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();

            PendingDealerOrders = pendingOrders.Count;

            DealerOrders = pendingOrders.Select(o => new DealerOrderViewModel
            {
                Id = o.Id,
                DealerName = o.Dealer?.Name ?? "N/A",
                CreatedAt = o.CreatedAt,
                VehicleCount = 3, // Mock - parse from ItemsJson
                Status = o.Status
            }).ToList();

            return Page();
        }

        public class DealerOrderViewModel
        {
            public int Id { get; set; }
            public string DealerName { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public int VehicleCount { get; set; }
            public string Status { get; set; } = "";
        }

        public class StockViewModel
        {
            public string VehicleName { get; set; } = "";
            public string Color { get; set; } = "";
            public int Qty { get; set; }
        }

        public class DealerViewModel
        {
            public string Name { get; set; } = "";
            public string Address { get; set; } = "";
            public string Status { get; set; } = "";
        }
    }
}

