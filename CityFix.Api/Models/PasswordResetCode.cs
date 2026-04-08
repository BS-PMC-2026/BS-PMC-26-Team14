using System.ComponentModel.DataAnnotations;

namespace CityFix.Api.Models
{
    public class PasswordResetCode
    {
        public int Id { get; set; }

        [MaxLength(20)]
        public string UserType { get; set; } = "";

        public int UserId { get; set; }

        [MaxLength(200)]
        public string CodeHash { get; set; } = "";

        public DateTime ExpiresAt { get; set; }

        public int FailedAttempts { get; set; }

        public bool IsUsed { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UsedAt { get; set; }
    }
}

