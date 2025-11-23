using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.Models;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.Pages.Dealer
{
    public class TestDriveSlotsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IVehicleService _vehicleService;
        private readonly ITestDriveService _testDriveService;

        public TestDriveSlotsModel(
            ApplicationDbContext context,
            IVehicleService vehicleService,
            ITestDriveService testDriveService)
        {
            _context = context;
            _vehicleService = vehicleService;
            _testDriveService = testDriveService;
        }

        public DateTime SelectedDate { get; set; }
        public List<SlotViewModel> Slots { get; set; } = new();
        public List<VehicleSimple> AllVehicles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string? date)
        {
            // Set UserRole from Session
            ViewData["UserRole"] = HttpContext.Session.GetString("UserRole") ?? "DEALER_STAFF";
            ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "User";

            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                TempData["Error"] = "Không tìm th?y thông tin ??i lý. Vui lòng ??ng nh?p l?i.";
                return RedirectToPage("/Auth/Login");
            }

            var dealerIdInt = int.Parse(dealerId);

            // Parse selected date
            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
            {
                SelectedDate = parsedDate;
            }
            else
            {
                SelectedDate = DateTime.Today;
            }

            // Get available vehicles
            var vehicles = await _vehicleService.GetAvailableVehiclesAsync();
            AllVehicles = vehicles.Select(v => new VehicleSimple
            {
                Id = v.Id,
                Name = v.ModelName + " " + v.VariantName
            }).ToList();

            // Get slots for selected date
            var slots = await _testDriveService.GetSlotsByDealerAndDateAsync(dealerIdInt, SelectedDate);
            
            Slots = new List<SlotViewModel>();
            foreach (var slot in slots.OrderBy(s => s.SlotStartTime))
            {
                // Get bookings for this slot
                var bookings = await _context.TestDrives
                    .Include(t => t.Customer)
                    .Include(t => t.Vehicle)
                    .Where(t => t.ParentSlotId == slot.Id && t.Status != "CANCELLED")
                    .ToListAsync();

                var availableVehicleIds = string.IsNullOrEmpty(slot.AvailableVehicleIds) 
                    ? new List<int>() 
                    : slot.AvailableVehicleIds.Split(',').Select(int.Parse).ToList();

                var availableVehicleNames = AllVehicles
                    .Where(v => availableVehicleIds.Contains(v.Id))
                    .Select(v => v.Name)
                    .ToList();

                Slots.Add(new SlotViewModel
                {
                    Id = slot.Id,
                    StartTime = slot.SlotStartTime ?? "",
                    EndTime = slot.SlotEndTime ?? "",
                    MaxSlots = slot.MaxSlots ?? 0,
                    CurrentBookings = bookings.Count,
                    AvailableVehicleNames = string.Join(", ", availableVehicleNames),
                    Bookings = bookings.Select(b => new BookingViewModel
                    {
                        Id = b.Id,
                        CustomerName = b.Customer?.FullName ?? "N/A",
                        CustomerPhone = b.Customer?.Phone ?? "N/A",
                        VehicleName = b.Vehicle != null ? $"{b.Vehicle.ModelName} {b.Vehicle.VariantName}" : "N/A",
                        Status = b.Status,
                        Note = b.Note
                    }).ToList()
                });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCreateSlotAsync(
            string date, 
            string startTime, 
            string endTime, 
            int maxSlots,
            int[] vehicleIds)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                TempData["Error"] = "Không tìm th?y thông tin ??i lý.";
                return RedirectToPage();
            }

            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(date) || string.IsNullOrEmpty(startTime) || string.IsNullOrEmpty(endTime))
                {
                    TempData["Error"] = "Vui lòng ?i?n ??y ?? thông tin.";
                    return RedirectToPage(new { date });
                }

                if (!DateTime.TryParse(date, out var scheduleDate))
                {
                    TempData["Error"] = "Ngày không h?p l?.";
                    return RedirectToPage(new { date });
                }

                if (maxSlots <= 0)
                {
                    TempData["Error"] = "S? l??ng slot ph?i l?n h?n 0.";
                    return RedirectToPage(new { date });
                }

                if (vehicleIds == null || vehicleIds.Length == 0)
                {
                    TempData["Error"] = "Vui lòng ch?n ít nh?t 1 m?u xe.";
                    return RedirectToPage(new { date });
                }

                // Validate date is not in the past
                if (scheduleDate.Date < DateTime.Today)
                {
                    TempData["Error"] = "Không th? t?o slot cho ngày trong quá kh?.";
                    return RedirectToPage(new { date });
                }

                var slot = new TestDrive
                {
                    DealerId = int.Parse(dealerId),
                    ScheduleTime = scheduleDate.Date,
                    IsSlot = true,
                    SlotStartTime = startTime,
                    SlotEndTime = endTime,
                    MaxSlots = maxSlots,
                    AvailableVehicleIds = string.Join(",", vehicleIds),
                    Status = "AVAILABLE",
                    CreatedAt = DateTime.UtcNow
                };

                await _testDriveService.CreateSlotAsync(slot);

                TempData["Success"] = $"?ã t?o slot {startTime} - {endTime} thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có l?i x?y ra: {ex.Message}";
            }

            return RedirectToPage(new { date });
        }

        public async Task<IActionResult> OnPostDeleteSlotAsync(int slotId, string? returnDate)
        {
            var dealerId = HttpContext.Session.GetString("DealerId");
            if (string.IsNullOrEmpty(dealerId))
            {
                TempData["Error"] = "Không tìm th?y thông tin ??i lý.";
                return RedirectToPage();
            }

            try
            {
                // Verify ownership
                var slot = await _testDriveService.GetSlotByIdAsync(slotId);
                if (slot == null)
                {
                    TempData["Error"] = "Không tìm th?y slot.";
                    return RedirectToPage(new { date = returnDate });
                }

                if (slot.DealerId != int.Parse(dealerId))
                {
                    TempData["Error"] = "B?n không có quy?n xóa slot này.";
                    return RedirectToPage(new { date = returnDate });
                }

                await _testDriveService.DeleteSlotAsync(slotId);

                TempData["Success"] = "?ã xóa slot thành công.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có l?i x?y ra: {ex.Message}";
            }

            return RedirectToPage(new { date = returnDate });
        }

        public async Task<IActionResult> OnPostConfirmBookingAsync(int bookingId, string? returnDate)
        {
            try
            {
                await _testDriveService.UpdateTestDriveStatusAsync(bookingId, "CONFIRMED");
                TempData["Success"] = "?ã xác nh?n booking.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có l?i x?y ra: {ex.Message}";
            }

            return RedirectToPage(new { date = returnDate });
        }

        public async Task<IActionResult> OnPostMarkDoneAsync(int bookingId, string? returnDate)
        {
            try
            {
                await _testDriveService.UpdateTestDriveStatusAsync(bookingId, "DONE");
                TempData["Success"] = "?ã ?ánh d?u hoàn thành.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có l?i x?y ra: {ex.Message}";
            }

            return RedirectToPage(new { date = returnDate });
        }

        public class SlotViewModel
        {
            public int Id { get; set; }
            public string StartTime { get; set; } = "";
            public string EndTime { get; set; } = "";
            public int MaxSlots { get; set; }
            public int CurrentBookings { get; set; }
            public string AvailableVehicleNames { get; set; } = "";
            public List<BookingViewModel> Bookings { get; set; } = new();

            public bool IsFull => CurrentBookings >= MaxSlots;
            public int AvailableSlots => Math.Max(0, MaxSlots - CurrentBookings);
        }

        public class BookingViewModel
        {
            public int Id { get; set; }
            public string CustomerName { get; set; } = "";
            public string CustomerPhone { get; set; } = "";
            public string VehicleName { get; set; } = "";
            public string Status { get; set; } = "";
            public string? Note { get; set; }
        }

        public class VehicleSimple
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }
    }
}
