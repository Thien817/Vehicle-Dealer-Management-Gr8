using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Dealer_Management.DAL.Models
{
    public class TestDrive
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DealerId { get; set; }

        // Nullable for slot entries (slots don't have customer)
        public int? CustomerId { get; set; }

        // Nullable for booking entries (bookings don't have specific vehicle, they reference slot)
        public int? VehicleId { get; set; }

        [Required]
        public DateTime ScheduleTime { get; set; } // Thời gian hẹn lái thử (≥ now) hoặc ngày của slot

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "REQUESTED"; // REQUESTED, CONFIRMED, DONE, CANCELLED

        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // ============ NEW FIELDS FOR SLOT SYSTEM ============

        /// <summary>
        /// Indicates if this is a slot (true) or a customer booking (false)
        /// </summary>
        public bool IsSlot { get; set; } = false;

        /// <summary>
        /// For slots: start time of the slot (e.g., 07:00)
        /// For bookings: null (uses ScheduleTime from parent slot)
        /// </summary>
        [StringLength(5)]
        public string? SlotStartTime { get; set; }

        /// <summary>
        /// For slots: end time of the slot (e.g., 09:00)
        /// For bookings: null
        /// </summary>
        [StringLength(5)]
        public string? SlotEndTime { get; set; }

        /// <summary>
        /// For slots: maximum number of customers that can book this slot
        /// For bookings: null
        /// </summary>
        public int? MaxSlots { get; set; }

        /// <summary>
        /// For bookings: reference to parent slot ID
        /// For slots: null
        /// </summary>
        public int? ParentSlotId { get; set; }

        /// <summary>
        /// Comma-separated vehicle IDs available in this slot
        /// For slots: "1,2,3" (vehicle IDs)
        /// For bookings: null (VehicleId field is used instead)
        /// </summary>
        [StringLength(500)]
        public string? AvailableVehicleIds { get; set; }

        // Navigation properties
        [ForeignKey("CustomerId")]
        public virtual CustomerProfile? Customer { get; set; }

        [ForeignKey("DealerId")]
        public virtual Dealer? Dealer { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle? Vehicle { get; set; }

        [ForeignKey("ParentSlotId")]
        public virtual TestDrive? ParentSlot { get; set; }

        // Helper properties
        [NotMapped]
        public int CurrentBookings { get; set; }

        [NotMapped]
        public bool IsFull => IsSlot && MaxSlots.HasValue && CurrentBookings >= MaxSlots.Value;
    }
}

