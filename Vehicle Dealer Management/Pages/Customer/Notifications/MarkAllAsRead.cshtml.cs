using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.Pages.Customer.Notifications
{
    [IgnoreAntiforgeryToken]
    public class MarkAllAsReadModel : PageModel
    {
        private readonly INotificationService _notificationService;

        public MarkAllAsReadModel(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest();
            }

            var userIdInt = int.Parse(userId);
            await _notificationService.MarkAllAsReadAsync(userIdInt);
            return new JsonResult(new { success = true });
        }
    }
}

