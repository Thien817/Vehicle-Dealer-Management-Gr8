using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.Pages.EVM
{
    public class StocksModel : PageModel
    {
        private readonly IStockService _stockService;
        private readonly IVehicleService _vehicleService;
        private readonly IPricePolicyService _pricePolicyService;
        private readonly ApplicationDbContext _context;

        public StocksModel(
            IStockService stockService,
            IVehicleService vehicleService,
            IPricePolicyService pricePolicyService,
            ApplicationDbContext context)
        {
            _stockService = stockService;
            _vehicleService = vehicleService;
            _pricePolicyService = pricePolicyService;
            _context = context;
        }

        public int TotalEvmStock { get; set; }
        public int TotalDealerStock { get; set; }
        public int LowStockCount { get; set; }
        public decimal TotalStockValue { get; set; }

        public List<VehicleSimple> Vehicles { get; set; } = new();
        public List<DealerSimple> Dealers { get; set; } = new(); // THÊM MỚI
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

            // Get dealers
            var dealers = await _context.Dealers.ToDictionaryAsync(d => d.Id, d => d.Name);

            // THÊM: Load danh sách dealers cho dropdown phân phối
            Dealers = await _context.Dealers
                .Where(d => d.Status == "ACTIVE")
                .OrderBy(d => d.Name)
                .Select(d => new DealerSimple
                {
                    Id = d.Id,
                    Name = d.Name
                })
                .ToListAsync();

            // Get vehicles for add form
            var allVehicles = await _vehicleService.GetAllVehiclesAsync();
            Vehicles = allVehicles.Select(v => new VehicleSimple
            {
                Id = v.Id,
                Name = v.ModelName + " " + v.VariantName
            }).ToList();

            // Calculate totals
            TotalEvmStock = (int)stocks.Where(s => s.OwnerType == "EVM").Sum(s => s.Qty);
            TotalDealerStock = (int)stocks.Where(s => s.OwnerType == "DEALER").Sum(s => s.Qty);
            LowStockCount = stocks.Count(s => s.Qty < 10);

            // Get price policies
            var prices = await _context.PricePolicies
                .Where(p => p.DealerId == null)
                .GroupBy(p => p.VehicleId)
                .Select(g => new { VehicleId = g.Key, Msrp = g.OrderByDescending(p => p.ValidFrom).First().Msrp })
                .ToListAsync();

            var priceDict = prices.ToDictionary(p => p.VehicleId, p => p.Msrp);

            // Map to view models
            Stocks = stocks.Select(s => new StockViewModel
            {
                Id = s.Id,
                VehicleId = s.VehicleId, // THÊM để dùng trong form
                VehicleName = $"{s.Vehicle?.ModelName} {s.Vehicle?.VariantName}",
                Color = s.ColorCode,
                OwnerType = s.OwnerType,
                OwnerName = s.OwnerType == "EVM" ? "EVM Central" : (dealers.ContainsKey(s.OwnerId) ? dealers[s.OwnerId] : "N/A"),
                Qty = (int)s.Qty,
                EstimatedValue = priceDict.ContainsKey(s.VehicleId) ? (priceDict[s.VehicleId] * s.Qty) : 0,
                UpdatedDate = s.CreatedDate
            }).ToList();

            TotalStockValue = Stocks.Sum(s => s.EstimatedValue) / 1000000000;

            return Page();
        }

        public async Task<IActionResult> OnPostAddStockAsync(int vehicleId, string color, int quantity)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            try
            {
                var existingStock = await _stockService.GetStockByOwnerAndVehicleAsync("EVM", 0, vehicleId, color);

                if (existingStock != null)
                {
                    var newQty = existingStock.Qty + quantity;
                    await _stockService.UpdateStockQtyAsync(existingStock.Id, newQty);
                }
                else
                {
                    await _stockService.CreateOrUpdateStockAsync("EVM", 0, vehicleId, color, quantity);
                }

                TempData["Success"] = "Nhập kho thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateStockAsync(int stockId, int newQuantity)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            try
            {
                if (newQuantity < 0)
                {
                    TempData["Error"] = "Số lượng không được âm.";
                    return RedirectToPage();
                }

                await _stockService.UpdateStockQtyAsync(stockId, newQuantity);
                TempData["Success"] = "Cập nhật số lượng tồn kho thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToPage();
        }

        // THÊM MỚI: Method phân phối stock tới dealer
        public async Task<IActionResult> OnPostDistributeStockAsync(int stockId, int dealerId, int quantity)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            try
            {
                // Validate input
                if (dealerId <= 0)
                {
                    TempData["Error"] = "Vui lòng chọn đại lý.";
                    return RedirectToPage();
                }

                if (quantity <= 0)
                {
                    TempData["Error"] = "Số lượng phải lớn hơn 0.";
                    return RedirectToPage();
                }

                // Distribute stock
                await _stockService.DistributeStockToDealerAsync(stockId, dealerId, quantity);

                // Get dealer name for success message
                var dealer = await _context.Dealers.FindAsync(dealerId);
                var dealerName = dealer?.Name ?? "đại lý";

                TempData["Success"] = $"Đã phân phối {quantity} xe tới {dealerName} thành công!";
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi không xác định: {ex.Message}";
            }

            return RedirectToPage();
        }

        // THÊM MỚI: Class cho Dealer dropdown
        public class DealerSimple
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class VehicleSimple
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class StockViewModel
        {
            public int Id { get; set; }
            public int VehicleId { get; set; } // THÊM
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