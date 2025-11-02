using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using System.Text.Json;

namespace Vehicle_Dealer_Management.Pages.EVM
{
    public class DealerOrderDetailModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DealerOrderDetailModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public DealerOrderViewModel Order { get; set; } = new();
        public bool HasEnoughStock { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var order = await _context.DealerOrders
                .Include(o => o.Dealer)
                .Include(o => o.CreatedByUser)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return RedirectToPage("/EVM/DealerOrders");
            }

            // Parse items from JSON (mock for now)
            var items = new List<OrderItemViewModel>
            {
                new() { VehicleName = "Model 3 Standard", Color = "BLACK", Quantity = 2, WholesalePrice = 1400000000, AvailableStock = 15 },
                new() { VehicleName = "Model S Premium", Color = "WHITE", Quantity = 1, WholesalePrice = 2300000000, AvailableStock = 8 }
            };

            foreach (var item in items)
            {
                item.Total = item.Quantity * item.WholesalePrice;
            }

            HasEnoughStock = items.All(i => i.AvailableStock >= i.Quantity);

            Order = new DealerOrderViewModel
            {
                Id = order.Id,
                DealerName = order.Dealer?.Name ?? "N/A",
                DealerCode = order.Dealer?.Code ?? "N/A",
                CreatedAt = order.CreatedAt,
                CreatedByName = order.CreatedByUser?.FullName ?? "N/A",
                Status = order.Status,
                ApprovedAt = order.ApprovedAt,
                Items = items
            };

            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(int orderId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var order = await _context.DealerOrders.FindAsync(orderId);
            if (order == null)
            {
                return RedirectToPage("/EVM/DealerOrders");
            }

            // Update order status
            order.Status = "APPROVED";
            order.ApprovedBy = int.Parse(userId);
            order.ApprovedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // TODO: Transfer stock from EVM to Dealer

            return RedirectToPage("/EVM/DealerOrders");
        }

        public async Task<IActionResult> OnPostRejectAsync(int orderId, string reason)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var order = await _context.DealerOrders.FindAsync(orderId);
            if (order == null)
            {
                return RedirectToPage("/EVM/DealerOrders");
            }

            // Update order status
            order.Status = "REJECTED";
            order.UpdatedAt = DateTime.UtcNow;
            // Store reason in ItemsJson or separate field

            await _context.SaveChangesAsync();

            return RedirectToPage("/EVM/DealerOrders");
        }

        public class DealerOrderViewModel
        {
            public int Id { get; set; }
            public string DealerName { get; set; } = "";
            public string DealerCode { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public string CreatedByName { get; set; } = "";
            public string Status { get; set; } = "";
            public DateTime? ApprovedAt { get; set; }
            public List<OrderItemViewModel> Items { get; set; } = new();
        }

        public class OrderItemViewModel
        {
            public string VehicleName { get; set; } = "";
            public string Color { get; set; } = "";
            public int Quantity { get; set; }
            public decimal WholesalePrice { get; set; }
            public decimal Total { get; set; }
            public int AvailableStock { get; set; }
        }
    }
}

