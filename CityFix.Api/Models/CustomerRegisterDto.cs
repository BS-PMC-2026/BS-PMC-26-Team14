using System.ComponentModel.DataAnnotations;

public class CustomerRegisterDto
{
    [Required]
public string Phone { get; set; } = string.Empty;
    [Required]
    public string FullName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Address { get; set; }

    [Required]
    [MinLength(6)]
    public string Password { get; set; }
}