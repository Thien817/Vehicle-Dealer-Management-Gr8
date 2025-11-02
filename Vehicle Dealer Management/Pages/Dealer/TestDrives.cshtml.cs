using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.Pages.Dealer
{
    public class TestDrivesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TestDrivesModel(ApplicationDbContext context)
        {
            _context = context;
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
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            Filter = filter ?? "all";
            var dealerIdInt = int.Parse(dealerId);

            // Get customers for create form
            AllCustomers = await _context.CustomerProfiles
                .Select(c => new CustomerSimple
                {
                    Id = c.Id,
                    Name = c.FullName,
                    Phone = c.Phone
                })
                .ToListAsync();

            // Get vehicles for create form
            AllVehicles = await _context.Vehicles
                .Where(v => v.Status == "AVAILABLE")
                .Select(v => new VehicleSimple
                {
                    Id = v.Id,
                    Name = v.ModelName + " " + v.VariantName
                })
                .ToListAsync();

            // Get test drives
            var query = _context.TestDrives
                .Where(t => t.DealerId == dealerIdInt)
                .Include(t => t.Customer)
                .Include(t => t.Vehicle)
                .AsQueryable();

            // Apply filter
            switch (Filter)
            {
                case "today":
                    query = query.Where(t => t.ScheduleTime.Date == DateTime.Today);
                    break;
                case "requested":
                    query = query.Where(t => t.Status == "REQUESTED");
                    break;
                case "upcoming":
                    query = query.Where(t => t.ScheduleTime > DateTime.Now && t.Status == "CONFIRMED");
                    break;
            }

            var testDrives = await query
                .OrderBy(t => t.ScheduleTime)
                .ToListAsync();

            // Calculate counts
            TodayCount = await _context.TestDrives
                .Where(t => t.DealerId == dealerIdInt && t.ScheduleTime.Date == DateTime.Today)
                .CountAsync();
            RequestedCount = await _context.TestDrives
                .Where(t => t.DealerId == dealerIdInt && t.Status == "REQUESTED")
                .CountAsync();
            ConfirmedCount = await _context.TestDrives
                .Where(t => t.DealerId == dealerIdInt && t.Status == "CONFIRMED")
                .CountAsync();
            DoneCount = await _context.TestDrives
                .Where(t => t.DealerId == dealerIdInt && t.Status == "DONE")
                .CountAsync();

            TestDrives = testDrives.Select(t => new TestDriveViewModel
            {
                Id = t.Id,
                CustomerName = t.Customer?.FullName ?? "N/A",
                CustomerPhone = t.Customer?.Phone ?? "N/A",
                VehicleName = $"{t.Vehicle?.ModelName} {t.Vehicle?.VariantName}",
                ScheduleTime = t.ScheduleTime,
                Status = t.Status,
                Note = t.Note ?? ""
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostConfirmAsync(int id)
        {
            var testDrive = await _context.TestDrives.FindAsync(id);
            if (testDrive != null)
            {
                testDrive.Status = "CONFIRMED";
                testDrive.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMarkDoneAsync(int id)
        {
            var testDrive = await _context.TestDrives.FindAsync(id);
            if (testDrive != null)
            {
                testDrive.Status = "DONE";
                testDrive.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateAsync(int customerId, int vehicleId, DateTime date, TimeSpan time, string? note)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var scheduleTime = date.Add(time);

            var testDrive = new TestDrive
            {
                CustomerId = customerId,
                DealerId = int.Parse(dealerId),
                VehicleId = vehicleId,
                ScheduleTime = scheduleTime,
                Status = "CONFIRMED", // Auto confirm if created by staff
                Note = note,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TestDrives.Add(testDrive);
            await _context.SaveChangesAsync();

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
        }
    }
}

