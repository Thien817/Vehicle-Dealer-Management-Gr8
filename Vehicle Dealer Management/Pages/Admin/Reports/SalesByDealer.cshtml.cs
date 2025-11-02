using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vehicle_Dealer_Management.Pages.Admin.Reports
{
    public class SalesByDealerModel : PageModel
    {
        public List<DealerReportViewModel> DealerReports { get; set; } = new();

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Mock data for report
            DealerReports = new List<DealerReportViewModel>
            {
                new() { Name = "Đại lý Hà Nội", Region = "Miền Bắc", OrderCount = 45, Sales = 3800000000, TargetPercent = 95, Growth = 24 },
                new() { Name = "Đại lý TP.HCM", Region = "Miền Nam", OrderCount = 42, Sales = 3600000000, TargetPercent = 90, Growth = 18 },
                new() { Name = "Đại lý Đà Nẵng", Region = "Miền Trung", OrderCount = 35, Sales = 2900000000, TargetPercent = 82, Growth = 15 },
                new() { Name = "Đại lý Cần Thơ", Region = "Miền Nam", OrderCount = 28, Sales = 2200000000, TargetPercent = 73, Growth = 12 },
                new() { Name = "Đại lý Hải Phòng", Region = "Miền Bắc", OrderCount = 22, Sales = 1800000000, TargetPercent = 60, Growth = -5 }
            };

            return Page();
        }

        public class DealerReportViewModel
        {
            public string Name { get; set; } = "";
            public string Region { get; set; } = "";
            public int OrderCount { get; set; }
            public decimal Sales { get; set; }
            public int TargetPercent { get; set; }
            public int Growth { get; set; }
        }
    }
}

