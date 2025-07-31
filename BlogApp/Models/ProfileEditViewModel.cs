using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BlogApp.Models
{
    public class ProfileEditViewModel
    {
        public string? UserId { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string? UserName { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string? Name { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        public string? Image { get; set; }

        [Display(Name = "Profile Image")]
        public IFormFile? ImageFile { get; set; }
    }
}