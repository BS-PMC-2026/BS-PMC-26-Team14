using System.ComponentModel.DataAnnotations;

namespace CityFix.Api.Tests;

public class AdminLoginTests
{
    private class AdminLoginModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = "";
    }

    private static IList<ValidationResult> Validate(object obj)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(obj);
        Validator.TryValidateObject(obj, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void AdminLogin_ValidInput_PassesValidation()
    {
        var model = new AdminLoginModel
        {
            Email = "admin@test.com",
            Password = "123456"
        };

        var errors = Validate(model);

        Assert.Empty(errors);
    }

    [Fact]
    public void AdminLogin_InvalidEmail_FailsValidation()
    {
        var model = new AdminLoginModel
        {
            Email = "not-an-email",
            Password = "123456"
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void AdminLogin_EmptyEmail_FailsValidation()
    {
        var model = new AdminLoginModel
        {
            Email = "",
            Password = "123456"
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void AdminLogin_EmptyPassword_FailsValidation()
    {
        var model = new AdminLoginModel
        {
            Email = "admin@test.com",
            Password = ""
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void AdminLogin_NullEmail_FailsValidation()
    {
        var model = new AdminLoginModel
        {
            Email = null!,
            Password = "123456"
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void AdminLogin_NullPassword_FailsValidation()
    {
        var model = new AdminLoginModel
        {
            Email = "admin@test.com",
            Password = null!
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void AdminLogin_PasswordTooShort_FailsValidation()
    {
        var model = new AdminLoginModel
        {
            Email = "admin@test.com",
            Password = "123"
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }
}