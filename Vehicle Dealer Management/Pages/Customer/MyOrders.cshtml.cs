using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.Customer
{
    public class MyOrdersModel : PageModel
    {
        private readonly ISalesDocumentService _salesDocumentService;
        private readonly IPaymentService _paymentService;
        private readonly ApplicationDbContext _context; // Cần cho CustomerProfile

        public MyOrdersModel(
            ISalesDocumentService salesDocumentService,
            IPaymentService paymentService,
            ApplicationDbContext context)
        {
            _salesDocumentService = salesDocumentService;
            _paymentService = paymentService;
            _context = context;
        }

        public List<OrderViewModel> Orders { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Get customer profile
            var customerProfile = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == int.Parse(userId));

            if (customerProfile == null)
            {
                return Page();
            }

            // Get orders using Service
            var orders = await _salesDocumentService.GetSalesDocumentsByCustomerIdAsync(customerProfile.Id, "ORDER");

            var ordersList = new List<OrderViewModel>();
            foreach (var o in orders)
            {
                var totalAmount = o.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0;
                var paidAmount = await _paymentService.GetTotalPaidAmountAsync(o.Id);
                ordersList.Add(new OrderViewModel
                {
                    Id = o.Id,
                    CreatedAt = o.CreatedAt,
                    VehicleCount = (int)(o.Lines?.Sum(l => (decimal?)l.Qty) ?? 0),
                    TotalAmount = totalAmount,
                    PaidAmount = paidAmount,
                    RemainingAmount = totalAmount - paidAmount,
                    Status = o.Status,
                    DeliveryDate = o.Delivery?.ScheduledDate,
                    DeliveryStatus = o.Delivery?.Status,
                    CustomerConfirmed = o.Delivery?.CustomerConfirmed ?? false
                });
            }
            Orders = ordersList;

            return Page();
        }

        public class OrderViewModel
        {
            public int Id { get; set; }
            public DateTime CreatedAt { get; set; }
            public int VehicleCount { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal RemainingAmount { get; set; }
            public string Status { get; set; } = "";
            public DateTime? DeliveryDate { get; set; }
            public string? DeliveryStatus { get; set; }
            public bool CustomerConfirmed { get; set; }
        }
    }
}

