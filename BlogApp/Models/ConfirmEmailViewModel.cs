using System.ComponentModel.DataAnnotations;

namespace BlogApp.Models
{
    public class ConfirmEmailViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
    }
}