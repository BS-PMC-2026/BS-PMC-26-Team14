using System.ComponentModel.DataAnnotations;

namespace CityFix.Api.Tests;

public class WorkerLoginTests
{
    private class WorkerLoginModel
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
    public void WorkerLogin_ValidInput_PassesValidation()
    {
        var model = new WorkerLoginModel
        {
            Email = "worker@test.com",
            Password = "123456"
        };

        var errors = Validate(model);

        Assert.Empty(errors);
    }

    [Fact]
    public void WorkerLogin_InvalidEmail_FailsValidation()
    {
        var model = new WorkerLoginModel
        {
            Email = "bad-email",
            Password = "123456"
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void WorkerLogin_EmptyEmail_FailsValidation()
    {
        var model = new WorkerLoginModel
        {
            Email = "",
            Password = "123456"
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void WorkerLogin_EmptyPassword_FailsValidation()
    {
        var model = new WorkerLoginModel
        {
            Email = "worker@test.com",
            Password = ""
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void WorkerLogin_NullEmail_FailsValidation()
    {
        var model = new WorkerLoginModel
        {
            Email = null!,
            Password = "123456"
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void WorkerLogin_NullPassword_FailsValidation()
    {
        var model = new WorkerLoginModel
        {
            Email = "worker@test.com",
            Password = null!
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void WorkerLogin_PasswordTooShort_FailsValidation()
    {
        var model = new WorkerLoginModel
        {
            Email = "worker@test.com",
            Password = "123"
        };

        var errors = Validate(model);

        Assert.NotEmpty(errors);
    }
}