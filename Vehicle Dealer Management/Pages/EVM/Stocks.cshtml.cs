using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.Pages.EVM
{
    public class StocksModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public StocksModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalEvmStock { get; set; }
        public int TotalDealerStock { get; set; }
        public int LowStockCount { get; set; }
        public decimal TotalStockValue { get; set; }

        public List<VehicleSimple> Vehicles { get; set; } = new();
        public List<StockViewModel> Stocks { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Get all stocks
            var stocks = await _context.Stocks
                .Include(s => s.Vehicle)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            // Get dealers separately
            var dealers = await _context.Dealers.ToDictionaryAsync(d => d.Id, d => d.Name);

            // Get vehicles for add form
            Vehicles = await _context.Vehicles
                .Select(v => new VehicleSimple
                {
                    Id = v.Id,
                    Name = v.ModelName + " " + v.VariantName
                })
                .ToListAsync();

            // Calculate totals
            TotalEvmStock = (int)stocks.Where(s => s.OwnerType == "EVM").Sum(s => s.Qty);
            TotalDealerStock = (int)stocks.Where(s => s.OwnerType == "DEALER").Sum(s => s.Qty);
            LowStockCount = stocks.Count(s => s.Qty < 10);

            // Get price policies for value calculation
            var prices = await _context.PricePolicies
                .Where(p => p.DealerId == null) // Global prices
                .GroupBy(p => p.VehicleId)
                .Select(g => new { VehicleId = g.Key, Msrp = g.OrderByDescending(p => p.ValidFrom).First().Msrp })
                .ToListAsync();

            var priceDict = prices.ToDictionary(p => p.VehicleId, p => p.Msrp);

            // Map to view models
            Stocks = stocks.Select(s => new StockViewModel
            {
                Id = s.Id,
                VehicleName = $"{s.Vehicle?.ModelName} {s.Vehicle?.VariantName}",
                Color = s.ColorCode,
                OwnerType = s.OwnerType,
                OwnerName = s.OwnerType == "EVM" ? "EVM Central" : (dealers.ContainsKey(s.OwnerId) ? dealers[s.OwnerId] : "N/A"),
                Qty = (int)s.Qty,
                EstimatedValue = priceDict.ContainsKey(s.VehicleId) ? (priceDict[s.VehicleId] * s.Qty) : 0,
                UpdatedDate = s.CreatedDate
            }).ToList();

            TotalStockValue = Stocks.Sum(s => s.EstimatedValue) / 1000000000; // Billion

            return Page();
        }

        public async Task<IActionResult> OnPostAddStockAsync(int vehicleId, string color, int quantity)
        {
            // Check if stock already exists
            var existingStock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.OwnerType == "EVM" && 
                                         s.OwnerId == 0 && 
                                         s.VehicleId == vehicleId && 
                                         s.ColorCode == color);

            if (existingStock != null)
            {
                // Update quantity
                existingStock.Qty += quantity;
                existingStock.CreatedDate = DateTime.UtcNow;
            }
            else
            {
                // Create new stock
                var stock = new Stock
                {
                    OwnerType = "EVM",
                    OwnerId = 0,
                    VehicleId = vehicleId,
                    ColorCode = color,
                    Qty = quantity,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Stocks.Add(stock);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Nhập kho thành công!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateStockAsync(int stockId, int newQuantity)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Validate quantity
            if (newQuantity < 0)
            {
                TempData["Error"] = "Số lượng không được âm.";
                return RedirectToPage();
            }

            // Get stock
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.Id == stockId && s.OwnerType == "EVM");

            if (stock == null)
            {
                TempData["Error"] = "Không tìm thấy tồn kho này.";
                return RedirectToPage();
            }

            // Update quantity
            stock.Qty = newQuantity;
            stock.CreatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật số lượng tồn kho thành công!";
            return RedirectToPage();
        }

        public class VehicleSimple
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class StockViewModel
        {
            public int Id { get; set; }
            public string VehicleName { get; set; } = "";
            public string Color { get; set; } = "";
            public string OwnerType { get; set; } = "";
            public string OwnerName { get; set; } = "";
            public int Qty { get; set; }
            public decimal EstimatedValue { get; set; }
            public DateTime UpdatedDate { get; set; }
        }
    }
}

