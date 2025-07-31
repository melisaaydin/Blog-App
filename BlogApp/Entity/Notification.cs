using System;

namespace BlogApp.Entity
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string LinkUrl { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;
    }
}