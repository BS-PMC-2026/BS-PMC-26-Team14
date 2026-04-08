using System.ComponentModel.DataAnnotations;

namespace CityFix.Api.Models
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}

