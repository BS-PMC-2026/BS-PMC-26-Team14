namespace CityFix.Api.Models
{
    public class WorkerRegisterDto
    {
        public string FullName { get; set; } = "";
        public string NationalId { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Department { get; set; } = "";
        public string Municipality { get; set; } = "";
        public string Password { get; set; } = "";
    }
}