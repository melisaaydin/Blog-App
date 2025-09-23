using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BlogApp.Data.Concrete.EfCore;

namespace BlogApp.ViewComponents
{
    public class UnreadMessageCountViewComponent : ViewComponent
    {
        private readonly BlogContext _context;

        public UnreadMessageCountViewComponent(BlogContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Content("");
            }

            var userId = UserClaimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Content("");
            }

            var unreadCount = await _context.Messages
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .CountAsync();

            return Content(unreadCount.ToString());
        }
    }
}
