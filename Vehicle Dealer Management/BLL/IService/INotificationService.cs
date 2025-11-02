using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.BLL.IService
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(
            int userId,
            string title,
            string? content = null,
            string type = "INFO",
            string? linkUrl = null,
            int? relatedEntityId = null,
            string? relatedEntityType = null);

        Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(int userId);
        Task<IEnumerable<Notification>> GetUnreadNotificationsByUserIdAsync(int userId);
        Task<int> GetUnreadCountByUserIdAsync(int userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int userId);
        
        // Helper method to create promotion notification for all customers
        Task CreatePromotionNotificationAsync(int vehicleId, string vehicleName, decimal discountPercent);
    }
}

