using System.ComponentModel.DataAnnotations;

namespace BlogApp.Models
{
    public class PostCreateViewModel
    {
        public int PostId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Content is required.")]
        public string? Content { get; set; }

        [Required(ErrorMessage = "Url is required.")]
        public string? Url { get; set; }

        public string? Image { get; set; }

        public bool IsActive { get; set; }

        public string[]? SelectedTagIds { get; set; }
        public string[]? SelectedCollectionIds { get; set; }
    }
}