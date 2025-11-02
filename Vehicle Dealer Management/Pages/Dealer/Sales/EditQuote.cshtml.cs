using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.Pages.Dealer.Sales
{
    public class EditQuoteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditQuoteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CustomerViewModel> Customers { get; set; } = new();
        public List<VehicleViewModel> Vehicles { get; set; } = new();
        public List<PromotionViewModel> Promotions { get; set; } = new();
        
        public int? QuoteId { get; set; }
        public QuoteEditViewModel? ExistingQuote { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            ViewData["UserRole"] = HttpContext.Session.GetString("UserRole") ?? "DEALER_STAFF";
            ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "User";

            var dealerIdInt = int.Parse(dealerId);

            // Load customers
            Customers = await _context.CustomerProfiles
                .Select(c => new CustomerViewModel
                {
                    Id = c.Id,
                    Name = c.FullName,
                    Phone = c.Phone
                })
                .ToListAsync();

            // Load available vehicles
            var vehicles = await _context.Vehicles
                .Where(v => v.Status == "AVAILABLE")
                .ToListAsync();

            foreach (var vehicle in vehicles)
            {
                var pricePolicy = await _context.PricePolicies
                    .Where(p => p.VehicleId == vehicle.Id &&
                                (p.DealerId == dealerIdInt || p.DealerId == null) &&
                                p.ValidFrom <= DateTime.UtcNow &&
                                (p.ValidTo == null || p.ValidTo >= DateTime.UtcNow))
                    .OrderByDescending(p => p.DealerId)
                    .FirstOrDefaultAsync();

                Vehicles.Add(new VehicleViewModel
                {
                    Id = vehicle.Id,
                    Name = vehicle.ModelName,
                    Variant = vehicle.VariantName,
                    Msrp = pricePolicy?.Msrp ?? 0
                });
            }

            // Load active promotions
            Promotions = await _context.Promotions
                .Where(p => p.ValidFrom <= DateTime.UtcNow &&
                            (p.ValidTo == null || p.ValidTo >= DateTime.UtcNow))
                .Select(p => new PromotionViewModel
                {
                    Id = p.Id,
                    Name = p.Name
                })
                .ToListAsync();

            // Load existing quote if id provided
            if (id.HasValue)
            {
                QuoteId = id.Value;
                var quote = await _context.SalesDocuments
                    .Include(s => s.Lines!)
                        .ThenInclude(l => l.Vehicle)
                    .FirstOrDefaultAsync(s => s.Id == id.Value && 
                                              s.DealerId == dealerIdInt && 
                                              s.Type == "QUOTE");

                if (quote == null)
                {
                    TempData["Error"] = "Không tìm thấy báo giá này.";
                    return RedirectToPage("/Dealer/Sales/Quotes");
                }

                // Validate status - only DRAFT or SENT can be edited
                if (quote.Status != "DRAFT" && quote.Status != "SENT")
                {
                    TempData["Error"] = "Chỉ có thể chỉnh sửa báo giá ở trạng thái DRAFT hoặc SENT.";
                    return RedirectToPage("/Dealer/Sales/QuoteDetail", new { id = id.Value });
                }

                ExistingQuote = new QuoteEditViewModel
                {
                    QuoteId = quote.Id,
                    CustomerId = quote.CustomerId,
                    PromotionId = quote.PromotionId,
                    Status = quote.Status,
                    Items = quote.Lines?.Select(l => new QuoteItemEditViewModel
                    {
                        LineId = l.Id,
                        VehicleId = l.VehicleId,
                        VehicleName = $"{l.Vehicle?.ModelName} {l.Vehicle?.VariantName}",
                        ColorCode = l.ColorCode,
                        Quantity = (int)l.Qty,
                        UnitPrice = l.UnitPrice,
                        DiscountValue = l.DiscountValue
                    }).ToList() ?? new List<QuoteItemEditViewModel>()
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? quoteId, int customerId, int vehicleId, string color, int quantity, string action, int? promotionId, decimal additionalDiscount, string? note)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            var userId = HttpContext.Session.GetString("UserId");
            
            if (string.IsNullOrEmpty(dealerId) || string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var dealerIdInt = int.Parse(dealerId);
            var userIdInt = int.Parse(userId);

            if (!quoteId.HasValue)
            {
                TempData["Error"] = "Thiếu ID báo giá.";
                return RedirectToPage("/Dealer/Sales/Quotes");
            }

            // Get existing quote
            var quote = await _context.SalesDocuments
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == quoteId.Value && 
                                          s.DealerId == dealerIdInt && 
                                          s.Type == "QUOTE");

            if (quote == null)
            {
                TempData["Error"] = "Không tìm thấy báo giá này.";
                return RedirectToPage("/Dealer/Sales/Quotes");
            }

            // Validate status
            if (quote.Status != "DRAFT" && quote.Status != "SENT")
            {
                TempData["Error"] = "Chỉ có thể chỉnh sửa báo giá ở trạng thái DRAFT hoặc SENT.";
                return RedirectToPage("/Dealer/Sales/QuoteDetail", new { id = quoteId.Value });
            }

            // Update quote
            quote.CustomerId = customerId;
            quote.PromotionId = promotionId;
            quote.Status = action == "send" ? "SENT" : "DRAFT";
            quote.UpdatedAt = DateTime.UtcNow;

            // Get price
            var pricePolicy = await _context.PricePolicies
                .Where(p => p.VehicleId == vehicleId &&
                            (p.DealerId == dealerIdInt || p.DealerId == null) &&
                            p.ValidFrom <= DateTime.UtcNow &&
                            (p.ValidTo == null || p.ValidTo >= DateTime.UtcNow))
                .OrderByDescending(p => p.DealerId)
                .FirstOrDefaultAsync();

            var unitPrice = pricePolicy?.Msrp ?? 0;

            // For simplicity, we'll replace all existing lines with the new one
            // In a real scenario, you might want to support adding/removing multiple lines
            if (quote.Lines != null && quote.Lines.Any())
            {
                _context.SalesDocumentLines.RemoveRange(quote.Lines);
            }

            // Add new line item
            var lineItem = new SalesDocumentLine
            {
                SalesDocumentId = quote.Id,
                VehicleId = vehicleId,
                ColorCode = color,
                Qty = quantity,
                UnitPrice = unitPrice,
                DiscountValue = additionalDiscount
            };

            _context.SalesDocumentLines.Add(lineItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật báo giá thành công!";
            return RedirectToPage("/Dealer/Sales/QuoteDetail", new { id = quote.Id });
        }

        public class CustomerViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Phone { get; set; } = "";
        }

        public class VehicleViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Variant { get; set; } = "";
            public decimal Msrp { get; set; }
        }

        public class PromotionViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class QuoteEditViewModel
        {
            public int QuoteId { get; set; }
            public int CustomerId { get; set; }
            public int? PromotionId { get; set; }
            public string Status { get; set; } = "";
            public List<QuoteItemEditViewModel> Items { get; set; } = new();
        }

        public class QuoteItemEditViewModel
        {
            public int LineId { get; set; }
            public int VehicleId { get; set; }
            public string VehicleName { get; set; } = "";
            public string ColorCode { get; set; } = "";
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal DiscountValue { get; set; }
        }
    }
}

