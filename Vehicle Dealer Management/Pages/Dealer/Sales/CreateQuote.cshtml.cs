using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.Pages.Dealer.Sales
{
    public class CreateQuoteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateQuoteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CustomerViewModel> Customers { get; set; } = new();
        public List<VehicleViewModel> Vehicles { get; set; } = new();
        public List<PromotionViewModel> Promotions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Load customers (all for now, can be filtered later)
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

            var dealerIdInt = int.Parse(dealerId);

            foreach (var vehicle in vehicles)
            {
                // Get price
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

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int customerId, int vehicleId, string color, int quantity, string action, int? promotionId, decimal additionalDiscount, string? note)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            var userId = HttpContext.Session.GetString("UserId");
            
            if (string.IsNullOrEmpty(dealerId) || string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var dealerIdInt = int.Parse(dealerId);
            var userIdInt = int.Parse(userId);

            // Create sales document (QUOTE)
            var salesDocument = new SalesDocument
            {
                Type = "QUOTE",
                DealerId = dealerIdInt,
                CustomerId = customerId,
                Status = action == "send" ? "SENT" : "DRAFT",
                PromotionId = promotionId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userIdInt
            };

            _context.SalesDocuments.Add(salesDocument);
            await _context.SaveChangesAsync();

            // Get price
            var pricePolicy = await _context.PricePolicies
                .Where(p => p.VehicleId == vehicleId &&
                            (p.DealerId == dealerIdInt || p.DealerId == null) &&
                            p.ValidFrom <= DateTime.UtcNow &&
                            (p.ValidTo == null || p.ValidTo >= DateTime.UtcNow))
                .OrderByDescending(p => p.DealerId)
                .FirstOrDefaultAsync();

            var unitPrice = pricePolicy?.Msrp ?? 0;

            // Create line item
            var lineItem = new SalesDocumentLine
            {
                SalesDocumentId = salesDocument.Id,
                VehicleId = vehicleId,
                ColorCode = color,
                Qty = quantity,
                UnitPrice = unitPrice,
                DiscountValue = additionalDiscount
            };

            _context.SalesDocumentLines.Add(lineItem);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Dealer/Sales/Quotes");
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
    }
}

