using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.DealerManager
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserName { get; set; } = "";
        public string DealerName { get; set; } = "";
        public decimal MonthSales { get; set; }
        public int TotalOrders { get; set; }
        public int TotalStaff { get; set; }
        public int ActiveStaff { get; set; }
        public decimal TotalDebt { get; set; }

        public List<StaffPerformanceViewModel> StaffPerformance { get; set; } = new();
        public List<CustomerDebtViewModel> CustomerDebts { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var dealerId = HttpContext.Session.GetString("DealerId");
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _context.Users
                .Include(u => u.Dealer)
                .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));

            if (user == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            UserName = user.FullName;
            DealerName = user.Dealer?.Name ?? "Đại lý";

            var dealerIdInt = int.Parse(dealerId);

            // Mock data for statistics
            MonthSales = 3500000000; // 3.5 billion
            TotalOrders = 42;
            TotalStaff = 8;
            ActiveStaff = 7;
            TotalDebt = 250000000; // 250 million

            // Mock staff performance
            StaffPerformance = new List<StaffPerformanceViewModel>
            {
                new() { Name = "Nguyễn Văn A", OrderCount = 15, Sales = 1200000000, TargetPercent = 95, Rank = 1 },
                new() { Name = "Trần Thị B", OrderCount = 12, Sales = 980000000, TargetPercent = 82, Rank = 2 },
                new() { Name = "Lê Văn C", OrderCount = 10, Sales = 850000000, TargetPercent = 71, Rank = 3 },
                new() { Name = "Phạm Thị D", OrderCount = 5, Sales = 470000000, TargetPercent = 47, Rank = 4 }
            };

            // Mock customer debts
            CustomerDebts = new List<CustomerDebtViewModel>
            {
                new() { CustomerName = "Nguyễn Văn X", Amount = 150000000, DueDate = DateTime.Now.AddDays(5) },
                new() { CustomerName = "Trần Thị Y", Amount = 100000000, DueDate = DateTime.Now.AddDays(-2) }
            };

            return Page();
        }

        public class StaffPerformanceViewModel
        {
            public string Name { get; set; } = "";
            public int OrderCount { get; set; }
            public decimal Sales { get; set; }
            public int TargetPercent { get; set; }
            public int Rank { get; set; }
        }

        public class CustomerDebtViewModel
        {
            public string CustomerName { get; set; } = "";
            public decimal Amount { get; set; }
            public DateTime DueDate { get; set; }
        }
    }
}

