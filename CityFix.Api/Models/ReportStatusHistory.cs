namespace CityFix.Api.Models
{
    public class ReportStatusHistory
    {
        public int Id { get; set; }

        public int ReportId { get; set; }

        public string OldStatus { get; set; } = "";

        public string NewStatus { get; set; } = "";

        public string ChangedByWorkerEmail { get; set; } = "";

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}