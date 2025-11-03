using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.Pages.Dealer.Sales
{
    public class OrdersModel : PageModel
    {
        private readonly ISalesDocumentService _salesDocumentService;
        private readonly IPaymentService _paymentService;

        public OrdersModel(
            ISalesDocumentService salesDocumentService,
            IPaymentService paymentService)
        {
            _salesDocumentService = salesDocumentService;
            _paymentService = paymentService;
        }

        public string StatusFilter { get; set; } = "all";
        public int TotalOrders { get; set; }
        public int OpenOrders { get; set; }
        public int PaidOrders { get; set; }
        public int DeliveredOrders { get; set; }

        public List<OrderViewModel> Orders { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string? status)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Set UserRole from Session for proper navigation
            ViewData["UserRole"] = HttpContext.Session.GetString("UserRole") ?? "DEALER_STAFF";
            ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "User";

            StatusFilter = status ?? "all";
            var dealerIdInt = int.Parse(dealerId);

            // Get orders using Service
            var orders = await _salesDocumentService.GetSalesDocumentsByDealerIdAsync(
                dealerIdInt,
                type: "ORDER",
                status: StatusFilter != "all" ? StatusFilter : null);

            // Calculate counts from the orders list
            var allOrders = await _salesDocumentService.GetSalesDocumentsByDealerIdAsync(
                dealerIdInt,
                type: "ORDER",
                status: null);

            TotalOrders = allOrders.Count();
            OpenOrders = allOrders.Count(o => o.Status == "OPEN");
            PaidOrders = allOrders.Count(o => o.Status == "PAID");
            DeliveredOrders = allOrders.Count(o => o.Status == "DELIVERED");

            // Load tất cả payments trong một query để tránh concurrent DbContext access
            var orderIds = orders.Select(o => o.Id).ToList();
            var allPayments = await _paymentService.GetPaymentsBySalesDocumentIdsAsync(orderIds);
            var paidAmountsDict = allPayments
                .GroupBy(p => p.SalesDocumentId)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

            // Xử lý tuần tự để tránh concurrent DbContext access
            Orders = orders.Select(o =>
            {
                var totalAmount = o.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0;
                var paidAmount = paidAmountsDict.GetValueOrDefault(o.Id, 0);
                var remainingAmount = totalAmount - paidAmount;
                
                // Xác định trạng thái hiển thị: nếu đã DELIVERED nhưng còn dư nợ, hiển thị "Còn dư nợ"
                // Lấy thông tin delivery để kiểm tra
                var delivery = o.Delivery;
                var displayStatus = o.Status;
                var isDelivered = o.Status == "DELIVERED" || 
                                  (delivery != null && delivery.Status == "DELIVERED") || 
                                  (delivery != null && delivery.CustomerConfirmed);
                
                if (isDelivered && remainingAmount > 0)
                {
                    displayStatus = "DEBT"; // Trạng thái đặc biệt cho "Còn dư nợ"
                }
                
                return new OrderViewModel
                {
                    Id = o.Id,
                    CustomerName = o.Customer?.FullName ?? "N/A",
                    CustomerPhone = o.Customer?.Phone ?? "N/A",
                    CreatedAt = o.CreatedAt,
                    VehicleCount = (int)(o.Lines?.Sum(l => (decimal?)l.Qty) ?? 0),
                    TotalAmount = totalAmount,
                    PaidAmount = paidAmount,
                    RemainingAmount = remainingAmount,
                    Status = o.Status,
                    DisplayStatus = displayStatus
                };
            }).ToList();

            return Page();
        }

        public class OrderViewModel
        {
            public int Id { get; set; }
            public string CustomerName { get; set; } = "";
            public string CustomerPhone { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public int VehicleCount { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal RemainingAmount { get; set; }
            public string Status { get; set; } = "";
            public string DisplayStatus { get; set; } = ""; // Trạng thái hiển thị (có thể khác Status nếu còn dư nợ)
        }
    }
}

