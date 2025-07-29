using System.ComponentModel.DataAnnotations;

namespace BlogApp.Models
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "User Name")]
        public string? UserName { get; set; }

        [Required]
        [Display(Name = "Name Surname")]
        public string? Name { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string? Email { get; set; }

        [Required]
        [StringLength(10, ErrorMessage = "Please specify a maximum of 10 and minimum {2} characters for the {0}.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Required]
        [StringLength(10, ErrorMessage = "Please specify a maximum of 10 and minimum {2} characters for the {0}.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Paswords do not match.")]
        [Display(Name = "Password Repeat")]
        public string? ConfirmPassword { get; set; }

    }
}