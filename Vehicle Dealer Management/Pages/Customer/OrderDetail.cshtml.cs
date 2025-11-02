using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.Customer
{
    public class OrderDetailModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public OrderDetailModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public OrderDetailViewModel Order { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            ViewData["UserRole"] = HttpContext.Session.GetString("UserRole") ?? "CUSTOMER";
            ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "Customer";

            var userIdInt = int.Parse(userId);

            // Get customer profile from user
            var customer = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == userIdInt);

            if (customer == null)
            {
                return RedirectToPage("/Auth/Profile");
            }

            // Get order with all related data from DB - only orders belonging to this customer
            var order = await _context.SalesDocuments
                .Include(s => s.Customer)
                .Include(s => s.Dealer)
                .Include(s => s.Promotion)
                .Include(s => s.Lines!)
                    .ThenInclude(l => l.Vehicle)
                .Include(s => s.Payments)
                .Include(s => s.Delivery)
                .FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customer.Id && s.Type == "ORDER");

            if (order == null)
            {
                return NotFound();
            }

            // Calculate totals from real data
            var totalAmount = order.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0;
            var paidAmount = order.Payments?.Sum(p => p.Amount) ?? 0;
            var remainingAmount = totalAmount - paidAmount;

            Order = new OrderDetailViewModel
            {
                Id = order.Id,
                OrderNumber = $"ORD-{order.Id:D6}",
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,

                // Dealer Info
                DealerName = order.Dealer?.Name ?? "N/A",
                DealerAddress = order.Dealer?.Address ?? "N/A",
                DealerPhone = order.Dealer?.PhoneNumber ?? "N/A",

                // Promotion
                PromotionName = order.Promotion?.Name,

                // Items (Lines)
                Items = order.Lines?.Select(l => new OrderItemViewModel
                {
                    VehicleId = l.VehicleId,
                    VehicleModel = l.Vehicle?.ModelName ?? "N/A",
                    VehicleVariant = l.Vehicle?.VariantName ?? "N/A",
                    ColorCode = l.ColorCode,
                    Qty = l.Qty,
                    UnitPrice = l.UnitPrice,
                    DiscountValue = l.DiscountValue,
                    LineTotal = l.UnitPrice * l.Qty - l.DiscountValue,
                    VehicleImageUrl = l.Vehicle?.ImageUrl
                }).ToList() ?? new List<OrderItemViewModel>(),

                // Payments
                Payments = order.Payments?.OrderByDescending(p => p.PaidAt).Select(p => new PaymentViewModel
                {
                    Method = p.Method,
                    Amount = p.Amount,
                    PaidAt = p.PaidAt
                }).ToList() ?? new List<PaymentViewModel>(),

                // Delivery
                Delivery = order.Delivery != null ? new DeliveryViewModel
                {
                    ScheduledDate = order.Delivery.ScheduledDate,
                    DeliveredDate = order.Delivery.DeliveredDate,
                    Status = order.Delivery.Status,
                    HandoverNote = order.Delivery.HandoverNote
                } : null,

                // Totals
                SubTotal = totalAmount,
                TotalPaid = paidAmount,
                RemainingAmount = remainingAmount,
                TotalAmount = totalAmount
            };

            return Page();
        }

        public class OrderDetailViewModel
        {
            public int Id { get; set; }
            public string OrderNumber { get; set; } = "";
            public string Status { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }

            // Dealer
            public string DealerName { get; set; } = "";
            public string DealerAddress { get; set; } = "";
            public string DealerPhone { get; set; } = "";

            // Promotion
            public string? PromotionName { get; set; }

            // Items
            public List<OrderItemViewModel> Items { get; set; } = new();

            // Payments
            public List<PaymentViewModel> Payments { get; set; } = new();

            // Delivery
            public DeliveryViewModel? Delivery { get; set; }

            // Totals
            public decimal SubTotal { get; set; }
            public decimal TotalPaid { get; set; }
            public decimal RemainingAmount { get; set; }
            public decimal TotalAmount { get; set; }
        }

        public class OrderItemViewModel
        {
            public int VehicleId { get; set; }
            public string VehicleModel { get; set; } = "";
            public string VehicleVariant { get; set; } = "";
            public string ColorCode { get; set; } = "";
            public decimal Qty { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal DiscountValue { get; set; }
            public decimal LineTotal { get; set; }
            public string? VehicleImageUrl { get; set; }
        }

        public class PaymentViewModel
        {
            public string Method { get; set; } = "";
            public decimal Amount { get; set; }
            public DateTime PaidAt { get; set; }
        }

        public class DeliveryViewModel
        {
            public DateTime ScheduledDate { get; set; }
            public DateTime? DeliveredDate { get; set; }
            public string Status { get; set; } = "";
            public string? HandoverNote { get; set; }
        }
    }
}

