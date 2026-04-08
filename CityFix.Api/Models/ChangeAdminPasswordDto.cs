using System.ComponentModel.DataAnnotations;

namespace CityFix.Api.Models
{
    public class ChangeAdminPasswordDto
    {
        [Required]
        [EmailAddress]
        public string CurrentEmail { get; set; } = "";

        [Required]
        public string CurrentPassword { get; set; } = "";

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = "";

        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
        public string ConfirmNewPassword { get; set; } = "";
    }
}

