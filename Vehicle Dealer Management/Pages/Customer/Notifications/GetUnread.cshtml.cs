using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.Pages.Customer.Notifications
{
    public class GetUnreadModel : PageModel
    {
        private readonly INotificationService _notificationService;

        public GetUnreadModel(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return new JsonResult(new { count = 0, notifications = new List<object>() });
            }

            var userIdInt = int.Parse(userId);
            var notifications = await _notificationService.GetUnreadNotificationsByUserIdAsync(userIdInt);
            var count = await _notificationService.GetUnreadCountByUserIdAsync(userIdInt);

            var result = notifications.Select(n => new
            {
                id = n.Id,
                title = n.Title,
                content = n.Content,
                type = n.Type,
                linkUrl = n.LinkUrl,
                isRead = n.IsRead,
                createdAt = n.CreatedAt
            }).ToList();

            return new JsonResult(new { count, notifications = result });
        }
    }
}

