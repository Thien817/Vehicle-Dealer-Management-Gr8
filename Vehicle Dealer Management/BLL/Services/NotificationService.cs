using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Data;
using Vehicle_Dealer_Management.DAL.IRepository;
using Vehicle_Dealer_Management.DAL.Models;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.BLL.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ApplicationDbContext _context;

        public NotificationService(INotificationRepository notificationRepository, ApplicationDbContext context)
        {
            _notificationRepository = notificationRepository;
            _context = context;
        }

        public async Task<Notification> CreateNotificationAsync(
            int userId,
            string title,
            string? content = null,
            string type = "INFO",
            string? linkUrl = null,
            int? relatedEntityId = null,
            string? relatedEntityType = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Content = content,
                Type = type,
                LinkUrl = linkUrl,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            return await _notificationRepository.AddAsync(notification);
        }

        public async Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(int userId)
        {
            return await _notificationRepository.GetNotificationsByUserIdAsync(userId);
        }

        public async Task<IEnumerable<Notification>> GetUnreadNotificationsByUserIdAsync(int userId)
        {
            return await _notificationRepository.GetUnreadNotificationsByUserIdAsync(userId);
        }

        public async Task<int> GetUnreadCountByUserIdAsync(int userId)
        {
            return await _notificationRepository.GetUnreadCountByUserIdAsync(userId);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            await _notificationRepository.MarkAsReadAsync(notificationId);
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            await _notificationRepository.MarkAllAsReadAsync(userId);
        }

        public async Task CreatePromotionNotificationAsync(int vehicleId, string vehicleName, decimal discountPercent)
        {
            // Get all customer users
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "CUSTOMER");
            if (customerRole == null) return;

            var customers = await _context.Users
                .Where(u => u.RoleId == customerRole.Id)
                .ToListAsync();

            // Create notification for each customer
            foreach (var customer in customers)
            {
                await CreateNotificationAsync(
                    userId: customer.Id,
                    title: $"🎉 Khuyến mãi {discountPercent}% cho {vehicleName}!",
                    content: $"Xe {vehicleName} đang được giảm giá {discountPercent}%. Hãy xem ngay!",
                    type: "PROMOTION",
                    linkUrl: $"/Customer/VehicleDetail?id={vehicleId}",
                    relatedEntityId: vehicleId,
                    relatedEntityType: "Vehicle");
            }
        }
    }
}

