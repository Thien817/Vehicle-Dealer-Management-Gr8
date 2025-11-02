using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.Dealer
{
    public class FeedbackModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public FeedbackModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string TypeFilter { get; set; } = "all";
        public int TotalFeedback { get; set; }
        public int NewCount { get; set; }
        public int InProgressCount { get; set; }
        public int ResolvedCount { get; set; }

        public List<FeedbackViewModel> Feedbacks { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string? type)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            TypeFilter = type ?? "all";
            var dealerIdInt = int.Parse(dealerId);

            // Get feedbacks
            var query = _context.Feedbacks
                .Where(f => f.DealerId == dealerIdInt)
                .Include(f => f.Customer)
                .AsQueryable();

            if (TypeFilter != "all")
            {
                query = query.Where(f => f.Type == TypeFilter);
            }

            var feedbacks = await query
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            TotalFeedback = feedbacks.Count;
            NewCount = feedbacks.Count(f => f.Status == "NEW");
            InProgressCount = feedbacks.Count(f => f.Status == "IN_PROGRESS");
            ResolvedCount = feedbacks.Count(f => f.Status == "RESOLVED");

            Feedbacks = feedbacks.Select(f => new FeedbackViewModel
            {
                Id = f.Id,
                CustomerName = f.Customer?.FullName ?? "N/A",
                Type = f.Type,
                Status = f.Status,
                Content = f.Content,
                CreatedAt = f.CreatedAt
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostStartProcessAsync(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback != null)
            {
                feedback.Status = "IN_PROGRESS";
                feedback.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostResolveAsync(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback != null)
            {
                feedback.Status = "RESOLVED";
                feedback.ResolvedAt = DateTime.UtcNow;
                feedback.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public class FeedbackViewModel
        {
            public int Id { get; set; }
            public string CustomerName { get; set; } = "";
            public string Type { get; set; } = "";
            public string Status { get; set; } = "";
            public string Content { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }
    }
}

