using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.DAL.Models;
using System.Text.Json;

namespace Vehicle_Dealer_Management.Pages.EVM.Vehicles
{
    public class CreateModel : PageModel
    {
        private readonly IVehicleService _vehicleService;
        private readonly IPricePolicyService _pricePolicyService;
        private readonly IStockService _stockService;
        private readonly IActivityLogService _activityLogService;

        public CreateModel(
            IVehicleService vehicleService,
            IPricePolicyService pricePolicyService,
            IStockService stockService,
            IActivityLogService activityLogService)
        {
            _vehicleService = vehicleService;
            _pricePolicyService = pricePolicyService;
            _stockService = stockService;
            _activityLogService = activityLogService;
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

            // Create vehicle using Service
            var vehicle = new Vehicle
            {
                ModelName = modelName.Trim(),
                VariantName = variantName.Trim(),
                ImageUrl = imageUrl,
                Status = status,
                SpecJson = specJson
            };

            var createdVehicle = await _vehicleService.CreateVehicleAsync(vehicle);

            // Create initial price policy if provided
            if (msrp.HasValue && msrp.Value > 0)
            {
                var pricePolicy = new PricePolicy
                {
                    VehicleId = createdVehicle.Id,
                    DealerId = null, // Global price
                    Msrp = msrp.Value,
                    WholesalePrice = wholesalePrice ?? msrp.Value * 0.9m,
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = null
                };

                await _pricePolicyService.CreatePricePolicyAsync(pricePolicy);
            }

            // Create initial stock if provided
            if (initialStock.HasValue && initialStock.Value > 0)
            {
                await _stockService.CreateOrUpdateStockAsync(
                    "EVM", 
                    0, 
                    createdVehicle.Id, 
                    "BLACK", 
                    initialStock.Value);
            }

            // Log activity
            var userIdInt = int.Parse(userId);
            var userRole = HttpContext.Session.GetString("UserRole") ?? "EVM_STAFF";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            
            await _activityLogService.LogActivityAsync(
                userId: userIdInt,
                action: "CREATE",
                entityType: "Vehicle",
                entityId: createdVehicle.Id,
                entityName: $"{createdVehicle.ModelName} {createdVehicle.VariantName}",
                description: "Đã thêm xe mới thành công",
                userRole: userRole,
                ipAddress: ipAddress);

            TempData["Success"] = "Đã thêm xe mới thành công!";
            return RedirectToPage("/EVM/Vehicles/Index");
        }
    }
}

