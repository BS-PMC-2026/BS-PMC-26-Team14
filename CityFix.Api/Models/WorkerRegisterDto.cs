using System.ComponentModel.DataAnnotations;

public class WorkerRegisterDto
{
    [Required]
    public string NationalId { get; set; }

    [Required]
    public string FullName { get; set; }

    [Required]
    public string Phone { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Municipality { get; set; }

    [Required]
    public string Department { get; set; }

    [Required]
    [MinLength(6)]
    public string Password { get; set; }
}