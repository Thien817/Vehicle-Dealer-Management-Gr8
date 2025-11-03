using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.Pages.Dealer
{
    public class ReviewsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeedbackService _feedbackService;

        public ReviewsModel(
            ApplicationDbContext context,
            IFeedbackService feedbackService)
        {
            _context = context;
            _feedbackService = feedbackService;
        }

        public List<ReviewViewModel> Reviews { get; set; } = new();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            ViewData["UserRole"] = HttpContext.Session.GetString("UserRole") ?? "DEALER_STAFF";
            ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "Dealer Staff";

            var dealerIdInt = int.Parse(dealerId);

            var reviews = await _feedbackService.GetReviewsByDealerIdAsync(dealerIdInt);
            AverageRating = await _feedbackService.GetAverageRatingByDealerIdAsync(dealerIdInt);
            TotalReviews = reviews.Count();

            Reviews = reviews.Select(r => new ReviewViewModel
            {
                Id = r.Id,
                OrderId = r.OrderId ?? 0,
                OrderNumber = r.OrderId.HasValue ? $"ORD-{r.OrderId.Value:D6}" : "N/A",
                CustomerName = r.Customer?.FullName ?? "N/A",
                Rating = r.Rating ?? 0,
                Content = r.Content,
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
            public string CustomerName { get; set; } = "";
            public int Rating { get; set; }
            public string? Content { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }
    }
}

