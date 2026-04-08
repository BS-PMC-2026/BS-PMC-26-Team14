namespace CityFix.Api.Services
{
    public class EmailSettings
    {
        public bool Enabled { get; set; }
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "CityFix";
    }
}

