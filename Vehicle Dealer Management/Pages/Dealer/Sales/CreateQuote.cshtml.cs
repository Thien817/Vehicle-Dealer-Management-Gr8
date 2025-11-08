using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.Pages.Dealer.Sales
{
    public class CreateQuoteModel : PageModel
    {
        private readonly ISalesDocumentService _salesDocumentService;
        private readonly IVehicleService _vehicleService;
        private readonly IPricePolicyService _pricePolicyService;
        private readonly ICustomerService _customerService;
        private readonly IStockService _stockService; // ✅ THÊM
        private readonly ApplicationDbContext _context;

        public CreateQuoteModel(
            ISalesDocumentService salesDocumentService,
            IVehicleService vehicleService,
            IPricePolicyService pricePolicyService,
            ICustomerService customerService,
            IStockService stockService, // ✅ THÊM
            ApplicationDbContext context)
        {
            _salesDocumentService = salesDocumentService;
            _vehicleService = vehicleService;
            _pricePolicyService = pricePolicyService;
            _customerService = customerService;
            _stockService = stockService; // ✅ THÊM
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

            // ✅ THAY ĐỔI: Chỉ load xe có trong kho dealer
            var dealerStocks = await _stockService.GetStocksByOwnerAsync("DEALER", dealerIdInt);

            // Group by vehicle to avoid duplicates
            var vehicleGroups = dealerStocks
                .Where(s => s.Qty > 0) // Chỉ lấy xe còn hàng
                .GroupBy(s => s.VehicleId);

            foreach (var group in vehicleGroups)
            {
                var vehicleId = group.Key;
                var vehicle = await _vehicleService.GetVehicleByIdAsync(vehicleId);

                if (vehicle == null || vehicle.Status != "AVAILABLE") continue;

                // Get price policy
                var pricePolicy = await _pricePolicyService.GetActivePricePolicyAsync(vehicleId, dealerIdInt);

                // Calculate total available quantity
                var totalQty = group.Sum(s => (int)s.Qty);

                Vehicles.Add(new VehicleViewModel
                {
                    Id = vehicle.Id,
                    Name = vehicle.ModelName,
                    Variant = vehicle.VariantName,
                    Msrp = pricePolicy?.Msrp ?? 0,
                    AvailableQty = totalQty, // ✅ THÊM để hiển thị số lượng
                    AvailableColors = group.Select(s => new ColorStock
                    {
                        Color = s.ColorCode,
                        Qty = (int)s.Qty
                    }).ToList()
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

        public async Task<IActionResult> OnPostAsync(
            int customerId,
            int vehicleId,
            string color,
            int quantity,
            string action,
            int? promotionId,
            decimal additionalDiscount,
            string? note)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(dealerId) || string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var dealerIdInt = int.Parse(dealerId);
            var userIdInt = int.Parse(userId);

            // ✅ VALIDATION: Kiểm tra xe có trong kho dealer không
            var dealerStock = await _stockService.GetStockByOwnerAndVehicleAsync(
                "DEALER",
                dealerIdInt,
                vehicleId,
                color
            );

            if (dealerStock == null)
            {
                TempData["Error"] = "Xe này chưa được phân phối cho đại lý của bạn!";
                return RedirectToPage();
            }

            if (dealerStock.Qty < quantity)
            {
                TempData["Error"] = $"Không đủ hàng trong kho. Có sẵn: {dealerStock.Qty}, yêu cầu: {quantity}";
                return RedirectToPage();
            }

            // Create sales document (QUOTE)
            var salesDocument = await _salesDocumentService.CreateQuoteAsync(
                dealerIdInt,
                customerId,
                userIdInt,
                promotionId);

            // Update status if sending
            if (action == "send")
            {
                await _salesDocumentService.UpdateSalesDocumentStatusAsync(salesDocument.Id, "SENT");
            }

            // Get price policy
            var pricePolicy = await _pricePolicyService.GetActivePricePolicyAsync(vehicleId, dealerIdInt);
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

            TempData["Success"] = "Tạo báo giá thành công!";
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
            public int AvailableQty { get; set; } // ✅ THÊM
            public List<ColorStock> AvailableColors { get; set; } = new(); // ✅ THÊM
        }

        public class ColorStock // ✅ THÊM
        {
            public string Color { get; set; } = "";
            public int Qty { get; set; }
        }

        public class PromotionViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }
    }
}