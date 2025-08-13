using System.Collections.Generic;
using BlogApp.Entity;

namespace BlogApp.Models
{
    public class ChatViewModel
    {
        public User? OtherUser { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();
        public string? NewMessageContent { get; set; }
    }
}