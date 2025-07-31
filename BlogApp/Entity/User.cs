using Microsoft.AspNetCore.Identity;

namespace BlogApp.Entity
{
    public class User : IdentityUser
    {
        public string? Name { get; set; }
        public string? Image { get; set; }
        public List<Post> Posts { get; set; } = new List<Post>();
        public List<Comment> Comments { get; set; } = new List<Comment>();
        public List<Follow> Followers { get; set; } = new();
        public List<Follow> Following { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
    }
}