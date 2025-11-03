using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.Pages.Customer
{
    public class RequestQuoteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IVehicleService _vehicleService;
        private readonly IPricePolicyService _pricePolicyService;
        private readonly IDealerService _dealerService;
        private readonly ISalesDocumentService _salesDocumentService;

        public RequestQuoteModel(
            ApplicationDbContext context,
            IVehicleService vehicleService,
            IPricePolicyService pricePolicyService,
            IDealerService dealerService,
            ISalesDocumentService salesDocumentService)
        {
            _context = context;
            _vehicleService = vehicleService;
            _pricePolicyService = pricePolicyService;
            _dealerService = dealerService;
            _salesDocumentService = salesDocumentService;
        }

        public VehicleViewModel? Vehicle { get; set; }
        public List<DealerViewModel> Dealers { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? vehicleId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            if (!vehicleId.HasValue)
            {
                ErrorMessage = "Vui lòng chọn mẫu xe.";
                return RedirectToPage("/Customer/Vehicles");
            }

            // Get vehicle
            var vehicle = await _vehicleService.GetVehicleByIdAsync(vehicleId.Value);
            if (vehicle == null || vehicle.Status != "AVAILABLE")
            {
                ErrorMessage = "Mẫu xe không tồn tại hoặc không có sẵn.";
                return RedirectToPage("/Customer/Vehicles");
            }

            // Get price policy
            var pricePolicy = await _pricePolicyService.GetActivePricePolicyAsync(vehicle.Id, null);

            Vehicle = new VehicleViewModel
            {
                Id = vehicle.Id,
                ModelName = vehicle.ModelName,
                VariantName = vehicle.VariantName,
                ImageUrl = vehicle.ImageUrl,
                Price = pricePolicy?.Msrp ?? 0
            };

            // Get active dealers
            var dealers = await _dealerService.GetActiveDealersAsync();
            Dealers = dealers.Select(d => new DealerViewModel
            {
                Id = d.Id,
                Name = d.Name,
                Address = d.Address,
                Phone = d.PhoneNumber ?? ""
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int vehicleId, int dealerId, string? color = null, int quantity = 1, string? note = null)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var userIdInt = int.Parse(userId);

            // Get user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userIdInt);

            if (user == null)
            {
                ErrorMessage = "Không tìm thấy thông tin người dùng.";
                return RedirectToPage("/Auth/Login");
            }

            // Get or create customer profile
            var customerProfile = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == userIdInt);

            // Kiểm tra customer có đơn hàng chưa thanh toán đủ 100% không
            if (customerProfile != null)
            {
                var existingOrders = await _salesDocumentService.GetSalesDocumentsByCustomerIdAsync(customerProfile.Id, "ORDER");
                foreach (var existingOrder in existingOrders)
                {
                    var orderTotal = existingOrder.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0;
                    var orderPaid = existingOrder.Payments?.Sum(p => p.Amount) ?? 0;
                    
                    if (orderTotal > 0 && orderPaid < orderTotal)
                    {
                        ErrorMessage = $"Bạn không thể yêu cầu báo giá mới khi còn đơn hàng chưa thanh toán đủ. Đơn hàng #{existingOrder.Id} còn thiếu {(orderTotal - orderPaid):N0} VND. Vui lòng thanh toán đủ 100% đơn hàng trước khi đặt mua xe mới.";
                        return RedirectToPage("/Customer/MyOrders");
                    }
                }
            }

            if (customerProfile == null)
            {
                // Check if profile exists with same email (but UserId is null)
                if (!string.IsNullOrEmpty(user.Email))
                {
                    var existingProfileByEmail = await _context.CustomerProfiles
                        .FirstOrDefaultAsync(c => c.Email == user.Email && c.UserId == null);
                    
                    if (existingProfileByEmail != null)
                    {
                        // Update existing profile to link with this user
                        existingProfileByEmail.UserId = user.Id;
                        existingProfileByEmail.FullName = user.FullName ?? existingProfileByEmail.FullName;
                        existingProfileByEmail.Phone = user.Phone ?? existingProfileByEmail.Phone;
                        customerProfile = existingProfileByEmail;
                        await _context.SaveChangesAsync();
                    }
                }

                // If still no profile, check by phone
                if (customerProfile == null && !string.IsNullOrEmpty(user.Phone))
                {
                    var existingProfileByPhone = await _context.CustomerProfiles
                        .FirstOrDefaultAsync(c => c.Phone == user.Phone && c.UserId == null);
                    
                    if (existingProfileByPhone != null)
                    {
                        // Update existing profile by phone
                        existingProfileByPhone.UserId = user.Id;
                        existingProfileByPhone.FullName = user.FullName ?? existingProfileByPhone.FullName;
                        if (!string.IsNullOrEmpty(user.Email))
                        {
                            // Only set email if it doesn't conflict
                            var emailExists = await _context.CustomerProfiles
                                .AnyAsync(c => c.Email == user.Email && c.Id != existingProfileByPhone.Id);
                            if (!emailExists)
                            {
                                existingProfileByPhone.Email = user.Email;
                            }
                        }
                        customerProfile = existingProfileByPhone;
                        await _context.SaveChangesAsync();
                    }
                }

                // If still no profile, create new one
                if (customerProfile == null)
                {
                    // Check if email already exists in another profile
                    string? emailToUse = user.Email;
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        var emailExists = await _context.CustomerProfiles
                            .AnyAsync(c => c.Email == user.Email);
                        if (emailExists)
                        {
                            // Email already taken, don't set it to avoid unique constraint violation
                            emailToUse = null;
                        }
                    }

                    customerProfile = new CustomerProfile
                    {
                        UserId = user.Id,
                        FullName = user.FullName ?? "Khách hàng",
                        Phone = user.Phone ?? "",
                        Email = emailToUse,
                        Address = "",
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.CustomerProfiles.Add(customerProfile);
                    await _context.SaveChangesAsync();
                }
            }

            // Validate dealer
            var dealer = await _dealerService.GetDealerByIdAsync(dealerId);
            if (dealer == null || dealer.Status != "ACTIVE")
            {
                ErrorMessage = "Đại lý không tồn tại hoặc không hoạt động.";
                return await OnGetAsync(vehicleId);
            }

            // Validate vehicle
            var vehicle = await _vehicleService.GetVehicleByIdAsync(vehicleId);
            if (vehicle == null || vehicle.Status != "AVAILABLE")
            {
                ErrorMessage = "Mẫu xe không tồn tại hoặc không có sẵn.";
                return await OnGetAsync(vehicleId);
            }

            try
            {
                // Note: SalesDocument.CustomerId references CustomerProfile
                // But SalesDocumentService.CreateQuoteAsync uses CustomerRepository which works with Customer model
                // We need to work around this - create quote directly in database

                // Create quote directly in database (bypass service for now)
                var quote = new SalesDocument
                {
                    Type = "QUOTE",
                    DealerId = dealerId,
                    CustomerId = customerProfile.Id,
                    Status = "DRAFT",
                    PromotionId = null,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userIdInt
                };

                _context.SalesDocuments.Add(quote);
                await _context.SaveChangesAsync();

                // Get price policy for this dealer
                var pricePolicy = await _pricePolicyService.GetActivePricePolicyAsync(vehicleId, dealerId);
                var unitPrice = pricePolicy?.Msrp ?? 0;

                // Add line item
                var lineItem = new SalesDocumentLine
                {
                    SalesDocumentId = quote.Id,
                    VehicleId = vehicleId,
                    ColorCode = color ?? "STANDARD",
                    Qty = quantity,
                    UnitPrice = unitPrice,
                    DiscountValue = 0
                };

                _context.SalesDocumentLines.Add(lineItem);
                await _context.SaveChangesAsync();

                SuccessMessage = "Yêu cầu báo giá đã được gửi thành công! Đại lý sẽ liên hệ với bạn sớm nhất.";
                return RedirectToPage("/Customer/MyQuotes");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Có lỗi xảy ra khi tạo yêu cầu báo giá. Vui lòng thử lại.";
                return await OnGetAsync(vehicleId);
            }
        }

        public class VehicleViewModel
        {
            public int Id { get; set; }
            public string ModelName { get; set; } = "";
            public string VariantName { get; set; } = "";
            public string ImageUrl { get; set; } = "";
            public decimal Price { get; set; }
        }

        public class DealerViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Address { get; set; } = "";
            public string Phone { get; set; } = "";
        }
    }
}

