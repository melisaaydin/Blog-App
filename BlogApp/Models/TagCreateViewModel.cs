using System.ComponentModel.DataAnnotations;

namespace BlogApp.Models
{
    public class TagCreateViewModel
    {
        [Required(ErrorMessage = "Tag name is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tag name must be between 3 and 50 characters.")]
        public string Text { get; set; } = null!;
    }
}