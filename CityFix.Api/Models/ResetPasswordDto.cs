using System.ComponentModel.DataAnnotations;

namespace CityFix.Api.Models
{
    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "Code must be 6 digits")]
        public string Code { get; set; } = "";

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = "";
    }
}

