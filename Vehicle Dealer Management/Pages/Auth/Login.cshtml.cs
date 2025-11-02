using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.BLL.IService;
using System.Security.Cryptography;
using System.Text;

namespace Vehicle_Dealer_Management.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogService _activityLogService;

        public LoginModel(ApplicationDbContext context, IActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        [BindProperty]
        public string? Email { get; set; }

        [BindProperty]
        public string? Password { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet(string? email)
        {
            // Auto-fill email from query string (from Home page)
            if (!string.IsNullOrEmpty(email))
            {
                Email = email;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Vui lòng nhập email và mật khẩu.";
                return Page();
            }

            // Hash password
            var passwordHash = HashPassword(Password);

            // Find user
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Dealer)
                .FirstOrDefaultAsync(u => u.Email == Email && u.PasswordHash == passwordHash);

            if (user == null)
            {
                ErrorMessage = "Email hoặc mật khẩu không đúng.";
                return Page();
            }

            // Store user info in session (simplified - in production use proper authentication)
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserRole", user.Role.Code);
            
            if (user.DealerId.HasValue)
            {
                HttpContext.Session.SetString("DealerId", user.DealerId.Value.ToString());
                HttpContext.Session.SetString("DealerName", user.Dealer?.Name ?? "");
            }

            // Log login activity
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _activityLogService.LogActivityAsync(
                userId: user.Id,
                action: "LOGIN",
                entityType: "User",
                entityId: user.Id,
                entityName: user.FullName,
                description: "Đăng nhập thành công",
                userRole: user.Role.Code,
                ipAddress: ipAddress);

            // Redirect based on role
            return user.Role.Code switch
            {
                "CUSTOMER" => RedirectToPage("/Customer/Dashboard"),
                "DEALER_STAFF" => RedirectToPage("/Dealer/Dashboard"),
                "DEALER_MANAGER" => RedirectToPage("/DealerManager/Dashboard"),
                "EVM_STAFF" => RedirectToPage("/EVM/Dashboard"),
                "EVM_ADMIN" => RedirectToPage("/Admin/Dashboard"),
                _ => RedirectToPage("/Index")
            };
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}

