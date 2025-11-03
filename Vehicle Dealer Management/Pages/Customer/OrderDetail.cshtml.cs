using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.Pages.Customer
{
    public class OrderDetailModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ISalesDocumentService _salesDocumentService;
        private readonly IPaymentService _paymentService;
        private readonly IDeliveryService _deliveryService;
        private readonly IFeedbackService _feedbackService;

        public OrderDetailModel(
            ApplicationDbContext context,
            ISalesDocumentService salesDocumentService,
            IPaymentService paymentService,
            IDeliveryService deliveryService,
            IFeedbackService feedbackService)
        {
            _context = context;
            _salesDocumentService = salesDocumentService;
            _paymentService = paymentService;
            _deliveryService = deliveryService;
            _feedbackService = feedbackService;
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
            var order = await _salesDocumentService.GetSalesDocumentWithDetailsAsync(id);

            if (order == null || order.CustomerId != customer.Id || order.Type != "ORDER")
            {
                return NotFound();
            }

            // Calculate totals from real data
            var totalAmount = order.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0;
            var paidAmount = await _paymentService.GetTotalPaidAmountAsync(id);
            var remainingAmount = totalAmount - paidAmount;

            // Get delivery info
            var delivery = await _deliveryService.GetDeliveryBySalesDocumentIdAsync(id);

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
                Payments = (await _paymentService.GetPaymentsBySalesDocumentIdAsync(id))
                    .OrderByDescending(p => p.PaidAt)
                    .Select(p => new PaymentViewModel
                    {
                        Method = p.Method,
                        Amount = p.Amount,
                        PaidAt = p.PaidAt
                    }).ToList(),

                // Delivery
                Delivery = delivery != null ? new DeliveryViewModel
                {
                    Id = delivery.Id,
                    ScheduledDate = delivery.ScheduledDate,
                    DeliveredDate = delivery.DeliveredDate,
                    Status = delivery.Status,
                    HandoverNote = delivery.HandoverNote,
                    CustomerConfirmed = delivery.CustomerConfirmed,
                    CustomerConfirmedDate = delivery.CustomerConfirmedDate
                } : null,

                // Totals
                SubTotal = totalAmount,
                TotalPaid = paidAmount,
                RemainingAmount = remainingAmount,
                TotalAmount = totalAmount
            };

            // Check if order has been reviewed
            var review = await _feedbackService.GetReviewByOrderIdAsync(id);
            Order.HasReview = review != null;
            if (review != null)
            {
                Order.Review = new ReviewViewModel
                {
                    Id = review.Id,
                    Rating = review.Rating ?? 0,
                    Content = review.Content,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostConfirmReceiptAsync(int id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var userIdInt = int.Parse(userId);
            var customer = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == userIdInt);

            if (customer == null)
            {
                return RedirectToPage("/Auth/Profile");
            }

            var order = await _salesDocumentService.GetSalesDocumentWithDetailsAsync(id);
            if (order == null || order.CustomerId != customer.Id || order.Type != "ORDER")
            {
                return NotFound();
            }

            var delivery = await _deliveryService.GetDeliveryBySalesDocumentIdAsync(order.Id);
            if (delivery == null)
            {
                TempData["Error"] = "Chưa có thông tin giao xe.";
                return RedirectToPage(new { id });
            }

            try
            {
                await _deliveryService.CustomerConfirmReceiptAsync(delivery.Id);
                TempData["Success"] = "Đã xác nhận nhận xe thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteReviewAsync(int orderId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var userIdInt = int.Parse(userId);
            var customer = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == userIdInt);

            if (customer == null)
            {
                return RedirectToPage("/Auth/Profile");
            }

            var order = await _salesDocumentService.GetSalesDocumentWithDetailsAsync(orderId);
            if (order == null || order.CustomerId != customer.Id || order.Type != "ORDER")
            {
                return NotFound();
            }

            var review = await _feedbackService.GetReviewByOrderIdAsync(orderId);
            if (review == null || review.CustomerId != customer.Id)
            {
                TempData["Error"] = "Không tìm thấy đánh giá hoặc bạn không có quyền xóa đánh giá này.";
                return RedirectToPage(new { id = orderId });
            }

            try
            {
                await _feedbackService.DeleteReviewAsync(review.Id);
                TempData["Success"] = "Đã xóa đánh giá thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToPage(new { id = orderId });
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

            // Review
            public bool HasReview { get; set; }
            public ReviewViewModel? Review { get; set; }
        }

        public class ReviewViewModel
        {
            public int Id { get; set; }
            public int Rating { get; set; }
            public string? Content { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
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
            public int Id { get; set; }
            public DateTime ScheduledDate { get; set; }
            public DateTime? DeliveredDate { get; set; }
            public string Status { get; set; } = "";
            public string? HandoverNote { get; set; }
            public bool CustomerConfirmed { get; set; }
            public DateTime? CustomerConfirmedDate { get; set; }
        }
    }
}

