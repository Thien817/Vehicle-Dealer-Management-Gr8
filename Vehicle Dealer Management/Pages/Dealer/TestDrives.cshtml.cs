using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.Pages.Dealer
{
    public class TestDrivesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IVehicleService _vehicleService;
        private readonly ITestDriveService _testDriveService;

        public TestDrivesModel(
            ApplicationDbContext context,
            IVehicleService vehicleService,
            ITestDriveService testDriveService)
        {
            _context = context;
            _vehicleService = vehicleService;
            _testDriveService = testDriveService;
        }

        public string Filter { get; set; } = "all";
        public int TodayCount { get; set; }
        public int RequestedCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int DoneCount { get; set; }

        public List<CustomerSimple> AllCustomers { get; set; } = new();
        public List<VehicleSimple> AllVehicles { get; set; } = new();
        public List<TestDriveViewModel> TestDrives { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string? filter)
        {
            // Set UserRole from Session
            ViewData["UserRole"] = HttpContext.Session.GetString("UserRole") ?? "DEALER_STAFF";
            ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "User";

            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                TempData["Error"] = "Không tìm thấy thông tin đại lý. Vui lòng đăng nhập lại.";
                return RedirectToPage("/Auth/Login");
            }

            Filter = filter ?? "all";
            var dealerIdInt = int.Parse(dealerId);
            
            // Debug: Verify dealer exists
            var dealer = await _context.Dealers.FindAsync(dealerIdInt);
            if (dealer == null)
            {
                TempData["Error"] = $"Đại lý với ID {dealerIdInt} không tồn tại.";
                return RedirectToPage("/Auth/Login");
            }

            // Get customers for create form - use CustomerProfiles (not Customers)
            var customerProfiles = await _context.CustomerProfiles
                .OrderBy(c => c.FullName)
                .ToListAsync();
            AllCustomers = customerProfiles.Select(c => new CustomerSimple
            {
                Id = c.Id,
                Name = c.FullName,
                Phone = c.Phone
            }).ToList();

            // Get vehicles for create form
            var vehicles = await _vehicleService.GetAvailableVehiclesAsync();
            AllVehicles = vehicles.Select(v => new VehicleSimple
            {
                Id = v.Id,
                Name = v.ModelName + " " + v.VariantName
            }).ToList();

            // Get test drives using service
            IEnumerable<Vehicle_Dealer_Management.DAL.Models.TestDrive> testDrives;

            switch (Filter)
            {
                case "today":
                    testDrives = await _testDriveService.GetTestDrivesByDealerAndDateAsync(dealerIdInt, DateTime.Today);
                    break;
                case "requested":
                    testDrives = await _testDriveService.GetTestDrivesByStatusAsync("REQUESTED", dealerIdInt);
                    break;
                case "upcoming":
                    var allTestDrives = await _testDriveService.GetTestDrivesByDealerIdAsync(dealerIdInt);
                    testDrives = allTestDrives.Where(t => t.ScheduleTime > DateTime.Now && t.Status == "CONFIRMED");
                    break;
                default:
                    testDrives = await _testDriveService.GetTestDrivesByDealerIdAsync(dealerIdInt);
                    break;
            }

            var testDrivesList = testDrives.ToList();

            // Calculate counts
            var todayTestDrives = await _testDriveService.GetTestDrivesByDealerAndDateAsync(dealerIdInt, DateTime.Today);
            TodayCount = todayTestDrives.Count();

            var requestedTestDrives = await _testDriveService.GetTestDrivesByStatusAsync("REQUESTED", dealerIdInt);
            RequestedCount = requestedTestDrives.Count();

            var confirmedTestDrives = await _testDriveService.GetTestDrivesByStatusAsync("CONFIRMED", dealerIdInt);
            ConfirmedCount = confirmedTestDrives.Count();

            var doneTestDrives = await _testDriveService.GetTestDrivesByStatusAsync("DONE", dealerIdInt);
            DoneCount = doneTestDrives.Count();

            TestDrives = testDrivesList.Select(t => new TestDriveViewModel
            {
                Id = t.Id,
                CustomerName = t.Customer?.FullName ?? "N/A",
                CustomerPhone = t.Customer?.Phone ?? "N/A",
                VehicleName = t.Vehicle != null ? $"{t.Vehicle.ModelName} {t.Vehicle.VariantName}" : "N/A",
                ScheduleTime = t.ScheduleTime,
                Status = t.Status,
                Note = t.Note ?? "",
                IsSlotBooking = t.ParentSlotId.HasValue,
                SlotTime = t.ParentSlot != null ? $"{t.ParentSlot.SlotStartTime} - {t.ParentSlot.SlotEndTime}" : null
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostConfirmAsync(int id)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                TempData["Error"] = "Không tìm thấy thông tin đại lý. Vui lòng đăng nhập lại.";
                return RedirectToPage("/Auth/Login");
            }

            var testDrive = await _testDriveService.GetTestDriveByIdAsync(id);
            if (testDrive == null)
            {
                TempData["Error"] = "Không tìm thấy lịch lái thử này.";
                return RedirectToPage();
            }

            // Verify this booking belongs to this dealer
            if (testDrive.DealerId != int.Parse(dealerId))
            {
                TempData["Error"] = "Bạn không có quyền xác nhận lịch này.";
                return RedirectToPage();
            }

            await _testDriveService.UpdateTestDriveStatusAsync(id, "CONFIRMED");
            TempData["Success"] = "Đã xác nhận lịch lái thử thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMarkDoneAsync(int id)
        {
            await _testDriveService.UpdateTestDriveStatusAsync(id, "DONE");
            TempData["Success"] = "Đã đánh dấu hoàn thành lịch lái thử.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCancelAsync(int id)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                TempData["Error"] = "Không tìm thấy thông tin đại lý. Vui lòng đăng nhập lại.";
                return RedirectToPage("/Auth/Login");
            }

            var testDrive = await _testDriveService.GetTestDriveByIdAsync(id);
            if (testDrive == null)
            {
                TempData["Error"] = "Không tìm thấy lịch lái thử này.";
                return RedirectToPage();
            }

            // Verify this booking belongs to this dealer
            if (testDrive.DealerId != int.Parse(dealerId))
            {
                TempData["Error"] = "Bạn không có quyền hủy lịch này.";
                return RedirectToPage();
            }

            // Check if can be cancelled (not already done or cancelled)
            if (testDrive.Status == "DONE" || testDrive.Status == "CANCELLED")
            {
                TempData["Error"] = "Lịch này không thể hủy.";
                return RedirectToPage();
            }

            await _testDriveService.UpdateTestDriveStatusAsync(id, "CANCELLED");
            TempData["Success"] = "Đã hủy lịch lái thử thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateAsync(int customerId, int vehicleId, string date, string time, string? note)
        {
            // Set UserRole from Session
            ViewData["UserRole"] = HttpContext.Session.GetString("UserRole") ?? "DEALER_STAFF";
            ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "User";

            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                TempData["Error"] = "Không tìm thấy thông tin đại lý. Vui lòng đăng nhập lại.";
                return RedirectToPage("/Auth/Login");
            }

            try
            {
                // Validate inputs
                if (customerId <= 0 || vehicleId <= 0 || string.IsNullOrEmpty(date) || string.IsNullOrEmpty(time))
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

                // Validate customer exists
                var customer = await _context.CustomerProfiles.FindAsync(customerId);
                if (customer == null)
                {
                    TempData["Error"] = "Khách hàng không tồn tại.";
                    return RedirectToPage();
                }

                // Validate vehicle exists
                var vehicle = await _vehicleService.GetVehicleByIdAsync(vehicleId);
                if (vehicle == null)
                {
                    TempData["Error"] = "Mẫu xe không tồn tại.";
                    return RedirectToPage();
                }

                var testDrive = new Vehicle_Dealer_Management.DAL.Models.TestDrive
                {
                    CustomerId = customerId,
                    DealerId = int.Parse(dealerId),
                    VehicleId = vehicleId,
                    ScheduleTime = scheduleDateTime,
                    Status = "CONFIRMED", // Auto confirm if created by staff
                    Note = note,
                    CreatedAt = DateTime.UtcNow
                };

                await _testDriveService.CreateTestDriveAsync(testDrive);

                TempData["Success"] = "Đặt lịch lái thử thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi đặt lịch lái thử. Vui lòng thử lại.";
                System.Diagnostics.Debug.WriteLine($"Error creating test drive: {ex.Message}");
            }

            return RedirectToPage();
        }

        public class CustomerSimple
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Phone { get; set; } = "";
        }

        public class VehicleSimple
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class TestDriveViewModel
        {
            public int Id { get; set; }
            public string CustomerName { get; set; } = "";
            public string CustomerPhone { get; set; } = "";
            public string VehicleName { get; set; } = "";
            public DateTime ScheduleTime { get; set; }
            public string Status { get; set; } = "";
            public string Note { get; set; } = "";
            public bool IsSlotBooking { get; set; }
            public string? SlotTime { get; set; }
        }
    }
}

