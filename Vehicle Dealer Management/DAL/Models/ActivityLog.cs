using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Dealer_Management.DAL.Models
{
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, LOGIN, REGISTER, etc.

        [Required]
        [StringLength(50)]
        public string EntityType { get; set; } = string.Empty; // Vehicle, Dealer, Customer, Quote, Order, etc.

        public int? EntityId { get; set; } // ID of the entity (if applicable)

        [StringLength(200)]
        public string? EntityName { get; set; } // Name/description of the entity

        [StringLength(1000)]
        public string? Description { get; set; } // Additional details

        [StringLength(50)]
        public string? UserRole { get; set; } // Role of user performing action

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? IpAddress { get; set; } // IP address of user

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}

