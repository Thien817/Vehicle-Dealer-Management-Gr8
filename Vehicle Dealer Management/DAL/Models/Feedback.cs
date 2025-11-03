using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Dealer_Management.DAL.Models
{
    public class Feedback
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int DealerId { get; set; }

        [Required]
        [StringLength(20)]
        public string Type { get; set; } = "FEEDBACK"; // FEEDBACK, COMPLAINT, hoặc REVIEW

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "NEW"; // NEW, IN_PROGRESS, RESOLVED, REJECTED (không dùng cho REVIEW)

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Content { get; set; } = string.Empty; // Nội dung phản hồi/khiếu nại/đánh giá

        // For REVIEW type only
        public int? Rating { get; set; } // 1-5 sao (chỉ dùng khi Type = REVIEW)
        public int? OrderId { get; set; } // SalesDocument ID (chỉ dùng khi Type = REVIEW, để liên kết với đơn hàng)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        // Navigation properties
        [ForeignKey("CustomerId")]
        public virtual CustomerProfile? Customer { get; set; }

        [ForeignKey("DealerId")]
        public virtual Dealer? Dealer { get; set; }

        [ForeignKey("OrderId")]
        public virtual SalesDocument? Order { get; set; }
    }
}

