using Vehicle_Dealer_Management.DAL.Models;
using Vehicle_Dealer_Management.DAL.IRepository;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.BLL.Services
{
    public class TestDriveService : ITestDriveService
    {
        private readonly ITestDriveRepository _testDriveRepository;

        public TestDriveService(ITestDriveRepository testDriveRepository)
        {
            _testDriveRepository = testDriveRepository;
        }

        public async Task<IEnumerable<TestDrive>> GetTestDrivesByCustomerIdAsync(int customerId)
        {
            return await _testDriveRepository.GetTestDrivesByCustomerIdAsync(customerId);
        }

        public async Task<IEnumerable<TestDrive>> GetTestDrivesByDealerIdAsync(int dealerId)
        {
            return await _testDriveRepository.GetTestDrivesByDealerIdAsync(dealerId);
        }

        public async Task<IEnumerable<TestDrive>> GetTestDrivesByDealerAndDateAsync(int dealerId, DateTime date)
        {
            return await _testDriveRepository.GetTestDrivesByDealerAndDateAsync(dealerId, date);
        }

        public async Task<IEnumerable<TestDrive>> GetTestDrivesByStatusAsync(string status, int? dealerId = null)
        {
            return await _testDriveRepository.GetTestDrivesByStatusAsync(status, dealerId);
        }

        public async Task<TestDrive?> GetTestDriveByIdAsync(int id)
        {
            return await _testDriveRepository.GetByIdAsync(id);
        }

        public async Task<TestDrive> CreateTestDriveAsync(TestDrive testDrive)
        {
            if (testDrive == null)
            {
                throw new ArgumentNullException(nameof(testDrive));
            }

            // Business logic: Validate test drive
            if (!testDrive.IsSlot && testDrive.ScheduleTime < DateTime.UtcNow)
            {
                throw new ArgumentException("Schedule time must be in the future", nameof(testDrive));
            }

            if (string.IsNullOrWhiteSpace(testDrive.Status))
            {
                testDrive.Status = "REQUESTED";
            }

            testDrive.CreatedAt = DateTime.UtcNow;

            return await _testDriveRepository.AddAsync(testDrive);
        }

        public async Task UpdateTestDriveAsync(TestDrive testDrive)
        {
            if (testDrive == null)
            {
                throw new ArgumentNullException(nameof(testDrive));
            }

            // Business logic: Validate test drive
            if (!testDrive.IsSlot && testDrive.ScheduleTime < DateTime.UtcNow)
            {
                throw new ArgumentException("Schedule time must be in the future", nameof(testDrive));
            }

            testDrive.UpdatedAt = DateTime.UtcNow;

            await _testDriveRepository.UpdateAsync(testDrive);
        }

        public async Task UpdateTestDriveStatusAsync(int id, string status)
        {
            var testDrive = await _testDriveRepository.GetByIdAsync(id);
            if (testDrive == null)
            {
                throw new KeyNotFoundException($"TestDrive with ID {id} not found");
            }

            testDrive.Status = status;
            testDrive.UpdatedAt = DateTime.UtcNow;

            await _testDriveRepository.UpdateAsync(testDrive);
        }

        public async Task<bool> TestDriveExistsAsync(int id)
        {
            return await _testDriveRepository.ExistsAsync(id);
        }

        // ============ NEW: Slot Management Methods ============

        public async Task<IEnumerable<TestDrive>> GetSlotsByDealerAndDateAsync(int dealerId, DateTime date)
        {
            return await _testDriveRepository.GetSlotsByDealerAndDateAsync(dealerId, date);
        }

        public async Task<IEnumerable<TestDrive>> GetAvailableSlotsByDealerAndDateAsync(int dealerId, DateTime date)
        {
            return await _testDriveRepository.GetAvailableSlotsByDealerAndDateAsync(dealerId, date);
        }

        public async Task<TestDrive?> GetSlotByIdAsync(int slotId)
        {
            return await _testDriveRepository.GetSlotByIdAsync(slotId);
        }

        public async Task<TestDrive> CreateSlotAsync(TestDrive slot)
        {
            if (slot == null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            // Validate slot
            if (!slot.IsSlot)
            {
                throw new ArgumentException("This is not a slot", nameof(slot));
            }

            if (string.IsNullOrEmpty(slot.SlotStartTime) || string.IsNullOrEmpty(slot.SlotEndTime))
            {
                throw new ArgumentException("Slot must have start and end time", nameof(slot));
            }

            if (!slot.MaxSlots.HasValue || slot.MaxSlots.Value <= 0)
            {
                throw new ArgumentException("Slot must have valid MaxSlots", nameof(slot));
            }

            slot.Status = "AVAILABLE";
            slot.CreatedAt = DateTime.UtcNow;

            return await _testDriveRepository.AddAsync(slot);
        }

        public async Task DeleteSlotAsync(int slotId)
        {
            var slot = await _testDriveRepository.GetSlotByIdAsync(slotId);
            if (slot == null)
            {
                throw new KeyNotFoundException($"Slot with ID {slotId} not found");
            }

            // Check if slot has bookings
            var bookingCount = await _testDriveRepository.GetSlotBookingCountAsync(slotId);
            if (bookingCount > 0)
            {
                throw new InvalidOperationException("Cannot delete slot with existing bookings");
            }

            await _testDriveRepository.DeleteAsync(slot);
        }

        public async Task<bool> CanBookSlotAsync(int slotId)
        {
            var slot = await _testDriveRepository.GetSlotByIdAsync(slotId);
            if (slot == null || !slot.IsSlot)
            {
                return false;
            }

            // Check if slot is in the future
            if (slot.ScheduleTime < DateTime.UtcNow)
            {
                return false;
            }

            // Check if slot is full
            return !slot.IsFull;
        }

        public async Task<TestDrive> BookSlotAsync(int slotId, int customerId, int vehicleId, string? note = null)
        {
            // Validate slot
            if (!await CanBookSlotAsync(slotId))
            {
                throw new InvalidOperationException("Slot is not available for booking");
            }

            var slot = await _testDriveRepository.GetSlotByIdAsync(slotId);
            if (slot == null)
            {
                throw new KeyNotFoundException($"Slot with ID {slotId} not found");
            }

            // Validate vehicle is in available list
            if (!string.IsNullOrEmpty(slot.AvailableVehicleIds))
            {
                var availableVehicleIds = slot.AvailableVehicleIds.Split(',').Select(int.Parse).ToList();
                if (!availableVehicleIds.Contains(vehicleId))
                {
                    throw new ArgumentException("Vehicle is not available in this slot");
                }
            }

            // Create booking
            var booking = new TestDrive
            {
                DealerId = slot.DealerId,
                CustomerId = customerId,
                VehicleId = vehicleId,
                ScheduleTime = slot.ScheduleTime, // Use slot's schedule time
                ParentSlotId = slotId,
                Status = "REQUESTED",
                Note = note,
                IsSlot = false,
                CreatedAt = DateTime.UtcNow
            };

            return await _testDriveRepository.AddAsync(booking);
        }
    }
}

