using BlogApp.Entity;

namespace BlogApp.Models
{
    public class PostsViewModel
    {
        public List<Post> Posts { get; set; } = new();
        public string? Tag { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}