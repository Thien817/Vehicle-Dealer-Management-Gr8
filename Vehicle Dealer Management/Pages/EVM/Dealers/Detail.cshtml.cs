using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.EVM.Dealers
{
    public class DetailModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public DealerDetailViewModel Dealer { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            if (!id.HasValue)
            {
                return RedirectToPage("/EVM/Dealers");
            }

            var dealer = await _context.Dealers.FindAsync(id.Value);
            if (dealer == null)
            {
                TempData["Error"] = "Không tìm thấy đại lý này.";
                return RedirectToPage("/EVM/Dealers");
            }

            // Get dealer orders
            var dealerOrders = await _context.DealerOrders
                .Where(o => o.DealerId == id.Value)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // Get sales documents (orders) from this dealer
            var orders = await _context.SalesDocuments
                .Where(s => s.DealerId == id.Value && s.Type == "ORDER")
                .Include(s => s.Customer)
                .Include(s => s.Lines)
                    .ThenInclude(l => l.Vehicle)
                .Include(s => s.Payments)
                .OrderByDescending(s => s.CreatedAt)
                .Take(10)
                .ToListAsync();

            // Calculate total sales (from ALL orders, not just recent 10)
            var allOrders = await _context.SalesDocuments
                .Where(s => s.DealerId == id.Value && s.Type == "ORDER")
                .Include(s => s.Lines!)
                .Include(s => s.Payments)
                .ToListAsync();

            var totalSales = allOrders
                .Sum(o => o.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0);

            // Calculate total paid
            var totalPaid = allOrders
                .Sum(o => o.Payments?.Sum(p => p.Amount) ?? 0);

            // Calculate debt (outstanding amount)
            var totalDebt = totalSales - totalPaid;

            // Get staff count
            var staffCount = await _context.Users
                .CountAsync(u => u.DealerId == id.Value);

            // Get stock summary
            var stockSummary = await _context.Stocks
                .Where(s => s.OwnerType == "DEALER" && s.OwnerId == id.Value)
                .Include(s => s.Vehicle)
                .GroupBy(s => s.VehicleId)
                .Select(g => new StockSummaryViewModel
                {
                    VehicleName = g.First().Vehicle!.ModelName + " " + g.First().Vehicle.VariantName,
                    TotalQty = (int)g.Sum(s => s.Qty)
                })
                .ToListAsync();

            Dealer = new DealerDetailViewModel
            {
                Id = dealer.Id,
                Name = dealer.Name,
                Address = dealer.Address,
                Phone = dealer.PhoneNumber ?? "",
                Email = dealer.Email ?? "",
                Status = dealer.Status,
                CreatedDate = dealer.CreatedDate,

                // Stats
                StaffCount = staffCount,
                TotalOrders = allOrders.Count,
                TotalSales = totalSales,
                TotalPaid = totalPaid,
                TotalDebt = totalDebt,
                DealerOrdersCount = dealerOrders.Count,

                // Recent orders
                RecentOrders = orders.Select(o => new OrderSummaryViewModel
                {
                    Id = o.Id,
                    OrderNumber = $"ORD-{o.Id:D6}",
                    CustomerName = o.Customer?.FullName ?? "N/A",
                    TotalAmount = o.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0,
                    PaidAmount = o.Payments?.Sum(p => p.Amount) ?? 0,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt
                }).ToList(),

                // Stock summary
                StockSummary = stockSummary
            };

            return Page();
        }

        public class DealerDetailViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Address { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Email { get; set; } = "";
            public string Status { get; set; } = "";
            public DateTime CreatedDate { get; set; }

            public int StaffCount { get; set; }
            public int TotalOrders { get; set; }
            public decimal TotalSales { get; set; }
            public decimal TotalPaid { get; set; }
            public decimal TotalDebt { get; set; }
            public int DealerOrdersCount { get; set; }

            public List<OrderSummaryViewModel> RecentOrders { get; set; } = new();
            public List<StockSummaryViewModel> StockSummary { get; set; } = new();
        }

        public class OrderSummaryViewModel
        {
            public int Id { get; set; }
            public string OrderNumber { get; set; } = "";
            public string CustomerName { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public decimal PaidAmount { get; set; }
            public string Status { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }

        public class StockSummaryViewModel
        {
            public string VehicleName { get; set; } = "";
            public int TotalQty { get; set; }
        }
    }
}

