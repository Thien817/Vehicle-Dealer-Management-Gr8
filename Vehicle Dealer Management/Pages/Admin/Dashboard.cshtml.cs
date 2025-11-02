using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserName { get; set; } = "";
        public decimal MonthSales { get; set; }
        public int TotalSoldVehicles { get; set; }
        public int ActiveDealers { get; set; }
        public int TotalDealers { get; set; }
        public int TotalInventory { get; set; }
        public int ActiveUsers { get; set; }

        public List<DealerPerformanceViewModel> TopDealers { get; set; } = new();
        public List<VehicleConsumptionViewModel> ConsumptionSpeed { get; set; } = new();
        public List<StockAlertViewModel> LowStockAlerts { get; set; } = new();

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

            // Mock high-level statistics
            MonthSales = 12500000000; // 12.5 billion
            TotalSoldVehicles = 185;
            ActiveUsers = 45;

            // Get dealers
            TotalDealers = await _context.Dealers.CountAsync();
            ActiveDealers = await _context.Dealers.CountAsync(d => d.Status == "ACTIVE");

            // Get total inventory
            TotalInventory = (int)(await _context.Stocks.SumAsync(s => (int?)s.Qty) ?? 0);

            // Mock top dealers
            TopDealers = new List<DealerPerformanceViewModel>
            {
                new() { Name = "Đại lý Hà Nội", Region = "Miền Bắc", OrderCount = 45, Sales = 3800000000, TargetPercent = 95, Rank = 1 },
                new() { Name = "Đại lý TP.HCM", Region = "Miền Nam", OrderCount = 42, Sales = 3600000000, TargetPercent = 90, Rank = 2 },
                new() { Name = "Đại lý Đà Nẵng", Region = "Miền Trung", OrderCount = 35, Sales = 2900000000, TargetPercent = 82, Rank = 3 },
                new() { Name = "Đại lý Cần Thơ", Region = "Miền Nam", OrderCount = 28, Sales = 2200000000, TargetPercent = 73, Rank = 4 },
                new() { Name = "Đại lý Hải Phòng", Region = "Miền Bắc", OrderCount = 22, Sales = 1800000000, TargetPercent = 60, Rank = 5 }
            };

            // Mock consumption speed
            ConsumptionSpeed = new List<VehicleConsumptionViewModel>
            {
                new() { Name = "Model 3 Standard", Sold = 65, Stock = 15, Speed = "FAST" },
                new() { Name = "Model S Premium", Sold = 42, Stock = 8, Speed = "FAST" },
                new() { Name = "Model X Performance", Sold = 25, Stock = 5, Speed = "MEDIUM" },
                new() { Name = "Model Y Long Range", Sold = 18, Stock = 22, Speed = "SLOW" }
            };

            // Get low stock alerts (real data)
            var lowStocks = await _context.Stocks
                .Where(s => s.Qty < 10 && s.OwnerType == "EVM")
                .Include(s => s.Vehicle)
                .OrderBy(s => s.Qty)
                .Take(5)
                .ToListAsync();

            LowStockAlerts = lowStocks.Select(s => new StockAlertViewModel
            {
                VehicleName = $"{s.Vehicle?.ModelName} {s.Vehicle?.VariantName}",
                Color = s.ColorCode,
                Qty = (int)s.Qty
            }).ToList();

            return Page();
        }

        public class DealerPerformanceViewModel
        {
            public string Name { get; set; } = "";
            public string Region { get; set; } = "";
            public int OrderCount { get; set; }
            public decimal Sales { get; set; }
            public int TargetPercent { get; set; }
            public int Rank { get; set; }
        }

        public class VehicleConsumptionViewModel
        {
            public string Name { get; set; } = "";
            public int Sold { get; set; }
            public int Stock { get; set; }
            public string Speed { get; set; } = ""; // FAST, MEDIUM, SLOW
        }

        public class StockAlertViewModel
        {
            public string VehicleName { get; set; } = "";
            public string Color { get; set; } = "";
            public int Qty { get; set; }
        }
    }
}

