using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;

namespace Vehicle_Dealer_Management.Pages.Customer
{
    public class TestDriveModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TestDriveModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<TestDriveViewModel> TestDrives { get; set; } = new();
        public List<DealerSimple> AllDealers { get; set; } = new();
        public List<VehicleSimple> AllVehicles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            // Get all dealers and vehicles for booking form
            AllDealers = await _context.Dealers
                .Where(d => d.Status == "ACTIVE")
                .Select(d => new DealerSimple
                {
                    Id = d.Id,
                    Name = d.Name,
                    Address = d.Address
                })
                .ToListAsync();

            AllVehicles = await _context.Vehicles
                .Where(v => v.Status == "AVAILABLE")
                .Select(v => new VehicleSimple
                {
                    Id = v.Id,
                    Name = v.ModelName + " " + v.VariantName
                })
                .ToListAsync();

            var customerProfile = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customerProfile != null)
            {
                var testDrives = await _context.TestDrives
                    .Where(t => t.CustomerId == customerProfile.Id)
                    .Include(t => t.Vehicle)
                    .Include(t => t.Dealer)
                    .OrderByDescending(t => t.ScheduleTime)
                    .ToListAsync();

                TestDrives = testDrives.Select(t => new TestDriveViewModel
                {
                    Id = t.Id,
                    VehicleName = $"{t.Vehicle?.ModelName} {t.Vehicle?.VariantName}",
                    DealerName = t.Dealer?.Name ?? "N/A",
                    DealerAddress = t.Dealer?.Address ?? "N/A",
                    ScheduleTime = t.ScheduleTime,
                    Status = t.Status,
                    Note = t.Note
                }).ToList();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int dealerId, int vehicleId, string date, string time, string? note)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            // Get or create customer profile
            var customerProfile = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customerProfile == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin khách hàng. Vui lòng cập nhật thông tin cá nhân.";
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
            var dealer = await _context.Dealers
                .FirstOrDefaultAsync(d => d.Id == dealerId && d.Status == "ACTIVE");

            if (dealer == null)
            {
                TempData["Error"] = "Đại lý không tồn tại hoặc không hoạt động.";
                return RedirectToPage();
            }

            // Validate vehicle exists and is available
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == vehicleId && v.Status == "AVAILABLE");

            if (vehicle == null)
            {
                TempData["Error"] = "Mẫu xe không tồn tại hoặc không có sẵn.";
                return RedirectToPage();
            }

            // Create test drive
            var testDrive = new Vehicle_Dealer_Management.DAL.Models.TestDrive
            {
                CustomerId = customerProfile.Id,
                DealerId = dealerId,
                VehicleId = vehicleId,
                ScheduleTime = scheduleDateTime,
                Status = "REQUESTED",
                Note = note,
                CreatedAt = DateTime.UtcNow
            };

            _context.TestDrives.Add(testDrive);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đặt lịch lái thử thành công! Đại lý sẽ xác nhận và liên hệ với bạn.";
            return RedirectToPage();
        }

        public class TestDriveViewModel
        {
            public int Id { get; set; }
            public string VehicleName { get; set; } = "";
            public string DealerName { get; set; } = "";
            public string DealerAddress { get; set; } = "";
            public DateTime ScheduleTime { get; set; }
            public string Status { get; set; } = "";
            public string? Note { get; set; }
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
        }
    }
}

