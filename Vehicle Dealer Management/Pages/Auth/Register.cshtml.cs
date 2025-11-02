using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;
using Vehicle_Dealer_Management.BLL.IService;
using System.Security.Cryptography;
using System.Text;

namespace Vehicle_Dealer_Management.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ICustomerService _customerService;
        private readonly IActivityLogService _activityLogService;

        public RegisterModel(
            ApplicationDbContext context, 
            ICustomerService customerService,
            IActivityLogService activityLogService)
        {
            _context = context;
            _customerService = customerService;
            _activityLogService = activityLogService;
        }

        [BindProperty]
        public string? FullName { get; set; }

        [BindProperty]
        public string? Email { get; set; }

        [BindProperty]
        public string? Phone { get; set; }

        [BindProperty]
        public string? Password { get; set; }

        [BindProperty]
        public string? ConfirmPassword { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Validation
            if (string.IsNullOrEmpty(FullName) || string.IsNullOrEmpty(Email) || 
                string.IsNullOrEmpty(Phone) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Vui lòng điền đầy đủ thông tin.";
                return Page();
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Mật khẩu xác nhận không khớp.";
                return Page();
            }

            if (Password.Length < 6)
            {
                ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.";
                return Page();
            }

            // Check if email already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (existingUser != null)
            {
                ErrorMessage = "Email đã được sử dụng.";
                return Page();
            }

            // Get CUSTOMER role
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "CUSTOMER");
            if (customerRole == null)
            {
                ErrorMessage = "Lỗi hệ thống: Không tìm thấy role CUSTOMER.";
                return Page();
            }

            // Create user
            var user = new User
            {
                Email = Email,
                PasswordHash = HashPassword(Password),
                FullName = FullName,
                Phone = Phone,
                RoleId = customerRole.Id,
                DealerId = null,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create customer profile - use CustomerService if available
            var customerProfile = new CustomerProfile
            {
                UserId = user.Id,
                FullName = FullName,
                Phone = Phone,
                Email = Email,
                Address = "",
                CreatedDate = DateTime.UtcNow
            };

            // Note: CustomerService works with Customer model, not CustomerProfile
            // So we'll still use _context for CustomerProfile
            _context.CustomerProfiles.Add(customerProfile);
            await _context.SaveChangesAsync();

            // Log registration activity
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _activityLogService.LogActivityAsync(
                userId: user.Id,
                action: "REGISTER",
                entityType: "User",
                entityId: user.Id,
                entityName: user.FullName,
                description: "Đăng ký tài khoản mới thành công",
                userRole: "CUSTOMER",
                ipAddress: ipAddress);

            SuccessMessage = "Đăng ký thành công! Vui lòng đăng nhập.";
            
            // Redirect to login after 2 seconds
            return RedirectToPage("/Auth/Login");
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}

