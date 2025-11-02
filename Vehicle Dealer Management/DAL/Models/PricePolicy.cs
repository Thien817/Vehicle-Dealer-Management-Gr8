using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Dealer_Management.DAL.Models
{
    public class PricePolicy
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VehicleId { get; set; }

        public int? DealerId { get; set; } // NULL = giá chung, có giá trị = giá riêng cho đại lý

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Msrp { get; set; } // Manufacturer's Suggested Retail Price

        [Column(TypeName = "decimal(18,2)")]
        public decimal? WholesalePrice { get; set; } // Giá sỉ cho đại lý

        [Column(TypeName = "nvarchar(max)")]
        public string? DiscountRuleJson { get; set; } // JSON chứa quy tắc giảm giá

        public int? PromotionId { get; set; } // Liên kết với Promotion để tính giá giảm

        [Column(TypeName = "nvarchar(max)")]
        public string? Note { get; set; } // Ghi chú về giá, đặc biệt khi có giảm giá

        [Required]
        public DateTime ValidFrom { get; set; } // Ngày bắt đầu hiệu lực

        public DateTime? ValidTo { get; set; } // Ngày kết thúc hiệu lực

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("VehicleId")]
        public virtual Vehicle? Vehicle { get; set; }

        [ForeignKey("DealerId")]
        public virtual Dealer? Dealer { get; set; }

        [ForeignKey("PromotionId")]
        public virtual Promotion? Promotion { get; set; }
    }
}

