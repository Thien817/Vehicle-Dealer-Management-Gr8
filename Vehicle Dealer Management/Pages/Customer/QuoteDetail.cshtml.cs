using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.Customer
{
    public class QuoteDetailModel : PageModel
    {
        private readonly ISalesDocumentService _salesDocumentService;
        private readonly ApplicationDbContext _context;

        public QuoteDetailModel(
            ISalesDocumentService salesDocumentService,
            ApplicationDbContext context)
        {
            _salesDocumentService = salesDocumentService;
            _context = context;
        }

        public QuoteDetailViewModel Quote { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            ViewData["UserRole"] = "CUSTOMER";
            ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "Customer";

            var userIdInt = int.Parse(userId);

            // Get customer profile from user
            var customer = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == userIdInt);

            if (customer == null)
            {
                return RedirectToPage("/Auth/Profile");
            }

            // Get quote with all related data - only quotes belonging to this customer
            var quote = await _salesDocumentService.GetSalesDocumentWithDetailsAsync(id);

            if (quote == null || quote.CustomerId != customer.Id || quote.Type != "QUOTE")
            {
                return NotFound();
            }

            // Calculate totals from real data
            var totalAmount = quote.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0;

            Quote = new QuoteDetailViewModel
            {
                Id = quote.Id,
                QuoteNumber = $"QUO-{quote.Id:D6}",
                Status = quote.Status,
                CreatedAt = quote.CreatedAt,
                UpdatedAt = quote.UpdatedAt,

                // Dealer Info
                DealerName = quote.Dealer?.Name ?? "N/A",
                DealerAddress = quote.Dealer?.Address ?? "N/A",
                DealerPhone = quote.Dealer?.PhoneNumber ?? "N/A",

                // Promotion
                PromotionId = quote.PromotionId,
                PromotionName = quote.Promotion?.Name,

                // Items (Lines)
                Items = quote.Lines?.Select(l => new QuoteItemViewModel
                {
                    Id = l.Id,
                    VehicleId = l.VehicleId,
                    VehicleModel = l.Vehicle?.ModelName ?? "N/A",
                    VehicleVariant = l.Vehicle?.VariantName ?? "N/A",
                    ColorCode = l.ColorCode,
                    Qty = l.Qty,
                    UnitPrice = l.UnitPrice,
                    DiscountValue = l.DiscountValue,
                    LineTotal = l.UnitPrice * l.Qty - l.DiscountValue
                }).ToList() ?? new List<QuoteItemViewModel>(),

                TotalAmount = totalAmount
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAcceptAsync(int id)
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

            var quote = await _salesDocumentService.GetSalesDocumentWithDetailsAsync(id);

            if (quote == null || quote.CustomerId != customer.Id || quote.Type != "QUOTE")
            {
                return NotFound();
            }

            // Update status to ACCEPTED
            quote.Status = "ACCEPTED";
            quote.UpdatedAt = DateTime.UtcNow;
            await _salesDocumentService.UpdateSalesDocumentStatusAsync(id, "ACCEPTED");

            return RedirectToPage("/Customer/MyQuotes");
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
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

            var quote = await _salesDocumentService.GetSalesDocumentWithDetailsAsync(id);

            if (quote == null || quote.CustomerId != customer.Id || quote.Type != "QUOTE")
            {
                return NotFound();
            }

            // Update status to REJECTED
            quote.Status = "REJECTED";
            quote.UpdatedAt = DateTime.UtcNow;
            await _salesDocumentService.UpdateSalesDocumentStatusAsync(id, "REJECTED");

            return RedirectToPage("/Customer/MyQuotes");
        }

        public class QuoteDetailViewModel
        {
            public int Id { get; set; }
            public string QuoteNumber { get; set; } = "";
            public string Status { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }

            // Dealer Info
            public string DealerName { get; set; } = "";
            public string DealerAddress { get; set; } = "";
            public string DealerPhone { get; set; } = "";

            // Promotion
            public int? PromotionId { get; set; }
            public string? PromotionName { get; set; }

            // Items
            public List<QuoteItemViewModel> Items { get; set; } = new();
            public decimal TotalAmount { get; set; }
        }

        public class QuoteItemViewModel
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
        }
    }
}

