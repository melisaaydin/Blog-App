namespace BlogApp.Models
{
    public class ConversationViewModel
    {
        public Entity.Message LastMessage { get; set; }
        public Entity.User OtherUser { get; set; }
    }
}