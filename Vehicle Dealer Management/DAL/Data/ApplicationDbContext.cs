using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.DAL.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Auth & Organization
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Dealer> Dealers { get; set; }

        // Products, Pricing & Distribution
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<PricePolicy> PricePolicies { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<DealerOrder> DealerOrders { get; set; }

        // Sales
        public DbSet<SalesDocument> SalesDocuments { get; set; }
        public DbSet<SalesDocumentLine> SalesDocumentLines { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<Promotion> Promotions { get; set; }

        // Customers & Interactions
        public DbSet<CustomerProfile> CustomerProfiles { get; set; }
        public DbSet<TestDrive> TestDrives { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }

        // Legacy (giữ lại để tương thích)
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Sale> Sales { get; set; }

        // Logging
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        // Notifications
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Role
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
            });

            // Configure User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.RoleId);
                entity.HasIndex(e => e.DealerId);
            });

            // Configure Dealer
            modelBuilder.Entity<Dealer>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
            });

            // Configure Vehicle
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasIndex(e => new { e.ModelName, e.VariantName }).IsUnique();
                entity.HasIndex(e => e.Status);
            });

            // Configure PricePolicy
            modelBuilder.Entity<PricePolicy>(entity =>
            {
                entity.HasIndex(e => new { e.VehicleId, e.DealerId, e.ValidFrom, e.ValidTo });
            });

            // Configure Stock
            modelBuilder.Entity<Stock>(entity =>
            {
                entity.HasIndex(e => new { e.OwnerType, e.OwnerId, e.VehicleId, e.ColorCode }).IsUnique();
                entity.ToTable(t => t.HasCheckConstraint("CK_Stock_Qty", "[Qty] >= 0"));
            });

            // Configure DealerOrder
            modelBuilder.Entity<DealerOrder>(entity =>
            {
                entity.HasIndex(e => new { e.DealerId, e.Status, e.CreatedAt });
            });

            // Configure SalesDocument
            modelBuilder.Entity<SalesDocument>(entity =>
            {
                entity.HasIndex(e => new { e.Type, e.DealerId, e.Status, e.CreatedAt });
                entity.HasIndex(e => new { e.CustomerId, e.CreatedAt });
            });

            // Configure SalesDocumentLine
            modelBuilder.Entity<SalesDocumentLine>(entity =>
            {
                entity.HasIndex(e => new { e.SalesDocumentId, e.VehicleId });
                entity.ToTable(t => 
                {
                    t.HasCheckConstraint("CK_SalesDocumentLine_Discount", "[DiscountValue] <= [UnitPrice] * [Qty]");
                    t.HasCheckConstraint("CK_SalesDocumentLine_Qty", "[Qty] > 0");
                });
            });

            // Configure Payment
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasIndex(e => new { e.SalesDocumentId, e.PaidAt });
                entity.ToTable(t => t.HasCheckConstraint("CK_Payment_Amount", "[Amount] > 0"));
            });

            // Configure Delivery
            modelBuilder.Entity<Delivery>(entity =>
            {
                entity.HasIndex(e => new { e.SalesDocumentId, e.ScheduledDate });
            });

            // Configure Promotion
            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.HasIndex(e => new { e.Scope, e.DealerId, e.VehicleId, e.ValidFrom, e.ValidTo });
            });

            // Configure CustomerProfile
            modelBuilder.Entity<CustomerProfile>(entity =>
            {
                entity.HasIndex(e => e.Phone).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
                entity.HasIndex(e => e.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
            });

            // Configure TestDrive
            modelBuilder.Entity<TestDrive>(entity =>
            {
                entity.HasIndex(e => new { e.DealerId, e.ScheduleTime });
                entity.HasIndex(e => new { e.CustomerId, e.Status });
            });

            // Configure Feedback
            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.HasIndex(e => new { e.DealerId, e.Status });
                entity.HasIndex(e => new { e.CustomerId, e.CreatedAt });
                entity.HasIndex(e => new { e.Type, e.OrderId }); // Index cho REVIEW type
                entity.HasIndex(e => new { e.DealerId, e.Type, e.Rating }); // Index cho rating queries
            });

            // Configure relationships
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasOne(u => u.Role)
                      .WithMany()
                      .HasForeignKey(u => u.RoleId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(u => u.Dealer)
                      .WithMany()
                      .HasForeignKey(u => u.DealerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PricePolicy>(entity =>
            {
                entity.HasOne(p => p.Vehicle)
                      .WithMany()
                      .HasForeignKey(p => p.VehicleId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Dealer)
                      .WithMany()
                      .HasForeignKey(p => p.DealerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Stock>(entity =>
            {
                entity.HasOne(s => s.Vehicle)
                      .WithMany()
                      .HasForeignKey(s => s.VehicleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SalesDocument>(entity =>
            {
                entity.HasOne(s => s.Customer)
                      .WithMany()
                      .HasForeignKey(s => s.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Dealer)
                      .WithMany()
                      .HasForeignKey(s => s.DealerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SalesDocumentLine>(entity =>
            {
                entity.HasOne(l => l.Vehicle)
                      .WithMany()
                      .HasForeignKey(l => l.VehicleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
