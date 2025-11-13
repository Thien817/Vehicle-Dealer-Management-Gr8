using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.DealerManager.Reports
{
    public class SalesByStaffModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SalesByStaffModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? StaffId { get; set; }

        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public int TotalVehiclesSold { get; set; }

        public List<StaffViewModel> AllStaff { get; set; } = new();
        public List<StaffReportViewModel> StaffReports { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(DateTime? fromDate, DateTime? toDate, int? staffId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var dealerId = HttpContext.Session.GetString("DealerId");
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var rangeStart = (fromDate ?? DateTime.Today.AddMonths(-1)).Date;
            var rangeEndInclusive = (toDate ?? DateTime.Today).Date;
            var rangeEndExclusive = rangeEndInclusive.AddDays(1);

            FromDate = rangeStart;
            ToDate = rangeEndInclusive;
            StaffId = staffId;

            var dealerIdInt = int.Parse(dealerId);

            // Get all staff in this dealer
            var staffQuery = _context.Users
                .Where(u => u.DealerId == dealerIdInt && 
                           (u.Role.Code == "DEALER_STAFF" || u.Role.Code == "DEALER_MANAGER"));

            AllStaff = await staffQuery
                .Select(u => new StaffViewModel
                {
                    Id = u.Id,
                    Name = u.FullName,
                    Email = u.Email
                })
                .ToListAsync();

            // Get sales documents in date range
            var salesQuery = _context.SalesDocuments
                .Where(s => s.DealerId == dealerIdInt &&
                           s.Type == "ORDER" &&
                           s.CreatedAt >= rangeStart &&
                           s.CreatedAt < rangeEndExclusive);

            if (staffId.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.CreatedBy == staffId.Value);
            }

            var salesDocs = await salesQuery
                .Include(s => s.Lines)
                .Include(s => s.CreatedByUser)
                .ToListAsync();

            // Group by staff
            var staffSales = salesDocs
                .GroupBy(s => new { s.CreatedBy, StaffName = s.CreatedByUser?.FullName ?? "N/A", StaffEmail = s.CreatedByUser?.Email ?? "N/A" })
                .Select(g => new StaffReportViewModel
                {
                    StaffId = g.Key.CreatedBy,
                    Name = g.Key.StaffName,
                    Email = g.Key.StaffEmail,
                    OrderCount = g.Count(),
                    VehiclesSold = (int)g.Sum(s => s.Lines?.Sum(l => l.Qty) ?? 0m),
                    Sales = g.Sum(s => s.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0m)
                })
                .ToList();

            var staffSalesLookup = staffSales.ToDictionary(s => s.StaffId);

            var relevantStaff = StaffId.HasValue
                ? AllStaff.Where(s => s.Id == StaffId.Value).ToList()
                : AllStaff.ToList();

            var reports = new List<StaffReportViewModel>();

            foreach (var staff in relevantStaff)
            {
                if (staffSalesLookup.TryGetValue(staff.Id, out var summary))
                {
                    reports.Add(summary);
                }
                else
                {
                    reports.Add(new StaffReportViewModel
                    {
                        StaffId = staff.Id,
                        Name = staff.Name,
                        Email = staff.Email,
                        OrderCount = 0,
                        VehiclesSold = 0,
                        Sales = 0m,
                        AvgOrderValue = 0m
                    });
                }
            }

            var missingStaffReports = staffSales
                .Where(s => relevantStaff.All(st => st.Id != s.StaffId))
                .ToList();
            reports.AddRange(missingStaffReports);

            reports = reports
                .OrderByDescending(s => s.Sales)
                .ThenBy(s => s.Name)
                .ToList();

            int rank = 1;
            foreach (var staff in reports)
            {
                staff.Rank = rank++;
                staff.AvgOrderValue = staff.OrderCount > 0
                    ? Math.Round(staff.Sales / staff.OrderCount, 2, MidpointRounding.AwayFromZero)
                    : 0m;
            }

            StaffReports = reports;

            TotalSales = reports.Sum(s => s.Sales);
            TotalOrders = reports.Sum(s => s.OrderCount);
            TotalVehiclesSold = reports.Sum(s => s.VehiclesSold);

            return Page();
        }

        public class StaffViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
        }

        public class StaffReportViewModel
        {
            public int StaffId { get; set; }
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public int OrderCount { get; set; }
            public int VehiclesSold { get; set; }
            public decimal Sales { get; set; }
            public decimal AvgOrderValue { get; set; }
            public int Rank { get; set; }
        }
    }
}

