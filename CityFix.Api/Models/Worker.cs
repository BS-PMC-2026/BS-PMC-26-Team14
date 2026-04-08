namespace CityFix.Api.Models
{
    public class Worker
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string NationalId { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Department { get; set; } = "";
        public string Municipality { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string ApprovalStatus { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}