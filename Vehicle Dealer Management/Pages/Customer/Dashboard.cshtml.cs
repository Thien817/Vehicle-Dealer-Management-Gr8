using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.Pages.Customer
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ISalesDocumentService _salesDocumentService;
        private readonly IVehicleService _vehicleService;
        private readonly ITestDriveService _testDriveService;
        private readonly IPaymentService _paymentService;

        public DashboardModel(
            ApplicationDbContext context,
            ISalesDocumentService salesDocumentService,
            IVehicleService vehicleService,
            ITestDriveService testDriveService,
            IPaymentService paymentService)
        {
            _context = context;
            _salesDocumentService = salesDocumentService;
            _vehicleService = vehicleService;
            _testDriveService = testDriveService;
            _paymentService = paymentService;
        }

        public string CustomerName { get; set; } = "";
        public int QuotesCount { get; set; }
        public int OrdersCount { get; set; }
        public int TestDrivesCount { get; set; }
        public int AvailableVehicles { get; set; }
        public decimal TotalOutstandingDebt { get; set; } = 0; // Tổng dư nợ
        public List<OrderViewModel> RecentOrders { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            CustomerName = user.FullName;

            // Get customer profile
            var customerProfile = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customerProfile != null)
            {
                // Get quotes and orders using service
                var quotes = await _salesDocumentService.GetSalesDocumentsByCustomerIdAsync(customerProfile.Id, "QUOTE");
                QuotesCount = quotes.Count();

                var orders = await _salesDocumentService.GetSalesDocumentsByCustomerIdAsync(customerProfile.Id, "ORDER");
                OrdersCount = orders.Count();

                var testDrives = await _testDriveService.GetTestDrivesByCustomerIdAsync(customerProfile.Id);
                TestDrivesCount = testDrives.Count();

                // Get recent orders (already include Payments via GetSalesDocumentsByCustomerIdAsync)
                var recentOrders = orders
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(5)
                    .ToList();

                // Calculate from navigation properties to avoid concurrent DbContext access
                RecentOrders = recentOrders.Select(o =>
                {
                    var totalPaid = o.Payments?.Sum(p => p.Amount) ?? 0;
                    return new OrderViewModel
                    {
                        Id = o.Id,
                        CreatedAt = o.CreatedAt,
                        Status = o.Status,
                        TotalAmount = o.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0,
                        TotalPaid = totalPaid
                    };
                }).ToList();

                // Tính tổng dư nợ từ tất cả đơn hàng
                TotalOutstandingDebt = orders
                    .Select(o =>
                    {
                        var totalAmount = o.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0;
                        var totalPaid = o.Payments?.Sum(p => p.Amount) ?? 0;
                        return totalAmount - totalPaid;
                    })
                    .Where(debt => debt > 0)
                    .Sum();
            }

            var availableVehicles = await _vehicleService.GetAvailableVehiclesAsync();
            AvailableVehicles = availableVehicles.Count();

            return Page();
        }

        public class OrderViewModel
        {
            public int Id { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public decimal TotalPaid { get; set; }
        }
    }
}

