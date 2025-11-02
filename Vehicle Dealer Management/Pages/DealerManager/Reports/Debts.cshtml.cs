using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.DealerManager.Reports
{
    public class DebtsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DebtsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public decimal TotalCustomerDebt { get; set; }
        public decimal TotalEvmDebt { get; set; }
        public decimal MonthlyCollected { get; set; }
        public decimal CollectionRate { get; set; }
        public int OverdueCustomers { get; set; }
        public int OverdueEvmOrders { get; set; }

        public List<CustomerDebtViewModel> CustomerDebts { get; set; } = new();
        public List<EvmDebtViewModel> EvmDebts { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var dealerId = HttpContext.Session.GetString("DealerId");
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var dealerIdInt = int.Parse(dealerId);

            // Get all orders from this dealer
            var orders = await _context.SalesDocuments
                .Where(s => s.DealerId == dealerIdInt && s.Type == "ORDER")
                .Include(s => s.Customer)
                .Include(s => s.Lines)
                .Include(s => s.Payments)
                .ToListAsync();

            // Calculate customer debts
            foreach (var order in orders)
            {
                var totalAmount = order.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0;
                var paidAmount = order.Payments?.Sum(p => p.Amount) ?? 0;
                var debtAmount = totalAmount - paidAmount;

                if (debtAmount > 0)
                {
                    // Mock due date (30 days from order creation)
                    var dueDate = order.CreatedAt.AddDays(30);

                    CustomerDebts.Add(new CustomerDebtViewModel
                    {
                        OrderId = order.Id,
                        CustomerName = order.Customer?.FullName ?? "N/A",
                        CustomerEmail = order.Customer?.Email ?? "N/A",
                        CustomerPhone = order.Customer?.Phone ?? "N/A",
                        TotalAmount = totalAmount,
                        PaidAmount = paidAmount,
                        DebtAmount = debtAmount,
                        DueDate = dueDate
                    });
                }
            }

            TotalCustomerDebt = CustomerDebts.Sum(d => d.DebtAmount);
            OverdueCustomers = CustomerDebts.Count(d => d.DueDate < DateTime.Today);

            // Mock EVM debts (dealer orders from EVM)
            var dealerOrders = await _context.DealerOrders
                .Where(o => o.DealerId == dealerIdInt && 
                           (o.Status == "FULFILLING" || o.Status == "APPROVED"))
                .ToListAsync();

            foreach (var order in dealerOrders)
            {
                // Mock data
                var totalAmount = 500000000m; // 500M per order
                var paidAmount = 0m; // Not paid yet
                var dueDate = order.CreatedAt.AddDays(45);

                EvmDebts.Add(new EvmDebtViewModel
                {
                    OrderId = order.Id,
                    OrderDate = order.CreatedAt,
                    VehicleCount = 3, // Mock
                    TotalAmount = totalAmount,
                    PaidAmount = paidAmount,
                    DebtAmount = totalAmount - paidAmount,
                    DueDate = dueDate
                });
            }

            TotalEvmDebt = EvmDebts.Sum(d => d.DebtAmount);
            OverdueEvmOrders = EvmDebts.Count(d => d.DueDate < DateTime.Today);

            // Mock monthly collected
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            MonthlyCollected = await _context.Payments
                .Where(p => p.PaidAt >= monthStart && 
                           p.SalesDocument!.DealerId == dealerIdInt)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            // Collection rate = collected / (collected + current debt)
            var totalDue = TotalCustomerDebt + MonthlyCollected;
            CollectionRate = totalDue > 0 ? (MonthlyCollected / totalDue * 100) : 0;

            return Page();
        }

        public class CustomerDebtViewModel
        {
            public int OrderId { get; set; }
            public string CustomerName { get; set; } = "";
            public string CustomerEmail { get; set; } = "";
            public string CustomerPhone { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal DebtAmount { get; set; }
            public DateTime DueDate { get; set; }
        }

        public class EvmDebtViewModel
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }
            public int VehicleCount { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal DebtAmount { get; set; }
            public DateTime DueDate { get; set; }
        }
    }
}

