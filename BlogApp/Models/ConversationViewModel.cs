using BlogApp.Entity;

namespace BlogApp.Models
{
    public class ConversationViewModel
    {
        public Message? LastMessage { get; set; }
        public User? OtherUser { get; set; }
    }
}