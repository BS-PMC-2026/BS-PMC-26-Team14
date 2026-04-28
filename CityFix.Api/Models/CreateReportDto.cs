using System.ComponentModel.DataAnnotations;

namespace CityFix.Api.Models
{
    public class CreateReportDto
    {
        [Required]
        public string CustomerEmail { get; set; } = "";

        [Required]
        public string Category { get; set; } = "";

        [Required]
        public string Priority { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        public string Notes { get; set; } = "";

        public string ImageBase64 { get; set; } = "";

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }
    }
}