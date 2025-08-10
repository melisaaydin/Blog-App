using System;

namespace BlogApp.Entity
{
    public class Message
    {
        public int MessageId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;

        public string SenderId { get; set; } = string.Empty;
        public User Sender { get; set; } = null!;

        public string ReceiverId { get; set; } = string.Empty;
        public User Receiver { get; set; } = null!;
        public string ConversationId { get; set; } = string.Empty;
    }
}