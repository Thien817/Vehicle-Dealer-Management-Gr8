using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.Admin.Reports
{
    public class SalesByVehicleModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SalesByVehicleModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public string TopVehicle { get; set; } = "";
        public decimal AvgPrice { get; set; }

        public List<VehicleReportViewModel> VehicleReports { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Get all vehicles
            var vehicles = await _context.Vehicles.ToListAsync();

            // Mock sales data for each vehicle
            VehicleReports = new List<VehicleReportViewModel>
            {
                new() 
                { 
                    ModelName = "Model 3", 
                    VariantName = "Standard", 
                    ImageUrl = "https://images.unsplash.com/photo-1617531653332-bd46c24f2068?w=800",
                    QuantitySold = 125, 
                    Revenue = 187500000000, 
                    Speed = "FAST" 
                },
                new() 
                { 
                    ModelName = "Model S", 
                    VariantName = "Premium", 
                    ImageUrl = "https://images.unsplash.com/photo-1617788138017-80ad40651399?w=800",
                    QuantitySold = 85, 
                    Revenue = 212500000000, 
                    Speed = "FAST" 
                },
                new() 
                { 
                    ModelName = "Model X", 
                    VariantName = "Performance", 
                    ImageUrl = "https://giaxeoto.vn/admin/upload/images/resize/640-van-hanh-xe-tesla-model-x.jpg",
                    QuantitySold = 42, 
                    Revenue = 147000000000, 
                    Speed = "MEDIUM" 
                },
                new() 
                { 
                    ModelName = "Model Y", 
                    VariantName = "Long Range", 
                    ImageUrl = "https://images.unsplash.com/photo-1617788138017-80ad40651399?w=800",
                    QuantitySold = 28, 
                    Revenue = 56000000000, 
                    Speed = "SLOW" 
                }
            };

            // Calculate avg price
            foreach (var vehicle in VehicleReports)
            {
                vehicle.AvgPrice = vehicle.QuantitySold > 0 ? vehicle.Revenue / vehicle.QuantitySold : 0;
            }

            TotalSold = VehicleReports.Sum(v => v.QuantitySold);
            TotalRevenue = VehicleReports.Sum(v => v.Revenue);
            TopVehicle = VehicleReports.OrderByDescending(v => v.QuantitySold).FirstOrDefault()?.ModelName ?? "N/A";
            AvgPrice = TotalSold > 0 ? TotalRevenue / TotalSold : 0;

            return Page();
        }

        public class VehicleReportViewModel
        {
            public string ModelName { get; set; } = "";
            public string VariantName { get; set; } = "";
            public string ImageUrl { get; set; } = "";
            public int QuantitySold { get; set; }
            public decimal Revenue { get; set; }
            public decimal AvgPrice { get; set; }
            public string Speed { get; set; } = ""; // FAST, MEDIUM, SLOW
        }
    }
}

