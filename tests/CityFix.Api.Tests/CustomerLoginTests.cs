using System.ComponentModel.DataAnnotations;

namespace CityFix.Api.Tests;

public class CustomerLoginTests
{
    private class CustomerLoginModel
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
    public void CustomerLogin_ValidInput_PassesValidation()
    {
        var model = new CustomerLoginModel
        {
            Email = "customer@test.com",
            Password = "123456"
        };

        var errors = Validate(model);

        Assert.Empty(errors);
    }

    [Fact]
    public void CustomerLogin_InvalidEmail_FailsValidation()
    {
        var model = new CustomerLoginModel
        {
            Email = "invalid-email",
            Password = "123456"
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void CustomerLogin_EmptyEmail_FailsValidation()
    {
        var model = new CustomerLoginModel
        {
            Email = "",
            Password = "123456"
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void CustomerLogin_EmptyPassword_FailsValidation()
    {
        var model = new CustomerLoginModel
        {
            Email = "customer@test.com",
            Password = ""
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void CustomerLogin_NullEmail_FailsValidation()
    {
        var model = new CustomerLoginModel
        {
            Email = null!,
            Password = "123456"
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void CustomerLogin_NullPassword_FailsValidation()
    {
        var model = new CustomerLoginModel
        {
            Email = "customer@test.com",
            Password = null!
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void CustomerLogin_PasswordTooShort_FailsValidation()
    {
        var model = new CustomerLoginModel
        {
            Email = "customer@test.com",
            Password = "123"
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }
}