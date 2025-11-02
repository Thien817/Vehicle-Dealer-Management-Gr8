using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.EVM
{
    public class DealersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DealersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalDealers { get; set; }
        public int ActiveDealers { get; set; }
        public int InactiveDealers { get; set; }
        public decimal AvgDealerSales { get; set; }

        public List<DealerViewModel> Dealers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Get all dealers
            var dealers = await _context.Dealers.ToListAsync();

            TotalDealers = dealers.Count;
            ActiveDealers = dealers.Count(d => d.Status == "ACTIVE");
            InactiveDealers = dealers.Count(d => d.Status != "ACTIVE");

            // Get sales for each dealer (mock for now)
            foreach (var dealer in dealers)
            {
                var monthlySales = 350000000m; // Mock
                var totalOrders = 15; // Mock

                Dealers.Add(new DealerViewModel
                {
                    Id = dealer.Id,
                    Name = dealer.Name,
                    Address = dealer.Address,
                    Phone = dealer.PhoneNumber,
                    Email = dealer.Email,
                    Status = dealer.Status,
                    MonthlySales = monthlySales,
                    TotalOrders = totalOrders
                });
            }

            AvgDealerSales = Dealers.Any() ? Dealers.Average(d => d.MonthlySales) : 0;

            return Page();
        }

        public class DealerViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Address { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Email { get; set; } = "";
            public string Status { get; set; } = "";
            public decimal MonthlySales { get; set; }
            public int TotalOrders { get; set; }
        }
    }
}

