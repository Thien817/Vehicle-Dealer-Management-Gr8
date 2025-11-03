using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.BLL.Constants;
using System.Security.Cryptography;
using System.Text;

namespace Vehicle_Dealer_Management.Pages.Admin
{
    public class UsersModel : AdminPageModel
    {
        private readonly IDealerService _dealerService;

        public UsersModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            IDealerService dealerService)
            : base(context, authorizationService)
        {
            _dealerService = dealerService;
        }

        public int TotalUsers { get; set; }
        public int CustomerCount { get; set; }
        public int DealerStaffCount { get; set; }
        public int DealerManagerCount { get; set; }
        public int EvmStaffCount { get; set; }
        public int AdminCount { get; set; }

        public List<RoleViewModel> AllRoles { get; set; } = new();
        public List<DealerViewModel> AllDealers { get; set; } = new();
        public List<UserViewModel> Users { get; set; } = new();

        // Properties for Edit modal
        public int? EditUserId { get; set; }
        public string? EditFullName { get; set; }
        public string? EditEmail { get; set; }
        public string? EditPhone { get; set; }
        public int? EditRoleId { get; set; }
        public int? EditDealerId { get; set; }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }


        public async Task<IActionResult> OnGetAsync()
        {
            var authResult = await CheckAuthorizationAsync();
            if (authResult != null)
                return authResult;

            SetViewData();

            // Get all roles
            var roles = await _context.Roles.ToListAsync();
            AllRoles = roles
                .Select(r => new RoleViewModel
                {
                    Id = r.Id,
                    Code = r.Code,
                    Name = r.Name
                }).ToList();

            // Get all dealers
            var dealers = await _dealerService.GetAllDealersAsync();
            AllDealers = dealers.Select(d => new DealerViewModel
            {
                Id = d.Id,
                Name = d.Name,
                Code = d.Code ?? ""
            }).ToList();

            // Get all users
            var users = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Dealer)
                .ToListAsync();

            TotalUsers = users.Count;
            CustomerCount = users.Count(u => u.Role.Code == RoleConstants.CUSTOMER);
            DealerStaffCount = users.Count(u => u.Role.Code == RoleConstants.DEALER_STAFF);
            DealerManagerCount = users.Count(u => u.Role.Code == RoleConstants.DEALER_MANAGER);
            EvmStaffCount = users.Count(u => u.Role.Code == RoleConstants.EVM_STAFF);
            AdminCount = users.Count(u => u.Role.Code == RoleConstants.EVM_ADMIN);

            // Sắp xếp users theo ID (tăng dần: 1 -> 6)
            Users = users.Select(u => new UserViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone ?? "",
                RoleCode = u.Role.Code,
                RoleName = u.Role.Name,
                RoleId = u.RoleId,
                DealerName = u.Dealer?.Name ?? "",
                DealerId = u.DealerId,
                CreatedAt = u.CreatedAt
            })
            .OrderBy(u => u.Id) // Sắp xếp theo ID (tăng dần)
            .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostCreateUserAsync(
            string fullName,
            string email,
            string? phone,
            int roleId,
            int? dealerId)
        {
            var authResult = await CheckAuthorizationAsync();
            if (authResult != null)
                return authResult;

            // Validate email unique
            var emailExists = await _context.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
            {
                TempData["Error"] = "Email đã tồn tại trong hệ thống.";
                return RedirectToPage();
            }

            // Validate role
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
            {
                TempData["Error"] = "Vai trò không hợp lệ.";
                return RedirectToPage();
            }

            // Default password
            var defaultPassword = HashPassword("123456");

            var user = new User
            {
                FullName = fullName,
                Email = email,
                Phone = phone,
                PasswordHash = defaultPassword,
                RoleId = roleId,
                DealerId = dealerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo người dùng {fullName} thành công. Mật khẩu mặc định: 123456";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateUserAsync(
            int userId,
            string fullName,
            string email,
            string? phone,
            int roleId,
            int? dealerId)
        {
            var authResult = await CheckAuthorizationAsync();
            if (authResult != null)
                return authResult;

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToPage();
            }

            // Validate email unique (excluding current user)
            var emailExists = await _context.Users.AnyAsync(u => u.Email == email && u.Id != userId);
            if (emailExists)
            {
                TempData["Error"] = "Email đã tồn tại trong hệ thống.";
                return RedirectToPage();
            }

            // Validate role
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
            {
                TempData["Error"] = "Vai trò không hợp lệ.";
                return RedirectToPage();
            }

            // Update user
            user.FullName = fullName;
            user.Email = email;
            user.Phone = phone;
            user.RoleId = roleId;
            user.DealerId = dealerId;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật thông tin người dùng {fullName} thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(int userId)
        {
            var authResult = await CheckAuthorizationAsync();
            if (authResult != null)
                return authResult;

            // Prevent self-deletion
            if (CurrentUserId == userId)
            {
                TempData["Error"] = "Bạn không thể xóa chính mình.";
                return RedirectToPage();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToPage();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa người dùng {user.FullName} thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetUserDataAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy người dùng" });
            }

            return new JsonResult(new
            {
                success = true,
                user = new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    phone = user.Phone,
                    roleId = user.RoleId,
                    roleCode = user.Role.Code,
                    dealerId = user.DealerId
                }
            });
        }

        public class UserViewModel
        {
            public int Id { get; set; }
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";
            public string RoleCode { get; set; } = "";
            public string RoleName { get; set; } = "";
            public int RoleId { get; set; }
            public string DealerName { get; set; } = "";
            public int? DealerId { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class RoleViewModel
        {
            public int Id { get; set; }
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
        }

        public class DealerViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Code { get; set; } = "";
        }
    }
}

