using BlogApp.Entity;

namespace BlogApp.Models
{
    public class CollectionsViewModel
    {
        public List<Collection> Collections { get; set; } = new List<Collection>();
        public string Sort { get; set; } = "newest";
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
    }
}