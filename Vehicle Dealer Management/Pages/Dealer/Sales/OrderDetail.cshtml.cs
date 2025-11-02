using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.Dealer.Sales
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
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Set UserRole from Session for proper navigation
            ViewData["UserRole"] = HttpContext.Session.GetString("UserRole") ?? "DEALER_STAFF";
            ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "User";

            var dealerIdInt = int.Parse(dealerId);

            // Get order with all related data from DB
            var order = await _context.SalesDocuments
                .Include(s => s.Customer)
                .Include(s => s.Dealer)
                .Include(s => s.Promotion)
                .Include(s => s.CreatedByUser)
                .Include(s => s.Lines!)
                    .ThenInclude(l => l.Vehicle)
                .Include(s => s.Payments)
                .Include(s => s.Delivery)
                .FirstOrDefaultAsync(s => s.Id == id && s.DealerId == dealerIdInt && s.Type == "ORDER");

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
                SignedAt = order.SignedAt,

                // Customer Info
                CustomerId = order.CustomerId,
                CustomerName = order.Customer?.FullName ?? "N/A",
                CustomerPhone = order.Customer?.Phone ?? "N/A",
                CustomerEmail = order.Customer?.Email ?? "N/A",
                CustomerAddress = order.Customer?.Address ?? "N/A",

                // Dealer Info
                DealerName = order.Dealer?.Name ?? "N/A",
                DealerAddress = order.Dealer?.Address ?? "N/A",

                // Created By
                CreatedBy = order.CreatedByUser?.FullName ?? "N/A",

                // Promotion
                PromotionId = order.PromotionId,
                PromotionName = order.Promotion?.Name,

                // Items (Lines)
                Items = order.Lines?.Select(l => new OrderItemViewModel
                {
                    Id = l.Id,
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
                    Id = p.Id,
                    Method = p.Method,
                    Amount = p.Amount,
                    PaidAt = p.PaidAt,
                    MetaJson = p.MetaJson
                }).ToList() ?? new List<PaymentViewModel>(),

                // Delivery
                Delivery = order.Delivery != null ? new DeliveryViewModel
                {
                    Id = order.Delivery.Id,
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

        public async Task<IActionResult> OnPostAddPaymentAsync(int id, string method, decimal amount, string? metaJson)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var dealerIdInt = int.Parse(dealerId);

            // Get order
            var order = await _context.SalesDocuments
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == id && s.DealerId == dealerIdInt && s.Type == "ORDER");

            if (order == null)
            {
                return NotFound();
            }

            // Validate amount
            if (amount <= 0)
            {
                TempData["Error"] = "Số tiền phải lớn hơn 0";
                return RedirectToPage(new { id });
            }

            // Calculate remaining amount
            var totalAmount = order.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0;
            var paidAmount = order.Payments?.Sum(p => p.Amount) ?? 0;
            var remainingAmount = totalAmount - paidAmount;

            if (amount > remainingAmount)
            {
                TempData["Error"] = $"Số tiền không được vượt quá số tiền còn lại: {remainingAmount:N0} VND";
                return RedirectToPage(new { id });
            }

            // Create payment
            var payment = new Vehicle_Dealer_Management.DAL.Models.Payment
            {
                SalesDocumentId = order.Id,
                Method = method ?? "CASH",
                Amount = amount,
                MetaJson = metaJson,
                PaidAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            // Update order status if fully paid
            var newPaidAmount = paidAmount + amount;
            if (newPaidAmount >= totalAmount && order.Status == "OPEN")
            {
                order.Status = "PAID";
                order.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm thanh toán thành công!";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostScheduleDeliveryAsync(int id, DateTime scheduledDate, string? scheduledTime)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var dealerIdInt = int.Parse(dealerId);

            // Get order
            var order = await _context.SalesDocuments
                .Include(s => s.Delivery)
                .FirstOrDefaultAsync(s => s.Id == id && s.DealerId == dealerIdInt && s.Type == "ORDER");

            if (order == null)
            {
                return NotFound();
            }

            // Combine date and time
            DateTime scheduledDateTime = scheduledDate.Date;
            if (!string.IsNullOrEmpty(scheduledTime) && TimeSpan.TryParse(scheduledTime, out var time))
            {
                scheduledDateTime = scheduledDate.Date.Add(time);
            }
            else
            {
                scheduledDateTime = scheduledDate.Date.AddHours(9); // Default 9:00 AM
            }

            // Validate date
            if (scheduledDateTime < DateTime.Now)
            {
                TempData["Error"] = "Ngày giờ giao không được trong quá khứ";
                return RedirectToPage(new { id });
            }

            // Create or update delivery
            if (order.Delivery == null)
            {
                order.Delivery = new Vehicle_Dealer_Management.DAL.Models.Delivery
                {
                    SalesDocumentId = order.Id,
                    ScheduledDate = scheduledDateTime,
                    Status = "SCHEDULED",
                    CreatedDate = DateTime.UtcNow
                };
                _context.Deliveries.Add(order.Delivery);
            }
            else
            {
                order.Delivery.ScheduledDate = scheduledDateTime;
                order.Delivery.Status = "SCHEDULED";
            }

            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Lên lịch giao xe thành công!";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostMarkDeliveredAsync(int id, string? handoverNote)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var dealerIdInt = int.Parse(dealerId);

            // Get order with delivery
            var order = await _context.SalesDocuments
                .Include(s => s.Delivery)
                .FirstOrDefaultAsync(s => s.Id == id && s.DealerId == dealerIdInt && s.Type == "ORDER");

            if (order == null || order.Delivery == null)
            {
                return NotFound();
            }

            // Mark as delivered
            order.Delivery.DeliveredDate = DateTime.UtcNow;
            order.Delivery.Status = "DELIVERED";
            order.Delivery.HandoverNote = handoverNote;

            // Update order status if not already delivered
            if (order.Status != "DELIVERED")
            {
                order.Status = "DELIVERED";
                order.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Xác nhận giao xe thành công!";
            return RedirectToPage(new { id });
        }

        public class OrderDetailViewModel
        {
            public int Id { get; set; }
            public string OrderNumber { get; set; } = "";
            public string Status { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public DateTime? SignedAt { get; set; }

            // Customer
            public int CustomerId { get; set; }
            public string CustomerName { get; set; } = "";
            public string CustomerPhone { get; set; } = "";
            public string CustomerEmail { get; set; } = "";
            public string CustomerAddress { get; set; } = "";

            // Dealer
            public string DealerName { get; set; } = "";
            public string DealerAddress { get; set; } = "";

            // Created By
            public string CreatedBy { get; set; } = "";

            // Promotion
            public int? PromotionId { get; set; }
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
            public int Id { get; set; }
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
            public int Id { get; set; }
            public string Method { get; set; } = "";
            public decimal Amount { get; set; }
            public DateTime PaidAt { get; set; }
            public string? MetaJson { get; set; }
        }

        public class DeliveryViewModel
        {
            public int Id { get; set; }
            public DateTime ScheduledDate { get; set; }
            public DateTime? DeliveredDate { get; set; }
            public string Status { get; set; } = "";
            public string? HandoverNote { get; set; }
        }
    }
}

