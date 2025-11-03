using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.Dealer.Sales
{
    public class QuoteDetailModel : PageModel
    {
        private readonly ISalesDocumentService _salesDocumentService;
        private readonly ApplicationDbContext _context; // Cần cho SalesDocumentLine khi convert

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
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Set UserRole from Session for proper navigation
            ViewData["UserRole"] = HttpContext.Session.GetString("UserRole") ?? "DEALER_STAFF";
            ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "User";

            var dealerIdInt = int.Parse(dealerId);

            // Get quote with all related data from Service
            var quote = await _salesDocumentService.GetSalesDocumentWithDetailsAsync(id);

            if (quote == null || quote.DealerId != dealerIdInt || quote.Type != "QUOTE")
            {
                return NotFound();
            }

            // Check if quote has already been converted (for UI display)
            var isConverted = await CheckIfQuoteConvertedAsync(quote);

            // Calculate totals from real data
            var totalAmount = quote.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0;

            Quote = new QuoteDetailViewModel
            {
                Id = quote.Id,
                QuoteNumber = $"QUO-{quote.Id:D6}",
                Status = quote.Status,
                CreatedAt = quote.CreatedAt,
                UpdatedAt = quote.UpdatedAt,
                IsConverted = isConverted,

                // Customer Info
                CustomerId = quote.CustomerId,
                CustomerName = quote.Customer?.FullName ?? "N/A",
                CustomerPhone = quote.Customer?.Phone ?? "N/A",
                CustomerEmail = quote.Customer?.Email ?? "N/A",
                CustomerAddress = quote.Customer?.Address ?? "N/A",

                // Dealer Info
                DealerName = quote.Dealer?.Name ?? "N/A",
                DealerAddress = quote.Dealer?.Address ?? "N/A",

                // Created By
                CreatedBy = quote.CreatedByUser?.FullName ?? "N/A",

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
                    LineTotal = l.UnitPrice * l.Qty - l.DiscountValue,
                    VehicleImageUrl = l.Vehicle?.ImageUrl
                }).ToList() ?? new List<QuoteItemViewModel>(),

                // Totals
                SubTotal = totalAmount,
                TotalAmount = totalAmount
            };

            return Page();
        }

        public async Task<IActionResult> OnPostConvertToOrderAsync(int id)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(dealerId) || string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var dealerIdInt = int.Parse(dealerId);
            var userIdInt = int.Parse(userId);

            // Get quote with all lines
            var quote = await _context.SalesDocuments
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == id && s.DealerId == dealerIdInt && s.Type == "QUOTE");

            if (quote == null)
            {
                return NotFound();
            }

            // Validate quote can be converted - chỉ cho phép khi customer đã chấp nhận
            if (quote.Status != "ACCEPTED")
            {
                TempData["Error"] = "Báo giá này không thể chuyển thành đơn hàng. Chỉ có thể chuyển đổi báo giá khi khách hàng đã chấp nhận (trạng thái ACCEPTED).";
                return RedirectToPage(new { id });
            }

            // Check if quote has already been converted to order
            // Look for existing orders with same customer, dealer, and matching lines
            var existingOrders = await _context.SalesDocuments
                .Include(o => o.Lines)
                .Where(o => o.Type == "ORDER" 
                    && o.DealerId == quote.DealerId 
                    && o.CustomerId == quote.CustomerId
                    && o.CreatedAt >= quote.CreatedAt.AddHours(-24)) // Orders created within 24 hours of quote creation/update
                .ToListAsync();

            foreach (var existingOrder in existingOrders)
            {
                // Check if order lines match quote lines
                if (existingOrder.Lines != null && quote.Lines != null)
                {
                    var orderLinesList = existingOrder.Lines.ToList();
                    var quoteLinesList = quote.Lines.ToList();

                    if (orderLinesList.Count == quoteLinesList.Count)
                    {
                        bool allLinesMatch = true;
                        foreach (var quoteLine in quoteLinesList)
                        {
                            var matchingOrderLine = orderLinesList.FirstOrDefault(ol =>
                                ol.VehicleId == quoteLine.VehicleId
                                && ol.ColorCode == quoteLine.ColorCode
                                && ol.Qty == quoteLine.Qty
                                && ol.UnitPrice == quoteLine.UnitPrice
                                && ol.DiscountValue == quoteLine.DiscountValue);

                            if (matchingOrderLine == null)
                            {
                                allLinesMatch = false;
                                break;
                            }
                        }

                        if (allLinesMatch)
                        {
                            TempData["Error"] = $"Báo giá này đã được chuyển thành đơn hàng #{existingOrder.Id} rồi. Không thể chuyển đổi lại.";
                            return RedirectToPage(new { id });
                        }
                    }
                }
            }

            // Also check if quote status is already CONVERTED
            if (quote.Status == "CONVERTED")
            {
                TempData["Error"] = "Báo giá này đã được chuyển thành đơn hàng rồi. Không thể chuyển đổi lại.";
                return RedirectToPage(new { id });
            }

            // Create new Order from Quote
            var order = new Vehicle_Dealer_Management.DAL.Models.SalesDocument
            {
                Type = "ORDER",
                DealerId = quote.DealerId,
                CustomerId = quote.CustomerId,
                Status = "OPEN",
                PromotionId = quote.PromotionId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userIdInt
            };

            _context.SalesDocuments.Add(order);
            await _context.SaveChangesAsync(); // Save to get Order ID

            // Copy all lines from Quote to Order
            if (quote.Lines != null && quote.Lines.Any())
            {
                foreach (var quoteLine in quote.Lines)
                {
                    var orderLine = new Vehicle_Dealer_Management.DAL.Models.SalesDocumentLine
                    {
                        SalesDocumentId = order.Id,
                        VehicleId = quoteLine.VehicleId,
                        ColorCode = quoteLine.ColorCode,
                        Qty = quoteLine.Qty,
                        UnitPrice = quoteLine.UnitPrice,
                        DiscountValue = quoteLine.DiscountValue
                    };

                    _context.SalesDocumentLines.Add(orderLine);
                }
            }

            // Update quote status to indicate it's been converted to prevent duplicate conversions
            quote.Status = "CONVERTED";
            quote.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Chuyển đổi báo giá thành đơn hàng thành công!";
            return RedirectToPage("/Dealer/Sales/OrderDetail", new { id = order.Id });
        }

        private async Task<bool> CheckIfQuoteConvertedAsync(DAL.Models.SalesDocument quote)
        {
            // Check if status is already CONVERTED
            if (quote.Status == "CONVERTED")
            {
                return true;
            }

            // Check if there's an existing order created from this quote
            // Look for orders with same customer, dealer, and matching lines
            var existingOrders = await _context.SalesDocuments
                .Include(o => o.Lines)
                .Where(o => o.Type == "ORDER" 
                    && o.DealerId == quote.DealerId 
                    && o.CustomerId == quote.CustomerId
                    && o.CreatedAt >= quote.CreatedAt.AddHours(-24)) // Orders created within 24 hours of quote creation/update
                .ToListAsync();

            if (quote.Lines == null || !quote.Lines.Any())
            {
                return false;
            }

            var quoteLinesList = quote.Lines.ToList();

            foreach (var existingOrder in existingOrders)
            {
                if (existingOrder.Lines != null)
                {
                    var orderLinesList = existingOrder.Lines.ToList();

                    if (orderLinesList.Count == quoteLinesList.Count)
                    {
                        bool allLinesMatch = true;
                        foreach (var quoteLine in quoteLinesList)
                        {
                            var matchingOrderLine = orderLinesList.FirstOrDefault(ol =>
                                ol.VehicleId == quoteLine.VehicleId
                                && ol.ColorCode == quoteLine.ColorCode
                                && ol.Qty == quoteLine.Qty
                                && ol.UnitPrice == quoteLine.UnitPrice
                                && ol.DiscountValue == quoteLine.DiscountValue);

                            if (matchingOrderLine == null)
                            {
                                allLinesMatch = false;
                                break;
                            }
                        }

                        if (allLinesMatch)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public class QuoteDetailViewModel
        {
            public int Id { get; set; }
            public string QuoteNumber { get; set; } = "";
            public string Status { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public bool IsConverted { get; set; }

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
            public List<QuoteItemViewModel> Items { get; set; } = new();

            // Totals
            public decimal SubTotal { get; set; }
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
            public string? VehicleImageUrl { get; set; }
        }
    }
}

