using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.EVM.Vehicles
{
    public class IndexModel : PageModel
    {
        private readonly IVehicleService _vehicleService;
        private readonly IPricePolicyService _pricePolicyService;
        private readonly IActivityLogService _activityLogService;
        private readonly ApplicationDbContext _context; // Tạm thời giữ để check SalesDocumentLines

        public IndexModel(
            IVehicleService vehicleService,
            IPricePolicyService pricePolicyService,
            IActivityLogService activityLogService,
            ApplicationDbContext context)
        {
            _vehicleService = vehicleService;
            _pricePolicyService = pricePolicyService;
            _activityLogService = activityLogService;
            _context = context;
        }

        public List<VehicleViewModel> Vehicles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var vehicles = await _vehicleService.GetAllVehiclesAsync();

            foreach (var vehicle in vehicles)
            {
                var pricePolicy = await _pricePolicyService.GetActivePricePolicyAsync(vehicle.Id, null);

                Vehicles.Add(new VehicleViewModel
                {
                    Id = vehicle.Id,
                    Name = vehicle.ModelName,
                    Variant = vehicle.VariantName,
                    ImageUrl = vehicle.ImageUrl,
                    Status = vehicle.Status,
                    Msrp = pricePolicy?.Msrp ?? 0
                });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int vehicleId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var vehicle = await _vehicleService.GetVehicleByIdAsync(vehicleId);
            if (vehicle == null)
            {
                TempData["Error"] = "Không tìm thấy xe này.";
                return RedirectToPage();
            }

            // Check if vehicle is used in any orders/quotes
            var hasOrders = await _context.SalesDocumentLines
                .AnyAsync(l => l.VehicleId == vehicleId);
            
            if (hasOrders)
            {
                TempData["Error"] = "Không thể xóa xe này vì đã có đơn hàng/báo giá sử dụng. Hãy đổi trạng thái thành DISCONTINUED thay vì xóa.";
                return RedirectToPage();
            }

            try
            {
                // Soft delete: Set status to DISCONTINUED instead of hard delete
                vehicle.Status = "DISCONTINUED";
                vehicle.UpdatedDate = DateTime.UtcNow;
                await _vehicleService.UpdateVehicleAsync(vehicle);

                // Log activity
                var userIdInt = int.Parse(userId);
                var userRole = HttpContext.Session.GetString("UserRole") ?? "EVM_STAFF";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                
                await _activityLogService.LogActivityAsync(
                    userId: userIdInt,
                    action: "DELETE",
                    entityType: "Vehicle",
                    entityId: vehicle.Id,
                    entityName: $"{vehicle.ModelName} {vehicle.VariantName}",
                    description: "Đã xóa (đổi trạng thái thành DISCONTINUED) xe thành công",
                    userRole: userRole,
                    ipAddress: ipAddress);

                TempData["Success"] = "Đã xóa xe thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xóa xe: {ex.Message}";
            }
            
            return RedirectToPage();
        }

        public class VehicleViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Variant { get; set; } = "";
            public string ImageUrl { get; set; } = "";
            public string Status { get; set; } = "";
            public decimal Msrp { get; set; }
        }
    }
}

