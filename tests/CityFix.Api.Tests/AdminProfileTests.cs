using System.ComponentModel.DataAnnotations;
using CityFix.Api.Models;

namespace CityFix.Api.Tests;

public class AdminProfileTests
{
    // Helper: runs DataAnnotations validation on an object and returns the errors
    private static IList<ValidationResult> Validate(object obj)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(obj);
        Validator.TryValidateObject(obj, ctx, results, validateAllProperties: true);
        return results;
    }

    // ── UpdateAdminProfileDto ──────────────────────────────────────────────────

    [Fact]
    public void UpdateAdminProfileDto_ValidInput_PassesValidation()
    {
        var dto = new UpdateAdminProfileDto
        {
            CurrentEmail = "admin@example.com",
            Username = "AdminUser",
            Email = "newadmin@example.com"
        };
        var errors = Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void UpdateAdminProfileDto_InvalidCurrentEmail_FailsValidation()
    {
        var dto = new UpdateAdminProfileDto
        {
            CurrentEmail = "not-an-email",
            Username = "AdminUser",
            Email = "newadmin@example.com"
        };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void UpdateAdminProfileDto_InvalidNewEmail_FailsValidation()
    {
        var dto = new UpdateAdminProfileDto
        {
            CurrentEmail = "admin@example.com",
            Username = "AdminUser",
            Email = "bad-email"
        };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void UpdateAdminProfileDto_UsernameTooShort_FailsValidation()
    {
        var dto = new UpdateAdminProfileDto
        {
            CurrentEmail = "admin@example.com",
            Username = "A",   // less than 2 chars
            Email = "newadmin@example.com"
        };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void UpdateAdminProfileDto_EmptyUsername_FailsValidation()
    {
        var dto = new UpdateAdminProfileDto
        {
            CurrentEmail = "admin@example.com",
            Username = "",
            Email = "newadmin@example.com"
        };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    // ── ChangeAdminPasswordDto ─────────────────────────────────────────────────

    [Fact]
    public void ChangeAdminPasswordDto_ValidInput_PassesValidation()
    {
        var dto = new ChangeAdminPasswordDto
        {
            CurrentEmail = "admin@example.com",
            CurrentPassword = "oldpass",
            NewPassword = "newpass123",
            ConfirmNewPassword = "newpass123"
        };
        var errors = Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void ChangeAdminPasswordDto_InvalidEmail_FailsValidation()
    {
        var dto = new ChangeAdminPasswordDto
        {
            CurrentEmail = "not-an-email",
            CurrentPassword = "oldpass",
            NewPassword = "newpass123",
            ConfirmNewPassword = "newpass123"
        };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ChangeAdminPasswordDto_NewPasswordTooShort_FailsValidation()
    {
        var dto = new ChangeAdminPasswordDto
        {
            CurrentEmail = "admin@example.com",
            CurrentPassword = "oldpass",
            NewPassword = "abc",   // less than 6 chars
            ConfirmNewPassword = "abc"
        };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ChangeAdminPasswordDto_PasswordMismatch_FailsValidation()
    {
        var dto = new ChangeAdminPasswordDto
        {
            CurrentEmail = "admin@example.com",
            CurrentPassword = "oldpass",
            NewPassword = "newpass123",
            ConfirmNewPassword = "differentpass"
        };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ChangeAdminPasswordDto_EmptyCurrentPassword_FailsValidation()
    {
        var dto = new ChangeAdminPasswordDto
        {
            CurrentEmail = "admin@example.com",
            CurrentPassword = "",
            NewPassword = "newpass123",
            ConfirmNewPassword = "newpass123"
        };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }
}