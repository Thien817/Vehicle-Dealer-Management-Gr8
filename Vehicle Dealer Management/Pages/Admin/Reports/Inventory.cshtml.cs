using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.Admin.Reports
{
    public class InventoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public InventoryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalInventory { get; set; }
        public int EvmStock { get; set; }
        public int DealerStock { get; set; }
        public decimal EvmStockPercent { get; set; }
        public decimal DealerStockPercent { get; set; }
        public decimal TotalValue { get; set; }

        public List<StockAlertViewModel> LowStockItems { get; set; } = new();
        public List<StockAlertViewModel> HighStockItems { get; set; } = new();
        public List<InventoryByVehicleViewModel> InventoryByVehicle { get; set; } = new();

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
                .ToListAsync();

            var dealers = await _context.Dealers.ToDictionaryAsync(d => d.Id, d => d.Name);

            // Calculate totals
            TotalInventory = (int)stocks.Sum(s => s.Qty);
            EvmStock = (int)stocks.Where(s => s.OwnerType == "EVM").Sum(s => s.Qty);
            DealerStock = (int)stocks.Where(s => s.OwnerType == "DEALER").Sum(s => s.Qty);
            
            EvmStockPercent = TotalInventory > 0 ? ((decimal)EvmStock / TotalInventory * 100) : 0;
            DealerStockPercent = TotalInventory > 0 ? ((decimal)DealerStock / TotalInventory * 100) : 0;

            // Low stock alerts (< 10)
            LowStockItems = stocks
                .Where(s => s.Qty < 10)
                .OrderBy(s => s.Qty)
                .Select(s => new StockAlertViewModel
                {
                    VehicleName = $"{s.Vehicle?.ModelName} {s.Vehicle?.VariantName}",
                    Location = s.OwnerType == "EVM" ? "EVM" : (dealers.ContainsKey(s.OwnerId) ? dealers[s.OwnerId] : "N/A"),
                    Color = s.ColorCode,
                    Qty = (int)s.Qty
                })
                .Take(10)
                .ToList();

            // High stock items (> 50)
            HighStockItems = stocks
                .Where(s => s.Qty > 50)
                .OrderByDescending(s => s.Qty)
                .Select(s => new StockAlertViewModel
                {
                    VehicleName = $"{s.Vehicle?.ModelName} {s.Vehicle?.VariantName}",
                    Location = s.OwnerType == "EVM" ? "EVM" : (dealers.ContainsKey(s.OwnerId) ? dealers[s.OwnerId] : "N/A"),
                    Color = s.ColorCode,
                    Qty = (int)s.Qty
                })
                .Take(10)
                .ToList();

            // Get prices
            var prices = await _context.PricePolicies
                .Where(p => p.DealerId == null)
                .GroupBy(p => p.VehicleId)
                .Select(g => new { VehicleId = g.Key, Msrp = g.OrderByDescending(p => p.ValidFrom).First().Msrp })
                .ToDictionaryAsync(p => p.VehicleId, p => p.Msrp);

            // Group by vehicle
            var vehicleGroups = stocks.GroupBy(s => new 
            { 
                s.VehicleId, 
                VehicleName = (s.Vehicle!.ModelName + " " + s.Vehicle.VariantName) 
            });

            foreach (var group in vehicleGroups)
            {
                var evmStock = (int)group.Where(s => s.OwnerType == "EVM").Sum(s => s.Qty);
                var dealerStock = (int)group.Where(s => s.OwnerType == "DEALER").Sum(s => s.Qty);
                var total = evmStock + dealerStock;
                var price = prices.ContainsKey(group.Key.VehicleId) ? prices[group.Key.VehicleId] : 0;
                var value = price * total / 1000000000; // Billion

                InventoryByVehicle.Add(new InventoryByVehicleViewModel
                {
                    VehicleName = group.Key.VehicleName,
                    EvmStock = evmStock,
                    DealerStock = dealerStock,
                    Total = total,
                    Value = value
                });
            }

            InventoryByVehicle = InventoryByVehicle.OrderByDescending(i => i.Total).ToList();
            TotalValue = InventoryByVehicle.Sum(i => i.Value);

            return Page();
        }

        public class StockAlertViewModel
        {
            public string VehicleName { get; set; } = "";
            public string Location { get; set; } = "";
            public string Color { get; set; } = "";
            public int Qty { get; set; }
        }

        public class InventoryByVehicleViewModel
        {
            public string VehicleName { get; set; } = "";
            public int EvmStock { get; set; }
            public int DealerStock { get; set; }
            public int Total { get; set; }
            public decimal Value { get; set; }
        }
    }
}

