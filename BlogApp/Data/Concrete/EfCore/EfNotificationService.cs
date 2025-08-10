using BlogApp.Data.Abstract;
using BlogApp.Data.Concrete.EfCore;
using BlogApp.Entity;

namespace BlogApp.Data.Concrete
{
    public class EfNotificationService : INotificationService
    {
        private readonly BlogContext _context;

        public EfNotificationService(BlogContext context)
        {
            _context = context;
        }

        public async Task CreateNotificationAsync(string userId, string message, string linkUrl)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(message) || string.IsNullOrEmpty(linkUrl))
            {
                return;
            }

            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                LinkUrl = linkUrl,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }
    }
}