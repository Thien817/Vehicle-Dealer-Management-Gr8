using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.DAL.Data;
using System.Text.Json;

namespace Vehicle_Dealer_Management.Pages.EVM.Vehicles
{
    public class EditModel : PageModel
    {
        private readonly IVehicleService _vehicleService;
        private readonly IPricePolicyService _pricePolicyService;
        private readonly IActivityLogService _activityLogService;
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context; // Tạm thời cần cho unique validation

        public EditModel(
            IVehicleService vehicleService,
            IPricePolicyService pricePolicyService,
            IActivityLogService activityLogService,
            INotificationService notificationService,
            ApplicationDbContext context)
        {
            _vehicleService = vehicleService;
            _pricePolicyService = pricePolicyService;
            _activityLogService = activityLogService;
            _notificationService = notificationService;
            _context = context;
        }

        public VehicleEditViewModel? Vehicle { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            if (!id.HasValue)
            {
                return RedirectToPage("/EVM/Vehicles/Index");
            }

            var vehicle = await _vehicleService.GetVehicleByIdAsync(id.Value);
            if (vehicle == null)
            {
                TempData["Error"] = "Không tìm thấy xe này.";
                return RedirectToPage("/EVM/Vehicles/Index");
            }

            // Parse specs from JSON
            var specs = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(vehicle.SpecJson))
            {
                try
                {
                    var parsedSpecs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(vehicle.SpecJson);
                    if (parsedSpecs != null)
                    {
                        foreach (var kvp in parsedSpecs)
                        {
                            specs[kvp.Key] = kvp.Value.GetRawText().Trim('"');
                        }
                    }
                }
                catch
                {
                    // Ignore parse errors
                }
            }

            // Get active price policy
            var pricePolicy = await _pricePolicyService.GetActivePricePolicyAsync(vehicle.Id, null);

            Vehicle = new VehicleEditViewModel
            {
                Id = vehicle.Id,
                ModelName = vehicle.ModelName,
                VariantName = vehicle.VariantName,
                ImageUrl = vehicle.ImageUrl,
                Status = vehicle.Status,
                Battery = specs.GetValueOrDefault("battery", ""),
                Range = specs.GetValueOrDefault("range", ""),
                Power = specs.GetValueOrDefault("power", ""),
                Acceleration = specs.GetValueOrDefault("acceleration", ""),
                MaxSpeed = specs.GetValueOrDefault("maxSpeed", ""),
                Seats = specs.ContainsKey("seats") && int.TryParse(specs["seats"], out var seats) ? seats : null,
                OtherSpecs = vehicle.SpecJson,
                Msrp = pricePolicy?.Msrp ?? 0,
                WholesalePrice = pricePolicy?.WholesalePrice ?? 0
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            int vehicleId,
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
            decimal? discountPercent,
            string? priceNote)
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
                return await OnGetAsync(vehicleId);
            }

            var vehicle = await _vehicleService.GetVehicleByIdAsync(vehicleId);
            if (vehicle == null)
            {
                TempData["Error"] = "Không tìm thấy xe này.";
                return RedirectToPage("/EVM/Vehicles/Index");
            }

            // Validate ModelName + VariantName unique (excluding current vehicle)
            var allVehicles = await _vehicleService.GetAllVehiclesAsync();
            var existing = allVehicles.FirstOrDefault(v => v.ModelName == modelName.Trim() &&
                                          v.VariantName == variantName.Trim() &&
                                          v.Id != vehicleId);
            if (existing != null)
            {
                ErrorMessage = "Mẫu xe và phiên bản này đã tồn tại.";
                return await OnGetAsync(vehicleId);
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

            // Update vehicle using Service
            vehicle.ModelName = modelName.Trim();
            vehicle.VariantName = variantName.Trim();
            vehicle.ImageUrl = imageUrl;
            vehicle.Status = status;
            vehicle.SpecJson = specJson;

            await _vehicleService.UpdateVehicleAsync(vehicle);

            // Update or create price policy if provided
            if (msrp.HasValue && msrp.Value > 0)
            {
                var existingPolicy = await _pricePolicyService.GetActivePricePolicyAsync(vehicleId, null);
                if (existingPolicy != null)
                {
                    // Update existing policy - invalidate old one and create new
                    existingPolicy.ValidTo = DateTime.UtcNow.AddSeconds(-1);
                    await _pricePolicyService.UpdatePricePolicyAsync(existingPolicy);
                }

                // Calculate final price if discount is applied
                decimal finalMsrp = msrp.Value;
                decimal? finalWholesalePrice = wholesalePrice ?? msrp.Value * 0.9m;
                decimal finalDiscountPercent = 0;

                if (discountPercent.HasValue && discountPercent.Value > 0)
                {
                    finalDiscountPercent = discountPercent.Value;
                    finalMsrp = msrp.Value * (1 - discountPercent.Value / 100);
                    finalWholesalePrice = (wholesalePrice ?? msrp.Value * 0.9m) * (1 - discountPercent.Value / 100);
                }

                // Create new price policy
                var newPolicy = new Vehicle_Dealer_Management.DAL.Models.PricePolicy
                {
                    VehicleId = vehicleId,
                    DealerId = null, // Global price
                    Msrp = finalMsrp,
                    WholesalePrice = finalWholesalePrice,
                    PromotionId = null, // No promotion if using direct discount
                    Note = string.IsNullOrWhiteSpace(priceNote) ? null : priceNote.Trim(),
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = null,
                    CreatedDate = DateTime.UtcNow
                };

                await _pricePolicyService.CreatePricePolicyAsync(newPolicy);

                // Create notification if discount is applied
                if (finalDiscountPercent > 0)
                {
                    await _notificationService.CreatePromotionNotificationAsync(
                        vehicleId,
                        $"{vehicle.ModelName} {vehicle.VariantName}",
                        finalDiscountPercent);
                }
            }

            // Log activity
            var userIdInt = int.Parse(userId);
            var userRole = HttpContext.Session.GetString("UserRole") ?? "EVM_STAFF";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            
            await _activityLogService.LogActivityAsync(
                userId: userIdInt,
                action: "UPDATE",
                entityType: "Vehicle",
                entityId: vehicle.Id,
                entityName: $"{vehicle.ModelName} {vehicle.VariantName}",
                description: "Đã chỉnh sửa xe thành công",
                userRole: userRole,
                ipAddress: ipAddress);

            TempData["Success"] = "Đã chỉnh sửa xe thành công!";
            return RedirectToPage("/EVM/Vehicles/Index");
        }

        public class VehicleEditViewModel
        {
            public int Id { get; set; }
            public string ModelName { get; set; } = "";
            public string VariantName { get; set; } = "";
            public string ImageUrl { get; set; } = "";
            public string Status { get; set; } = "";
            public string Battery { get; set; } = "";
            public string Range { get; set; } = "";
            public string Power { get; set; } = "";
            public string Acceleration { get; set; } = "";
            public string MaxSpeed { get; set; } = "";
            public int? Seats { get; set; }
            public string? OtherSpecs { get; set; }
            public decimal Msrp { get; set; }
            public decimal WholesalePrice { get; set; }
        }
    }
}

