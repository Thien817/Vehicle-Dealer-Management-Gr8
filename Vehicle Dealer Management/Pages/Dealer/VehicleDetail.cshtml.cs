using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using System.Text.Json;

namespace Vehicle_Dealer_Management.Pages.Dealer
{
    public class VehicleDetailModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public VehicleDetailModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public VehicleDetailViewModel Vehicle { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            ViewData["UserRole"] = HttpContext.Session.GetString("UserRole") ?? "DEALER_STAFF";
            ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "User";

            var dealerIdInt = int.Parse(dealerId);

            // Get vehicle with all related data from DB
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                return NotFound();
            }

            // Get price policy (dealer-specific or global)
            var pricePolicy = await _context.PricePolicies
                .Where(p => p.VehicleId == vehicle.Id &&
                            (p.DealerId == dealerIdInt || p.DealerId == null) &&
                            p.ValidFrom <= DateTime.UtcNow &&
                            (p.ValidTo == null || p.ValidTo >= DateTime.UtcNow))
                .OrderByDescending(p => p.DealerId) // Dealer-specific first
                .FirstOrDefaultAsync();

            // Get stock availability (EVM stock - dealer can order from EVM)
            var stocks = await _context.Stocks
                .Where(s => s.VehicleId == vehicle.Id && s.OwnerType == "EVM" && s.Qty > 0)
                .ToListAsync();

            // Get dealer stock (if any)
            var dealerStocks = await _context.Stocks
                .Where(s => s.VehicleId == vehicle.Id && s.OwnerType == "DEALER" && s.OwnerId == dealerIdInt && s.Qty > 0)
                .ToListAsync();

            // Parse specs from JSON
            var specs = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(vehicle.SpecJson))
            {
                try
                {
                    specs = JsonSerializer.Deserialize<Dictionary<string, string>>(vehicle.SpecJson) ?? new Dictionary<string, string>();
                }
                catch
                {
                    specs = new Dictionary<string, string>();
                }
            }

            Vehicle = new VehicleDetailViewModel
            {
                Id = vehicle.Id,
                ModelName = vehicle.ModelName,
                VariantName = vehicle.VariantName,
                ImageUrl = vehicle.ImageUrl,
                Status = vehicle.Status,
                Specs = specs,
                Msrp = pricePolicy?.Msrp ?? 0,
                WholesalePrice = pricePolicy?.WholesalePrice ?? 0,
                AvailableColors = stocks.Select(s => new StockColorViewModel
                {
                    ColorCode = s.ColorCode,
                    Qty = (int)s.Qty,
                    OwnerType = "EVM"
                }).ToList(),
                DealerStocks = dealerStocks.Select(s => new StockColorViewModel
                {
                    ColorCode = s.ColorCode,
                    Qty = (int)s.Qty,
                    OwnerType = "DEALER"
                }).ToList()
            };

            return Page();
        }

        public class VehicleDetailViewModel
        {
            public int Id { get; set; }
            public string ModelName { get; set; } = "";
            public string VariantName { get; set; } = "";
            public string ImageUrl { get; set; } = "";
            public string Status { get; set; } = "";
            public Dictionary<string, string> Specs { get; set; } = new();
            public decimal Msrp { get; set; }
            public decimal WholesalePrice { get; set; }
            public List<StockColorViewModel> AvailableColors { get; set; } = new();
            public List<StockColorViewModel> DealerStocks { get; set; } = new();
        }

        public class StockColorViewModel
        {
            public string ColorCode { get; set; } = "";
            public int Qty { get; set; }
            public string OwnerType { get; set; } = "";
        }
    }
}

