using System.ComponentModel.DataAnnotations;
using CityFix.Api.Models;

namespace CityFix.Api.Tests;

public class ForgotPasswordTests
{
    // Helper: runs DataAnnotations validation on an object and returns the errors
    private static IList<ValidationResult> Validate(object obj)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(obj);
        Validator.TryValidateObject(obj, ctx, results, validateAllProperties: true);
        return results;
    }

    // ── ForgotPasswordDto ──────────────────────────────────────────────────────

    [Fact]
    public void ForgotPasswordDto_ValidEmail_PassesValidation()
    {
        var dto = new ForgotPasswordDto { Email = "user@example.com" };
        var errors = Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void ForgotPasswordDto_InvalidEmail_FailsValidation()
    {
        var dto = new ForgotPasswordDto { Email = "not-an-email" };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ForgotPasswordDto_EmptyEmail_FailsValidation()
    {
        var dto = new ForgotPasswordDto { Email = "" };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    // ── ResetPasswordDto ───────────────────────────────────────────────────────

    [Fact]
    public void ResetPasswordDto_ValidInput_PassesValidation()
    {
        var dto = new ResetPasswordDto
        {
            Email = "user@example.com",
            Code = "123456",
            NewPassword = "secret123"
        };
        var errors = Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void ResetPasswordDto_InvalidEmailFormat_FailsValidation()
    {
        var dto = new ResetPasswordDto
        {
            Email = "bad-email",
            Code = "123456",
            NewPassword = "secret123"
        };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ResetPasswordDto_CodeTooShort_FailsValidation()
    {
        var dto = new ResetPasswordDto
        {
            Email = "user@example.com",
            Code = "123",       // must be exactly 6 digits
            NewPassword = "secret123"
        };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ResetPasswordDto_CodeContainsLetters_FailsValidation()
    {
        var dto = new ResetPasswordDto
        {
            Email = "user@example.com",
            Code = "12345a",    // letters not allowed
            NewPassword = "secret123"
        };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ResetPasswordDto_PasswordTooShort_FailsValidation()
    {
        var dto = new ResetPasswordDto
        {
            Email = "user@example.com",
            Code = "123456",
            NewPassword = "abc"  // less than 6 chars
        };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ResetPasswordDto_EmptyCode_FailsValidation()
    {
        var dto = new ResetPasswordDto
        {
            Email = "user@example.com",
            Code = "",
            NewPassword = "secret123"
        };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }
}