using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using System.Text.Json;

namespace Vehicle_Dealer_Management.Pages.Dealer
{
    public class VehiclesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public VehiclesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<VehicleViewModel> Vehicles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Get all vehicles with price policies and stocks
            var vehicles = await _context.Vehicles
                .Where(v => v.Status == "AVAILABLE")
                .ToListAsync();

            var dealerIdInt = int.Parse(dealerId);

            foreach (var vehicle in vehicles)
            {
                // Get price policy (dealer-specific or global)
                var pricePolicy = await _context.PricePolicies
                    .Where(p => p.VehicleId == vehicle.Id && 
                                (p.DealerId == dealerIdInt || p.DealerId == null) &&
                                p.ValidFrom <= DateTime.UtcNow &&
                                (p.ValidTo == null || p.ValidTo >= DateTime.UtcNow))
                    .OrderByDescending(p => p.DealerId) // Dealer-specific first
                    .FirstOrDefaultAsync();

                // Get stock colors available at EVM (dealer can order from EVM)
                var stocks = await _context.Stocks
                    .Where(s => s.VehicleId == vehicle.Id && s.OwnerType == "EVM" && s.Qty > 0)
                    .ToListAsync();

                Vehicles.Add(new VehicleViewModel
                {
                    Id = vehicle.Id,
                    Name = vehicle.ModelName,
                    Variant = vehicle.VariantName,
                    ImageUrl = vehicle.ImageUrl,
                    Status = vehicle.Status,
                    Msrp = pricePolicy?.Msrp ?? 0,
                    WholesalePrice = pricePolicy?.WholesalePrice ?? 0,
                    AvailableColors = stocks.Select(s => new ColorStock
                    {
                        Color = s.ColorCode,
                        Qty = (int)s.Qty
                    }).ToList()
                });
            }

            return Page();
        }

        public class VehicleViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Variant { get; set; } = "";
            public string ImageUrl { get; set; } = "";
            public string Status { get; set; } = "";
            public decimal Msrp { get; set; }
            public decimal WholesalePrice { get; set; }
            public List<ColorStock> AvailableColors { get; set; } = new();
        }

        public class ColorStock
        {
            public string Color { get; set; } = "";
            public int Qty { get; set; }
        }
    }
}

