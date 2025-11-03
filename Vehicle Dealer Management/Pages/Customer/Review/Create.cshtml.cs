using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.Pages.Customer.Review
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeedbackService _feedbackService;
        private readonly ISalesDocumentService _salesDocumentService;

        public CreateModel(
            ApplicationDbContext context,
            IFeedbackService feedbackService,
            ISalesDocumentService salesDocumentService)
        {
            _context = context;
            _feedbackService = feedbackService;
            _salesDocumentService = salesDocumentService;
        }

        [BindProperty]
        public int OrderId { get; set; }

        [BindProperty]
        public int Rating { get; set; } = 5;

        [BindProperty]
        public string? Content { get; set; }

        public OrderInfoViewModel OrderInfo { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int orderId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            ViewData["UserRole"] = HttpContext.Session.GetString("UserRole") ?? "CUSTOMER";
            ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "Customer";

            var userIdInt = int.Parse(userId);
            var customer = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == userIdInt);

            if (customer == null)
            {
                return RedirectToPage("/Auth/Profile");
            }

            // Set OrderId from query parameter
            OrderId = orderId;

            var order = await _salesDocumentService.GetSalesDocumentWithDetailsAsync(OrderId);
            if (order == null || order.CustomerId != customer.Id || order.Type != "ORDER")
            {
                return NotFound();
            }

            // Check if order is completed (DELIVERED and fully paid)
            if (order.Status != "DELIVERED")
            {
                TempData["Error"] = "Chỉ có thể đánh giá đơn hàng đã hoàn thành.";
                return RedirectToPage("/Customer/OrderDetail", new { id = OrderId });
            }

            // Check if review already exists
            var existingReview = await _feedbackService.GetReviewByOrderIdAsync(OrderId);
            if (existingReview != null)
            {
                TempData["Error"] = "Đơn hàng này đã có đánh giá. Vui lòng chỉnh sửa đánh giá hiện có.";
                return RedirectToPage("/Customer/Review/Edit", new { orderId = OrderId });
            }

            OrderInfo = new OrderInfoViewModel
            {
                Id = order.Id,
                OrderNumber = $"ORD-{order.Id:D6}",
                DealerName = order.Dealer?.Name ?? "N/A"
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
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

            var order = await _salesDocumentService.GetSalesDocumentWithDetailsAsync(OrderId);
            if (order == null || order.CustomerId != customer.Id || order.Type != "ORDER")
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                OrderInfo = new OrderInfoViewModel
                {
                    Id = order.Id,
                    OrderNumber = $"ORD-{order.Id:D6}",
                    DealerName = order.Dealer?.Name ?? "N/A"
                };
                return Page();
            }

            try
            {
                var review = new Vehicle_Dealer_Management.DAL.Models.Feedback
                {
                    Type = "REVIEW",
                    OrderId = OrderId,
                    CustomerId = customer.Id,
                    DealerId = order.DealerId,
                    Rating = Rating,
                    Content = Content ?? ""
                };

                await _feedbackService.CreateReviewAsync(review);
                TempData["Success"] = "Đánh giá đã được tạo thành công!";
                return RedirectToPage("/Customer/OrderDetail", new { id = OrderId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                OrderInfo = new OrderInfoViewModel
                {
                    Id = order.Id,
                    OrderNumber = $"ORD-{order.Id:D6}",
                    DealerName = order.Dealer?.Name ?? "N/A"
                };
                return Page();
            }
        }

        public class OrderInfoViewModel
        {
            public int Id { get; set; }
            public string OrderNumber { get; set; } = "";
            public string DealerName { get; set; } = "";
        }
    }
}

