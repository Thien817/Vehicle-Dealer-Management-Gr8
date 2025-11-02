using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Dealer_Management.DAL.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // User sẽ nhận notification (Customer)

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty; // Tiêu đề thông báo

        [Column(TypeName = "nvarchar(max)")]
        public string? Content { get; set; } // Nội dung thông báo

        [StringLength(50)]
        public string Type { get; set; } = "INFO"; // INFO, PROMOTION, ORDER, QUOTE, etc.

        [StringLength(200)]
        public string? LinkUrl { get; set; } // URL để điều hướng khi click (ví dụ: /Customer/Vehicles/Detail?id=1)

        public int? RelatedEntityId { get; set; } // ID của entity liên quan (VehicleId, QuoteId, etc.)

        [StringLength(50)]
        public string? RelatedEntityType { get; set; } // Vehicle, Quote, Order, etc.

        [Required]
        public bool IsRead { get; set; } = false; // Đã đọc chưa

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; } // Thời điểm đọc

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}

