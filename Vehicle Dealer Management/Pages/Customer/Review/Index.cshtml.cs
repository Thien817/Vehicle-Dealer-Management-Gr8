using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.Pages.Customer.Review
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeedbackService _feedbackService;

        public IndexModel(
            ApplicationDbContext context,
            IFeedbackService feedbackService)
        {
            _context = context;
            _feedbackService = feedbackService;
        }

        public List<ReviewViewModel> Reviews { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
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

            var reviews = await _feedbackService.GetFeedbacksByTypeAsync("REVIEW");
            Reviews = reviews
                .Where(r => r.CustomerId == customer.Id && r.OrderId.HasValue)
                .Select(r => new ReviewViewModel
                {
                    Id = r.Id,
                    OrderId = r.OrderId!.Value,
                    OrderNumber = $"ORD-{r.OrderId.Value:D6}",
                    Rating = r.Rating ?? 0,
                    Content = r.Content,
                    DealerName = r.Dealer?.Name ?? "N/A",
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                }).ToList();

            return Page();
        }

        public class ReviewViewModel
        {
            public int Id { get; set; }
            public int OrderId { get; set; }
            public string OrderNumber { get; set; } = "";
            public int Rating { get; set; }
            public string? Content { get; set; }
            public string DealerName { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }
    }
}

