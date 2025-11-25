using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.Pages.Customer
{
    public class TestDriveModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDealerService _dealerService;
        private readonly IVehicleService _vehicleService;
        private readonly ITestDriveService _testDriveService;

        public TestDriveModel(
            ApplicationDbContext context,
            IDealerService dealerService,
            IVehicleService vehicleService,
            ITestDriveService testDriveService)
        {
            _context = context;
            _dealerService = dealerService;
            _vehicleService = vehicleService;
            _testDriveService = testDriveService;
        }

        public List<TestDriveViewModel> TestDrives { get; set; } = new();
        public List<DealerSimple> AllDealers { get; set; } = new();
        public List<VehicleSimple> AllVehicles { get; set; } = new();
        
        // NEW: Slot browsing
        public DateTime SelectedDate { get; set; }
        public int? SelectedDealerId { get; set; }
        public List<SlotViewModel> AvailableSlots { get; set; } = new();
        public bool HasActiveBooking { get; set; } = false;

        public async Task<IActionResult> OnGetAsync(string? date, int? dealerId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");
            
            if (string.IsNullOrEmpty(userId) || userRole != "CUSTOMER")
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));
            if (user == null || user.Role?.Code != "CUSTOMER")
            {
                return RedirectToPage("/Auth/Login");
            }

            // Get all dealers and vehicles for booking form
            var activeDealers = await _dealerService.GetActiveDealersAsync();
            AllDealers = activeDealers.Select(d => new DealerSimple
            {
                Id = d.Id,
                Name = d.Name,
                Address = d.Address
            }).ToList();

            var availableVehicles = await _vehicleService.GetAvailableVehiclesAsync();
            AllVehicles = availableVehicles.Select(v => new VehicleSimple
            {
                Id = v.Id,
                Name = v.ModelName + " " + v.VariantName
            }).ToList();

            var customerProfile = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            // Create customer profile if not exists
            if (customerProfile == null)
            {
                customerProfile = await CreateOrGetCustomerProfileAsync(user);
            }

            // Load my bookings
            if (customerProfile != null)
            {
                var testDrives = await _testDriveService.GetTestDrivesByCustomerIdAsync(customerProfile.Id);

                TestDrives = testDrives.Select(t => new TestDriveViewModel
                {
                    Id = t.Id,
                    VehicleId = t.VehicleId ?? 0,
                    VehicleName = t.Vehicle != null ? $"{t.Vehicle.ModelName} {t.Vehicle.VariantName}" : "N/A",
                    DealerId = t.DealerId,
                    DealerName = t.Dealer?.Name ?? "N/A",
                    DealerAddress = t.Dealer?.Address ?? "N/A",
                    ScheduleTime = t.ScheduleTime,
                    Status = t.Status,
                    Note = t.Note,
                    IsSlotBooking = t.ParentSlotId.HasValue,
                    SlotTime = t.ParentSlot != null ? $"{t.ParentSlot.SlotStartTime} - {t.ParentSlot.SlotEndTime}" : null
                }).ToList();

                // Check if customer has active booking
                HasActiveBooking = await _testDriveService.HasActiveBookingAsync(customerProfile.Id);
            }

            // NEW: Load available slots
            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
            {
                SelectedDate = parsedDate;
            }
            else
            {
                SelectedDate = DateTime.Today;
            }

            if (dealerId.HasValue)
            {
                SelectedDealerId = dealerId.Value;
                await LoadAvailableSlotsAsync(dealerId.Value, SelectedDate);
            }

            return Page();
        }

        private async Task LoadAvailableSlotsAsync(int dealerId, DateTime date)
        {
            var slots = await _testDriveService.GetAvailableSlotsByDealerAndDateAsync(dealerId, date);
            
            AvailableSlots = new List<SlotViewModel>();
            foreach (var slot in slots)
            {
                var availableVehicleIds = string.IsNullOrEmpty(slot.AvailableVehicleIds) 
                    ? new List<int>() 
                    : slot.AvailableVehicleIds.Split(',').Select(int.Parse).ToList();

                // Get bookings for this slot to see which vehicles are already booked
                var bookings = await _testDriveService.GetBookingsBySlotIdAsync(slot.Id);
                var bookedVehicleIds = bookings
                    .Where(b => b.Status != "CANCELLED")
                    .Select(b => b.VehicleId)
                    .Where(vid => vid.HasValue)
                    .Select(vid => vid.Value)
                    .ToList();

                var vehicles = AllVehicles
                    .Where(v => availableVehicleIds.Contains(v.Id))
                    .Select(v => new VehicleSimple
                    {
                        Id = v.Id,
                        Name = v.Name,
                        IsBooked = bookedVehicleIds.Contains(v.Id)
                    })
                    .ToList();

                var isFull = slot.IsFull;

                AvailableSlots.Add(new SlotViewModel
                {
                    Id = slot.Id,
                    StartTime = slot.SlotStartTime ?? "",
                    EndTime = slot.SlotEndTime ?? "",
                    AvailableSlots = slot.MaxSlots.GetValueOrDefault() - slot.CurrentBookings,
                    MaxSlots = slot.MaxSlots.GetValueOrDefault(),
                    AvailableVehicles = vehicles,
                    BookedVehicleIds = bookedVehicleIds,
                    IsFull = isFull
                });
            }
        }

        public async Task<IActionResult> OnPostBookSlotAsync(int slotId, int vehicleId, string? note)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == int.Parse(userId));
            if (user == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            var customerProfile = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customerProfile == null)
            {
                customerProfile = await CreateOrGetCustomerProfileAsync(user);
            }

            try
            {
                await _testDriveService.BookSlotAsync(slotId, customerProfile.Id, vehicleId, note);
                TempData["Success"] = "Đặt lịch lái thử thành công! Đại lý sẽ liên hệ với bạn.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
            }

            return RedirectToPage();
        }

        // Keep old booking methods for backward compatibility...
        public async Task<IActionResult> OnPostAsync(int dealerId, int vehicleId, string date, string time, string? note)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");
            
            if (string.IsNullOrEmpty(userId) || userRole != "CUSTOMER")
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));
            if (user == null || user.Role?.Code != "CUSTOMER")
            {
                return RedirectToPage("/Auth/Login");
            }

            var customerProfile = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customerProfile == null)
            {
                customerProfile = await CreateOrGetCustomerProfileAsync(user);
            }

            // Load dealers and vehicles for form
            var activeDealers = await _dealerService.GetActiveDealersAsync();
            AllDealers = activeDealers.Select(d => new DealerSimple
            {
                Id = d.Id,
                Name = d.Name,
                Address = d.Address
            }).ToList();

            var availableVehicles = await _vehicleService.GetAvailableVehiclesAsync();
            AllVehicles = availableVehicles.Select(v => new VehicleSimple
            {
                Id = v.Id,
                Name = v.ModelName + " " + v.VariantName
            }).ToList();

            // Load test drives for display
            var testDrives = await _testDriveService.GetTestDrivesByCustomerIdAsync(customerProfile.Id);
            TestDrives = testDrives.Select(t => new TestDriveViewModel
            {
                Id = t.Id,
                VehicleId = t.VehicleId ?? 0,
                VehicleName = t.Vehicle != null ? $"{t.Vehicle.ModelName} {t.Vehicle.VariantName}" : "N/A",
                DealerId = t.DealerId,
                DealerName = t.Dealer?.Name ?? "N/A",
                DealerAddress = t.Dealer?.Address ?? "N/A",
                ScheduleTime = t.ScheduleTime,
                Status = t.Status,
                Note = t.Note,
                IsSlotBooking = t.ParentSlotId.HasValue,
                SlotTime = t.ParentSlot != null ? $"{t.ParentSlot.SlotStartTime} - {t.ParentSlot.SlotEndTime}" : null
            }).ToList();

            // Validate inputs
            if (dealerId <= 0 || vehicleId <= 0 || string.IsNullOrEmpty(date) || string.IsNullOrEmpty(time))
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin.";
                TempData["KeepModalOpen"] = "true";
                return Page();
            }

            // Parse date and time
            if (!DateTime.TryParse(date, out var scheduleDate))
            {
                TempData["Error"] = "Ngày không hợp lệ.";
                TempData["KeepModalOpen"] = "true";
                return Page();
            }

            if (!TimeSpan.TryParse(time, out var scheduleTime))
            {
                TempData["Error"] = "Giờ không hợp lệ.";
                TempData["KeepModalOpen"] = "true";
                return Page();
            }

            var scheduleDateTime = scheduleDate.Date.Add(scheduleTime);

            // Validate schedule time is in the future
            if (scheduleDateTime < DateTime.Now)
            {
                TempData["Error"] = "Thời gian đặt lịch phải trong tương lai.";
                TempData["KeepModalOpen"] = "true";
                return Page();
            }

            // Validate dealer exists and is active
            var dealer = await _dealerService.GetDealerByIdAsync(dealerId);
            if (dealer == null || dealer.Status != "ACTIVE")
            {
                TempData["Error"] = "Đại lý không tồn tại hoặc không hoạt động.";
                TempData["KeepModalOpen"] = "true";
                return Page();
            }

            // Check if customer already has an active booking
            if (await _testDriveService.HasActiveBookingAsync(customerProfile.Id))
            {
                TempData["Error"] = "Bạn đã có một lịch lái thử chưa hoàn thành. Vui lòng hoàn thành hoặc hủy lịch hiện tại trước khi đặt lịch mới.";
                TempData["KeepModalOpen"] = "true";
                return Page();
            }

            // Validate vehicle exists and is available
            var vehicle = await _vehicleService.GetVehicleByIdAsync(vehicleId);
            if (vehicle == null || vehicle.Status != "AVAILABLE")
            {
                TempData["Error"] = "Mẫu xe không tồn tại hoặc không có sẵn.";
                TempData["KeepModalOpen"] = "true";
                return Page();
            }

            // Create test drive using service
            var testDrive = new TestDrive
            {
                CustomerId = customerProfile.Id,
                DealerId = dealerId,
                VehicleId = vehicleId,
                ScheduleTime = scheduleDateTime,
                Status = "REQUESTED",
                Note = note
            };

            await _testDriveService.CreateTestDriveAsync(testDrive);

            TempData["Success"] = "Đặt lịch lái thử thành công! Đại lý sẽ xác nhận và liên hệ với bạn.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCancelAsync(int testDriveId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");
            
            if (string.IsNullOrEmpty(userId) || userRole != "CUSTOMER")
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));
            if (user == null || user.Role?.Code != "CUSTOMER")
            {
                return RedirectToPage("/Auth/Login");
            }

            var customerProfile = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customerProfile == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToPage();
            }

            var testDrive = await _testDriveService.GetTestDriveByIdAsync(testDriveId);
            if (testDrive == null)
            {
                TempData["Error"] = "Không tìm thấy lịch lái thử này.";
                return RedirectToPage();
            }

            // Verify ownership
            if (testDrive.CustomerId != customerProfile.Id)
            {
                TempData["Error"] = "Bạn không có quyền hủy lịch này.";
                return RedirectToPage();
            }

            // Check if can be cancelled
            if (testDrive.Status == "DONE" || testDrive.Status == "CANCELLED")
            {
                TempData["Error"] = "Lịch này không thể hủy.";
                return RedirectToPage();
            }

            // Check if past
            if (testDrive.ScheduleTime < DateTime.Now)
            {
                TempData["Error"] = "Không thể hủy lịch đã qua.";
                return RedirectToPage();
            }

            // Cancel test drive
            await _testDriveService.UpdateTestDriveStatusAsync(testDriveId, "CANCELLED");

            TempData["Success"] = "Đã hủy lịch lái thử thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(int testDriveId, int dealerId, int vehicleId, string date, string time, string? note)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");
            
            if (string.IsNullOrEmpty(userId) || userRole != "CUSTOMER")
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));
            if (user == null || user.Role?.Code != "CUSTOMER")
            {
                return RedirectToPage("/Auth/Login");
            }

            var customerProfile = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customerProfile == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToPage();
            }

            var testDrive = await _testDriveService.GetTestDriveByIdAsync(testDriveId);
            if (testDrive == null)
            {
                TempData["Error"] = "Không tìm thấy lịch lái thử này.";
                return RedirectToPage();
            }

            // Verify ownership
            if (testDrive.CustomerId != customerProfile.Id)
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa lịch này.";
                return RedirectToPage();
            }

            // Check if can be edited
            if (testDrive.Status == "DONE" || testDrive.Status == "CANCELLED")
            {
                TempData["Error"] = "Lịch này không thể chỉnh sửa.";
                return RedirectToPage();
            }

            // Validate inputs
            if (dealerId <= 0 || vehicleId <= 0 || string.IsNullOrEmpty(date) || string.IsNullOrEmpty(time))
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin.";
                return RedirectToPage();
            }

            // Parse date and time
            if (!DateTime.TryParse(date, out var scheduleDate))
            {
                TempData["Error"] = "Ngày không hợp lệ.";
                return RedirectToPage();
            }

            if (!TimeSpan.TryParse(time, out var scheduleTime))
            {
                TempData["Error"] = "Giờ không hợp lệ.";
                return RedirectToPage();
            }

            var scheduleDateTime = scheduleDate.Date.Add(scheduleTime);

            // Validate schedule time is in the future
            if (scheduleDateTime < DateTime.Now)
            {
                TempData["Error"] = "Thời gian đặt lịch phải trong tương lai.";
                return RedirectToPage();
            }

            // Validate dealer exists and is active
            var dealer = await _dealerService.GetDealerByIdAsync(dealerId);
            if (dealer == null || dealer.Status != "ACTIVE")
            {
                TempData["Error"] = "Đại lý không tồn tại hoặc không hoạt động.";
                return RedirectToPage();
            }

            // Validate vehicle exists and is available
            var vehicle = await _vehicleService.GetVehicleByIdAsync(vehicleId);
            if (vehicle == null || vehicle.Status != "AVAILABLE")
            {
                TempData["Error"] = "Mẫu xe không tồn tại hoặc không có sẵn.";
                return RedirectToPage();
            }

            // Update test drive
            testDrive.DealerId = dealerId;
            testDrive.VehicleId = vehicleId;
            testDrive.ScheduleTime = scheduleDateTime;
            testDrive.Note = note;
            // If was CONFIRMED and edited, change back to REQUESTED for dealer to reconfirm
            if (testDrive.Status == "CONFIRMED")
            {
                testDrive.Status = "REQUESTED";
            }

            await _testDriveService.UpdateTestDriveAsync(testDrive);

            TempData["Success"] = "Cập nhật lịch lái thử thành công! Đại lý sẽ xác nhận lại và liên hệ với bạn.";
            return RedirectToPage();
        }

        private async Task<CustomerProfile> CreateOrGetCustomerProfileAsync(User user)
        {
            if (!string.IsNullOrEmpty(user.Email))
            {
                var existingProfileByEmail = await _context.CustomerProfiles
                    .FirstOrDefaultAsync(c => c.Email == user.Email && c.UserId == null);
                
                if (existingProfileByEmail != null)
                {
                    existingProfileByEmail.UserId = user.Id;
                    existingProfileByEmail.FullName = user.FullName ?? existingProfileByEmail.FullName;
                    existingProfileByEmail.Phone = user.Phone ?? existingProfileByEmail.Phone;
                    await _context.SaveChangesAsync();
                    return existingProfileByEmail;
                }
            }

            if (!string.IsNullOrEmpty(user.Phone))
            {
                var existingProfileByPhone = await _context.CustomerProfiles
                    .FirstOrDefaultAsync(c => c.Phone == user.Phone && c.UserId == null);
                
                if (existingProfileByPhone != null)
                {
                    existingProfileByPhone.UserId = user.Id;
                    existingProfileByPhone.FullName = user.FullName ?? existingProfileByPhone.FullName;
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        var emailExists = await _context.CustomerProfiles
                            .AnyAsync(c => c.Email == user.Email && c.Id != existingProfileByPhone.Id);
                        if (!emailExists)
                        {
                            existingProfileByPhone.Email = user.Email;
                        }
                    }
                    await _context.SaveChangesAsync();
                    return existingProfileByPhone;
                }
            }

            string? emailToUse = user.Email;
            if (!string.IsNullOrEmpty(user.Email))
            {
                var emailExists = await _context.CustomerProfiles.AnyAsync(c => c.Email == user.Email);
                if (emailExists)
                {
                    emailToUse = null;
                }
            }

            var customerProfile = new CustomerProfile
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
            return customerProfile;
        }

        public class TestDriveViewModel
        {
            public int Id { get; set; }
            public int VehicleId { get; set; }
            public string VehicleName { get; set; } = "";
            public int DealerId { get; set; }
            public string DealerName { get; set; } = "";
            public string DealerAddress { get; set; } = "";
            public DateTime ScheduleTime { get; set; }
            public string Status { get; set; } = "";
            public string? Note { get; set; }
            public bool IsSlotBooking { get; set; }
            public string? SlotTime { get; set; }
        }

        public class DealerSimple
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Address { get; set; } = "";
        }

        public class VehicleSimple
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public bool IsBooked { get; set; } = false;
        }

        public class SlotViewModel
        {
            public int Id { get; set; }
            public string StartTime { get; set; } = "";
            public string EndTime { get; set; } = "";
            public int AvailableSlots { get; set; }
            public int MaxSlots { get; set; }
            public List<VehicleSimple> AvailableVehicles { get; set; } = new();
            public List<int> BookedVehicleIds { get; set; } = new();
            public bool IsFull { get; set; } = false;
        }
    }
}

