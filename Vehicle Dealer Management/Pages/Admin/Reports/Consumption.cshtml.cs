using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.Admin.Reports
{
    public class ConsumptionModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ConsumptionModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public decimal AvgConsumptionRate { get; set; }
        public int FastMovingCount { get; set; }
        public int SlowMovingCount { get; set; }
        public int AvgDaysToEmpty { get; set; }

        public List<ConsumptionDataViewModel> ConsumptionData { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Get all vehicles
            var vehicles = await _context.Vehicles.ToListAsync();

            // Get current stocks
            var stocks = await _context.Stocks
                .GroupBy(s => s.VehicleId)
                .Select(g => new { VehicleId = g.Key, Stock = g.Sum(s => s.Qty) })
                .ToDictionaryAsync(s => s.VehicleId, s => (int)s.Stock);

            // Mock consumption data
            ConsumptionData = new List<ConsumptionDataViewModel>
            {
                new() 
                { 
                    VehicleName = "Model 3 Standard", 
                    SoldLast30Days = 65, 
                    CurrentStock = 27, 
                    WeeklyRate = 15.2m,
                    DaysToEmpty = 13,
                    Speed = "FAST"
                },
                new() 
                { 
                    VehicleName = "Model S Premium", 
                    SoldLast30Days = 48, 
                    CurrentStock = 18, 
                    WeeklyRate = 11.2m,
                    DaysToEmpty = 11,
                    Speed = "FAST"
                },
                new() 
                { 
                    VehicleName = "Model X Performance", 
                    SoldLast30Days = 32, 
                    CurrentStock = 13, 
                    WeeklyRate = 7.5m,
                    DaysToEmpty = 12,
                    Speed = "MEDIUM"
                },
                new() 
                { 
                    VehicleName = "Model Y Long Range", 
                    SoldLast30Days = 18, 
                    CurrentStock = 58, 
                    WeeklyRate = 4.2m,
                    DaysToEmpty = 97,
                    Speed = "SLOW"
                },
                new() 
                { 
                    VehicleName = "Cybertruck", 
                    SoldLast30Days = 5, 
                    CurrentStock = 42, 
                    WeeklyRate = 1.2m,
                    DaysToEmpty = null,
                    Speed = "SLOW"
                }
            };

            // Calculate summary
            AvgConsumptionRate = ConsumptionData.Average(c => c.WeeklyRate);
            FastMovingCount = ConsumptionData.Count(c => c.Speed == "FAST");
            SlowMovingCount = ConsumptionData.Count(c => c.Speed == "SLOW");
            AvgDaysToEmpty = (int)ConsumptionData.Where(c => c.DaysToEmpty.HasValue).Average(c => c.DaysToEmpty ?? 0);

            return Page();
        }

        public class ConsumptionDataViewModel
        {
            public string VehicleName { get; set; } = "";
            public int SoldLast30Days { get; set; }
            public int CurrentStock { get; set; }
            public decimal WeeklyRate { get; set; }
            public int? DaysToEmpty { get; set; }
            public string Speed { get; set; } = ""; // FAST, MEDIUM, SLOW
        }
    }
}

