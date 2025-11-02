using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;
using System.Security.Cryptography;
using System.Text;

namespace Vehicle_Dealer_Management.Pages.Auth
{
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProfileModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public new User User { get; set; } = null!;
        public string UserRole { get; set; } = "";
        public string UserName { get; set; } = "";

        public string? SuccessMessage { get; set; }
        public string? PasswordError { get; set; }
        public string? PasswordSuccess { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            User = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Dealer)
                .FirstOrDefaultAsync(u => u.Id == int.Parse(userId)) ?? new User();

            UserRole = User.Role?.Code ?? "CUSTOMER";
            UserName = User.FullName;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string fullName, string phone)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Dealer)
                .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));

            if (user != null)
            {
                user.FullName = fullName;
                user.Phone = phone;
                await _context.SaveChangesAsync();

                // Update session
                HttpContext.Session.SetString("UserName", fullName);

                SuccessMessage = "Cập nhật thông tin thành công!";
            }

            User = user ?? new User();
            UserRole = User.Role?.Code ?? "CUSTOMER";
            UserName = User.FullName;

            return Page();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Dealer)
                .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));

            if (user == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            // Verify current password
            var currentPasswordHash = HashPassword(currentPassword);
            if (user.PasswordHash != currentPasswordHash)
            {
                PasswordError = "Mật khẩu hiện tại không đúng.";
                User = user;
                UserRole = user.Role?.Code ?? "CUSTOMER";
                UserName = user.FullName;
                return Page();
            }

            // Validate new password
            if (newPassword != confirmPassword)
            {
                PasswordError = "Mật khẩu mới và xác nhận không khớp.";
                User = user;
                UserRole = user.Role?.Code ?? "CUSTOMER";
                UserName = user.FullName;
                return Page();
            }

            if (newPassword.Length < 6)
            {
                PasswordError = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                User = user;
                UserRole = user.Role?.Code ?? "CUSTOMER";
                UserName = user.FullName;
                return Page();
            }

            // Update password
            user.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();

            PasswordSuccess = "Đổi mật khẩu thành công!";
            User = user;
            UserRole = user.Role?.Code ?? "CUSTOMER";
            UserName = user.FullName;

            return Page();
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}

