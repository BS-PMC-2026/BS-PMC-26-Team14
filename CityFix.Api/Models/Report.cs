using NetTopologySuite.Geometries;

namespace CityFix.Api.Models
{
    public class Report
    {
        public int Id { get; set; }

        public string CustomerEmail { get; set; } = "";

        public string Category { get; set; } = "";
        public string Priority { get; set; } = "";
        public string Description { get; set; } = "";
        public string Notes { get; set; } = "";

        public string Location { get; set; } = "";
        public string ImageBase64 { get; set; } = "";

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string Status { get; set; } = "Open";

        public string? AssignedWorkerEmail { get; set; }
        public DateTime? AcceptedAt { get; set; }

        public string? WorkerImageBase64 { get; set; }
        public string? WorkerImageNote { get; set; }
        public DateTime? WorkerImageUploadedAt { get; set; }

        public Point? LocationPoint { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}