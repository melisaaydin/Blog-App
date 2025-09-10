using BlogApp.Entity;

namespace BlogApp.Models
{
    public class TagsViewModel
    {
        public List<Tag> Tags { get; set; } = new List<Tag>();
        public string Sort { get; set; } = "newest";
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
    }
}