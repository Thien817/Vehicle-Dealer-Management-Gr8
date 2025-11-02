using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using System.Text.Json;

namespace Vehicle_Dealer_Management.Pages.EVM.Vehicles
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
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

            var vehicle = await _context.Vehicles.FindAsync(id.Value);
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
                OtherSpecs = vehicle.SpecJson
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
            string? otherSpecs)
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

            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null)
            {
                TempData["Error"] = "Không tìm thấy xe này.";
                return RedirectToPage("/EVM/Vehicles/Index");
            }

            // Validate ModelName + VariantName unique (excluding current vehicle)
            var existing = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.ModelName == modelName.Trim() &&
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

            // Update vehicle
            vehicle.ModelName = modelName.Trim();
            vehicle.VariantName = variantName.Trim();
            vehicle.ImageUrl = imageUrl;
            vehicle.Status = status;
            vehicle.SpecJson = specJson;
            vehicle.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật thông tin xe thành công!";
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
        }
    }
}

