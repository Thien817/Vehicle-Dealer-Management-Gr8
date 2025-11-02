using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.Admin
{
    public class UsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public UsersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalUsers { get; set; }
        public int CustomerCount { get; set; }
        public int DealerStaffCount { get; set; }
        public int EvmStaffCount { get; set; }
        public int AdminCount { get; set; }

        public List<string> AllRoles { get; set; } = new();
        public List<string> AllDealers { get; set; } = new();
        public List<UserViewModel> Users { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Get all roles
            AllRoles = await _context.Roles
                .Select(r => r.Name)
                .ToListAsync();

            // Get all dealers
            AllDealers = await _context.Dealers
                .Select(d => d.Name)
                .ToListAsync();

            // Get all users
            var users = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Dealer)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            TotalUsers = users.Count;
            CustomerCount = users.Count(u => u.Role.Code == "CUSTOMER");
            DealerStaffCount = users.Count(u => u.Role.Code == "DEALER_STAFF" || u.Role.Code == "DEALER_MANAGER");
            EvmStaffCount = users.Count(u => u.Role.Code == "EVM_STAFF");
            AdminCount = users.Count(u => u.Role.Code == "EVM_ADMIN");

            Users = users.Select(u => new UserViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                RoleCode = u.Role.Code,
                RoleName = u.Role.Name,
                DealerName = u.Dealer?.Name ?? "",
                CreatedAt = u.CreatedAt
            }).ToList();

            return Page();
        }

        public class UserViewModel
        {
            public int Id { get; set; }
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";
            public string RoleCode { get; set; } = "";
            public string RoleName { get; set; } = "";
            public string DealerName { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }
    }
}

