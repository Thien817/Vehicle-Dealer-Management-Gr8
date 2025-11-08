using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.Pages.Dealer
{
    public class VehiclesModel : PageModel
    {
        private readonly IVehicleService _vehicleService;
        private readonly IPricePolicyService _pricePolicyService;
        private readonly IStockService _stockService;

        public VehiclesModel(
            IVehicleService vehicleService,
            IPricePolicyService pricePolicyService,
            IStockService stockService)
        {
            _vehicleService = vehicleService;
            _pricePolicyService = pricePolicyService;
            _stockService = stockService;
        }

        public List<VehicleViewModel> Vehicles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var dealerIdInt = int.Parse(dealerId);

            // ✅ THAY ĐỔI: Chỉ lấy xe trong kho DEALER, không phải EVM
            var dealerStocks = await _stockService.GetStocksByOwnerAsync("DEALER", dealerIdInt);

            // Group by VehicleId để tránh trùng lặp
            var vehicleGroups = dealerStocks.GroupBy(s => s.VehicleId);

            foreach (var group in vehicleGroups)
            {
                var vehicleId = group.Key;

                // Get vehicle details
                var vehicle = await _vehicleService.GetVehicleByIdAsync(vehicleId);
                if (vehicle == null || vehicle.Status != "AVAILABLE") continue;

                // Get price policy
                var pricePolicy = await _pricePolicyService.GetActivePricePolicyAsync(vehicleId, dealerIdInt);

                // Get available colors from dealer stock
                var colorStocks = group.Select(s => new ColorStock
                {
                    Color = s.ColorCode,
                    Qty = (int)s.Qty
                }).ToList();

                Vehicles.Add(new VehicleViewModel
                {
                    Id = vehicle.Id,
                    Name = vehicle.ModelName,
                    Variant = vehicle.VariantName,
                    ImageUrl = vehicle.ImageUrl,
                    Status = vehicle.Status,
                    Msrp = pricePolicy?.Msrp ?? 0,
                    WholesalePrice = pricePolicy?.WholesalePrice ?? 0,
                    AvailableColors = colorStocks
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