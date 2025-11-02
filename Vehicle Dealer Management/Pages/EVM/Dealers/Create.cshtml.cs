using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.DAL.Models;
using D = Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.Pages.EVM.Dealers
{
    public class CreateModel : PageModel
    {
        private readonly IDealerService _dealerService;
        private readonly IActivityLogService _activityLogService;

        public CreateModel(
            IDealerService dealerService,
            IActivityLogService activityLogService)
        {
            _dealerService = dealerService;
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
            string name,
            string address,
            string? phoneNumber,
            string? email,
            string status)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Validate
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ tên đại lý và địa chỉ.";
                return Page();
            }

            // Validate status
            if (string.IsNullOrWhiteSpace(status))
            {
                status = "ACTIVE";
            }

            // Generate dealer code from name (uppercase, remove special chars, replace spaces with underscores)
            var dealerCode = name.Trim()
                .ToUpperInvariant()
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "")
                .Replace(",", "")
                .Replace("Đ", "D")
                .Replace("Á", "A")
                .Replace("Ạ", "A")
                .Replace("À", "A")
                .Replace("Ả", "A")
                .Replace("Ã", "A")
                .Replace("Â", "A")
                .Replace("Ậ", "A")
                .Replace("Ầ", "A")
                .Replace("Ẩ", "A")
                .Replace("Ẫ", "A")
                .Replace("Ă", "A")
                .Replace("Ặ", "A")
                .Replace("Ằ", "A")
                .Replace("Ẳ", "A")
                .Replace("Ẵ", "A")
                .Replace("É", "E")
                .Replace("Ẹ", "E")
                .Replace("È", "E")
                .Replace("Ẻ", "E")
                .Replace("Ẽ", "E")
                .Replace("Ê", "E")
                .Replace("Ệ", "E")
                .Replace("Ề", "E")
                .Replace("Ể", "E")
                .Replace("Ễ", "E")
                .Replace("Í", "I")
                .Replace("Ị", "I")
                .Replace("Ì", "I")
                .Replace("Ỉ", "I")
                .Replace("Ĩ", "I")
                .Replace("Ó", "O")
                .Replace("Ọ", "O")
                .Replace("Ò", "O")
                .Replace("Ỏ", "O")
                .Replace("Õ", "O")
                .Replace("Ô", "O")
                .Replace("Ộ", "O")
                .Replace("Ồ", "O")
                .Replace("Ổ", "O")
                .Replace("Ỗ", "O")
                .Replace("Ơ", "O")
                .Replace("Ợ", "O")
                .Replace("Ờ", "O")
                .Replace("Ở", "O")
                .Replace("Ỡ", "O")
                .Replace("Ú", "U")
                .Replace("Ụ", "U")
                .Replace("Ù", "U")
                .Replace("Ủ", "U")
                .Replace("Ũ", "U")
                .Replace("Ư", "U")
                .Replace("Ự", "U")
                .Replace("Ừ", "U")
                .Replace("Ử", "U")
                .Replace("Ữ", "U")
                .Replace("Ý", "Y")
                .Replace("Ỵ", "Y")
                .Replace("Ỳ", "Y")
                .Replace("Ỷ", "Y")
                .Replace("Ỹ", "Y");

            // Remove any remaining special characters
            dealerCode = new string(dealerCode.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            
            // Ensure max length
            if (dealerCode.Length > 50)
            {
                dealerCode = dealerCode.Substring(0, 50);
            }

            // Check if code already exists and append number if needed
            var existingDealers = await _dealerService.GetAllDealersAsync();
            var originalCode = dealerCode;
            var codeCounter = 1;
            while (existingDealers.Any(d => d.Code == dealerCode))
            {
                var suffix = $"_{codeCounter}";
                var maxLength = 50 - suffix.Length;
                dealerCode = originalCode.Length > maxLength 
                    ? originalCode.Substring(0, maxLength) + suffix 
                    : originalCode + suffix;
                codeCounter++;
            }

            // Create dealer using Service
            var dealer = new D.Dealer
            {
                Name = name.Trim(),
                Code = dealerCode,
                Address = address.Trim(),
                PhoneNumber = phoneNumber?.Trim(),
                Email = email?.Trim(),
                Status = status,
                IsActive = status == "ACTIVE",
                CreatedDate = DateTime.UtcNow
            };

            await _dealerService.CreateDealerAsync(dealer);

            // Log activity
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdStr))
            {
                var userIdInt = int.Parse(userIdStr);
                var userRole = HttpContext.Session.GetString("UserRole") ?? "EVM_STAFF";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                
                await _activityLogService.LogActivityAsync(
                    userId: userIdInt,
                    action: "CREATE",
                    entityType: "Dealer",
                    entityId: dealer.Id,
                    entityName: dealer.Name,
                    description: "Đã thêm đại lý mới thành công",
                    userRole: userRole,
                    ipAddress: ipAddress);
            }

            TempData["Success"] = "Đã thêm đại lý mới thành công!";
            return RedirectToPage("/EVM/Dealers");
        }
    }
}

