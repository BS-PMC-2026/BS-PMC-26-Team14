using System.ComponentModel.DataAnnotations;

namespace CityFix.Api.Models
{
    public class UpdateAdminProfileDto
    {
        [Required]
        [EmailAddress]
        public string CurrentEmail { get; set; } = "";

        [Required]
        [MinLength(2)]
        public string Username { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}

