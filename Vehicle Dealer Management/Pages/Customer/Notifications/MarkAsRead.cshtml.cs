using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.Pages.Customer.Notifications
{
    [IgnoreAntiforgeryToken]
    public class MarkAsReadModel : PageModel
    {
        private readonly INotificationService _notificationService;

        public MarkAsReadModel(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest();
            }

            await _notificationService.MarkAsReadAsync(id);
            return new JsonResult(new { success = true });
        }
    }
}

