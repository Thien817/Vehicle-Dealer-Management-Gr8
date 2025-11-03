using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.Pages.Customer.Review
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeedbackService _feedbackService;
        private readonly ISalesDocumentService _salesDocumentService;

        public EditModel(
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

            var review = await _feedbackService.GetReviewByOrderIdAsync(OrderId);
            if (review == null || review.CustomerId != customer.Id)
            {
                TempData["Error"] = "Không tìm thấy đánh giá hoặc bạn không có quyền chỉnh sửa đánh giá này.";
                return RedirectToPage("/Customer/OrderDetail", new { id = OrderId });
            }
            Rating = review.Rating ?? 5;
            Content = review.Content;
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

            var review = await _feedbackService.GetReviewByOrderIdAsync(OrderId);
            if (review == null || review.CustomerId != customer.Id)
            {
                TempData["Error"] = "Không tìm thấy đánh giá hoặc bạn không có quyền chỉnh sửa đánh giá này.";
                return RedirectToPage("/Customer/OrderDetail", new { id = OrderId });
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
                await _feedbackService.UpdateReviewAsync(review.Id, Rating, Content);
                TempData["Success"] = "Đánh giá đã được cập nhật thành công!";
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

