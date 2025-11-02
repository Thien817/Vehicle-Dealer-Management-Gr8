using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;
using System.Text.Json;

namespace Vehicle_Dealer_Management.Pages.EVM.Vehicles
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            string modelName, 
            string variantName, 
            string imageUrl, 
            string status,
            string? battery,
            string? range,
            string? power,
            string? acceleration,
            string? maxSpeed,
            int? seats,
            string? otherSpecs,
            decimal? msrp,
            decimal? wholesalePrice,
            int? initialStock)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Validate
            if (string.IsNullOrWhiteSpace(modelName) || string.IsNullOrWhiteSpace(variantName))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ tên mẫu xe và phiên bản.";
                return Page();
            }

            // Build spec JSON
            var specs = new Dictionary<string, object?>();
            if (!string.IsNullOrWhiteSpace(battery)) specs["battery"] = battery;
            if (!string.IsNullOrWhiteSpace(range)) specs["range"] = range;
            if (!string.IsNullOrWhiteSpace(power)) specs["power"] = power;
            if (!string.IsNullOrWhiteSpace(acceleration)) specs["acceleration"] = acceleration;
            if (!string.IsNullOrWhiteSpace(maxSpeed)) specs["maxSpeed"] = maxSpeed;
            if (seats.HasValue) specs["seats"] = seats.Value;

            // Parse other specs if provided
            if (!string.IsNullOrWhiteSpace(otherSpecs))
            {
                try
                {
                    var additionalSpecs = JsonSerializer.Deserialize<Dictionary<string, object>>(otherSpecs);
                    if (additionalSpecs != null)
                    {
                        foreach (var kvp in additionalSpecs)
                        {
                            specs[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch
                {
                    // Ignore invalid JSON
                }
            }

            var specJson = JsonSerializer.Serialize(specs);

            // Create vehicle
            var vehicle = new Vehicle
            {
                ModelName = modelName.Trim(),
                VariantName = variantName.Trim(),
                ImageUrl = imageUrl,
                Status = status,
                SpecJson = specJson,
                CreatedDate = DateTime.UtcNow
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            // Create initial price policy if provided
            if (msrp.HasValue && msrp.Value > 0)
            {
                var pricePolicy = new PricePolicy
                {
                    VehicleId = vehicle.Id,
                    DealerId = null, // Global price
                    Msrp = msrp.Value,
                    WholesalePrice = wholesalePrice ?? msrp.Value * 0.9m,
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = null,
                    CreatedDate = DateTime.UtcNow
                };

                _context.PricePolicies.Add(pricePolicy);
                await _context.SaveChangesAsync();
            }

            // Create initial stock if provided
            if (initialStock.HasValue && initialStock.Value > 0)
            {
                var stock = new Stock
                {
                    OwnerType = "EVM",
                    OwnerId = 0,
                    VehicleId = vehicle.Id,
                    ColorCode = "BLACK", // Default color
                    Qty = initialStock.Value,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Stocks.Add(stock);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/EVM/Vehicles/Index");
        }
    }
}

