using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.Pages.EVM
{
    public class PricePoliciesModel : PageModel
    {
        private readonly IPricePolicyService _pricePolicyService;
        private readonly IVehicleService _vehicleService;
        private readonly IDealerService _dealerService;
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context; // Tạm thời cần cho query phức tạp

        public PricePoliciesModel(
            IPricePolicyService pricePolicyService,
            IVehicleService vehicleService,
            IDealerService dealerService,
            INotificationService notificationService,
            ApplicationDbContext context)
        {
            _pricePolicyService = pricePolicyService;
            _vehicleService = vehicleService;
            _dealerService = dealerService;
            _notificationService = notificationService;
            _context = context;
        }

        public List<string> AllVehicles { get; set; } = new();
        public List<string> AllDealers { get; set; } = new();
        public List<VehicleSimple> Vehicles { get; set; } = new();
        public List<PromotionSimple> Promotions { get; set; } = new();
        public List<PricePolicyViewModel> PricePolicies { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Get all vehicles for filter
            var allVehiclesList = await _vehicleService.GetAllVehiclesAsync();
            AllVehicles = allVehiclesList.Select(v => v.ModelName + " " + v.VariantName).ToList();

            // Get all dealers for filter
            var allDealersList = await _dealerService.GetAllDealersAsync();
            AllDealers = allDealersList.Select(d => d.Name).ToList();

            // Get vehicles for create form
            Vehicles = allVehiclesList.Select(v => new VehicleSimple
            {
                Id = v.Id,
                Name = v.ModelName + " " + v.VariantName
            }).ToList();

            // Get active promotions for create form
            Promotions = await _context.Promotions
                .Where(p => p.ValidFrom <= DateTime.UtcNow && (p.ValidTo == null || p.ValidTo >= DateTime.UtcNow))
                .Select(p => new PromotionSimple
                {
                    Id = p.Id,
                    Name = p.Name,
                    RuleJson = p.RuleJson
                })
                .ToListAsync();

            // Get all price policies (cần include để map ViewModel)
            var policies = await _context.PricePolicies
                .Include(p => p.Vehicle)
                .Include(p => p.Dealer)
                .OrderByDescending(p => p.ValidFrom)
                .ToListAsync();

            PricePolicies = policies.Select(p => new PricePolicyViewModel
            {
                Id = p.Id,
                VehicleName = $"{p.Vehicle?.ModelName} {p.Vehicle?.VariantName}",
                DealerName = p.Dealer?.Name ?? "",
                Msrp = p.Msrp,
                WholesalePrice = p.WholesalePrice ?? 0,
                ValidFrom = p.ValidFrom,
                ValidTo = p.ValidTo,
                Note = p.Note ?? ""
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int vehicleId, decimal msrp, decimal wholesalePrice, int? promotionId, decimal? discountPercent, string? note)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            try
            {
                decimal? finalMsrp = msrp;
                decimal? finalWholesalePrice = wholesalePrice;
                decimal finalDiscountPercent = 0;

                // Ưu tiên discountPercent trực tiếp, nếu không có thì lấy từ promotion
                if (discountPercent.HasValue && discountPercent.Value > 0)
                {
                    finalDiscountPercent = discountPercent.Value;
                    finalMsrp = msrp * (1 - discountPercent.Value / 100);
                    finalWholesalePrice = wholesalePrice * (1 - discountPercent.Value / 100);
                }
                else if (promotionId.HasValue)
                {
                    var promotion = await _context.Promotions.FindAsync(promotionId.Value);
                    if (promotion != null)
                    {
                        // Parse discount from RuleJson
                        var ruleJson = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(promotion.RuleJson ?? "{}");
                        if (ruleJson != null && ruleJson.ContainsKey("discountPercent"))
                        {
                            if (decimal.TryParse(ruleJson["discountPercent"].ToString(), out var percent))
                            {
                                finalDiscountPercent = percent;
                                finalMsrp = msrp * (1 - percent / 100);
                                finalWholesalePrice = wholesalePrice * (1 - percent / 100);
                            }
                        }
                    }
                }

                var pricePolicy = new PricePolicy
                {
                    VehicleId = vehicleId,
                    DealerId = null, // Global
                    Msrp = finalMsrp ?? msrp,
                    WholesalePrice = finalWholesalePrice ?? wholesalePrice,
                    PromotionId = promotionId, // Có thể null nếu dùng discountPercent trực tiếp
                    Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = null
                };

                await _pricePolicyService.CreatePricePolicyAsync(pricePolicy);

                // Create notification for customers if discount is applied
                if (finalDiscountPercent > 0)
                {
                    var vehicle = await _vehicleService.GetVehicleByIdAsync(vehicleId);
                    if (vehicle != null)
                    {
                        await _notificationService.CreatePromotionNotificationAsync(
                            vehicleId, 
                            $"{vehicle.ModelName} {vehicle.VariantName}", 
                            finalDiscountPercent);
                    }
                }

                TempData["Success"] = "Tạo chính sách giá thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdatePolicyAsync(int policyId, decimal msrp, decimal wholesalePrice, string validFrom, string? validTo, decimal? discountPercent, string? note)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            try
            {
                var policy = await _pricePolicyService.GetPricePolicyByIdAsync(policyId);
                if (policy == null)
                {
                    TempData["EditError"] = "Không tìm thấy chính sách giá này.";
                    return RedirectToPage();
                }

                // Parse dates
                if (!DateTime.TryParse(validFrom, out var validFromDate))
                {
                    TempData["EditError"] = "Ngày bắt đầu không hợp lệ.";
                    return RedirectToPage();
                }

                DateTime? validToDate = null;
                if (!string.IsNullOrWhiteSpace(validTo) && DateTime.TryParse(validTo, out var parsedDate))
                {
                    validToDate = parsedDate;
                }

                // Calculate final price if discount is applied
                decimal finalMsrp = msrp;
                decimal? finalWholesalePrice = wholesalePrice;
                decimal finalDiscountPercent = 0;

                if (discountPercent.HasValue && discountPercent.Value > 0)
                {
                    finalDiscountPercent = discountPercent.Value;
                    finalMsrp = msrp * (1 - discountPercent.Value / 100);
                    finalWholesalePrice = wholesalePrice * (1 - discountPercent.Value / 100);
                }

                // Update policy
                policy.Msrp = finalMsrp;
                policy.WholesalePrice = finalWholesalePrice;
                policy.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
                policy.ValidFrom = validFromDate;
                policy.ValidTo = validToDate;

                await _pricePolicyService.UpdatePricePolicyAsync(policy);

                // Create notification if discount is applied
                if (finalDiscountPercent > 0)
                {
                    var vehicle = await _vehicleService.GetVehicleByIdAsync(policy.VehicleId);
                    if (vehicle != null)
                    {
                        await _notificationService.CreatePromotionNotificationAsync(
                            policy.VehicleId,
                            $"{vehicle.ModelName} {vehicle.VariantName}",
                            finalDiscountPercent);
                    }
                }

                TempData["Success"] = "Cập nhật chính sách giá thành công!";
            }
            catch (Exception ex)
            {
                TempData["EditError"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeletePolicyAsync(int policyId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            try
            {
                await _pricePolicyService.DeletePricePolicyAsync(policyId);
                TempData["Success"] = "Xóa chính sách giá thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToPage();
        }

        public class VehicleSimple
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class PromotionSimple
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string? RuleJson { get; set; }
        }

        public class PricePolicyViewModel
        {
            public int Id { get; set; }
            public string VehicleName { get; set; } = "";
            public string DealerName { get; set; } = "";
            public decimal Msrp { get; set; }
            public decimal WholesalePrice { get; set; }
            public DateTime ValidFrom { get; set; }
            public DateTime? ValidTo { get; set; }
            public string Note { get; set; } = "";
        }
    }
}

