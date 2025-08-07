using System.Collections.Generic;
using BlogApp.Entity;

namespace BlogApp.Models
{
    public class InboxViewModel
    {
        public List<ConversationViewModel> Conversations { get; set; } = new();

        public List<User> Contacts { get; set; } = new();
    }
}