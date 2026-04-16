using System.Security.Cryptography;
using System.Text;
using CityFix.Api.Controllers;
using CityFix.Api.Data;
using CityFix.Api.Models;
using CityFix.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CityFix.Api.Tests
{
    public class CustomerProfileTests
    {
        private ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private AuthController CreateController(ApplicationDbContext context)
        {
            return new AuthController(
                context,
                new FakeEmailSender(),
                NullLogger<AuthController>.Instance
            );
        }

        private static string HashPasswordForTest(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        [Fact]
        public async Task GetCustomerProfile_ShouldReturnBadRequest_WhenEmailIsEmpty()
        {
            using var context = CreateContext(nameof(GetCustomerProfile_ShouldReturnBadRequest_WhenEmailIsEmpty));
            var controller = CreateController(context);

            var result = await controller.GetCustomerProfile("");

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetCustomerProfile_ShouldReturnNotFound_WhenCustomerDoesNotExist()
        {
            using var context = CreateContext(nameof(GetCustomerProfile_ShouldReturnNotFound_WhenCustomerDoesNotExist));
            var controller = CreateController(context);

            var result = await controller.GetCustomerProfile("missing@test.com");

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetCustomerProfile_ShouldReturnOk_WhenCustomerExists()
        {
            using var context = CreateContext(nameof(GetCustomerProfile_ShouldReturnOk_WhenCustomerExists));

            context.Customers.Add(new Customer
            {
                FullName = "Ahmad Akhras",
                Phone = "0529030244",
                Email = "ahmad@test.com",
                Address = "Beer Sheva",
                PasswordHash = HashPasswordForTest("123456")
            });

            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result = await controller.GetCustomerProfile("ahmad@test.com");

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();

            var value = okResult.Value!;
            value.Should().BeEquivalentTo(new
            {
                fullName = "Ahmad Akhras",
                email = "ahmad@test.com",
                phone = "0529030244",
                address = "Beer Sheva"
            });
        }

        [Fact]
        public async Task UpdateCustomerProfile_ShouldReturnNotFound_WhenCustomerDoesNotExist()
        {
            using var context = CreateContext(nameof(UpdateCustomerProfile_ShouldReturnNotFound_WhenCustomerDoesNotExist));
            var controller = CreateController(context);

            var dto = new AuthController.UpdateCustomerProfileDto
            {
                Email = "missing@test.com",
                FullName = "New Name",
                Phone = "0500000000",
                Address = "New Address"
            };

            var result = await controller.UpdateCustomerProfile(dto);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateCustomerProfile_ShouldUpdateBasicFields_WhenNoPasswordChangeRequested()
        {
            using var context = CreateContext(nameof(UpdateCustomerProfile_ShouldUpdateBasicFields_WhenNoPasswordChangeRequested));

            var customer = new Customer
            {
                FullName = "Old Name",
                Phone = "1111111",
                Email = "customer@test.com",
                Address = "Old Address",
                PasswordHash = HashPasswordForTest("oldpass")
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var oldHash = customer.PasswordHash;
            var controller = CreateController(context);

            var dto = new AuthController.UpdateCustomerProfileDto
            {
                Email = "customer@test.com",
                FullName = "New Name",
                Phone = "2222222",
                Address = "New Address",
                CurrentPassword = "",
                NewPassword = ""
            };

            var result = await controller.UpdateCustomerProfile(dto);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();

            customer.FullName.Should().Be("New Name");
            customer.Phone.Should().Be("2222222");
            customer.Address.Should().Be("New Address");
            customer.PasswordHash.Should().Be(oldHash);

            okResult.Value.Should().BeEquivalentTo(new
            {
                message = "הפרופיל עודכן בהצלחה",
                role = "Customer",
                fullName = "New Name",
                email = "customer@test.com",
                phone = "2222222",
                address = "New Address"
            });
        }

        [Fact]
        public async Task UpdateCustomerProfile_ShouldReturnBadRequest_WhenNewPasswordProvidedButCurrentPasswordMissing()
        {
            using var context = CreateContext(nameof(UpdateCustomerProfile_ShouldReturnBadRequest_WhenNewPasswordProvidedButCurrentPasswordMissing));

            context.Customers.Add(new Customer
            {
                FullName = "Ahmad",
                Phone = "050",
                Email = "customer@test.com",
                Address = "Beer Sheva",
                PasswordHash = HashPasswordForTest("oldpass")
            });

            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var dto = new AuthController.UpdateCustomerProfileDto
            {
                Email = "customer@test.com",
                FullName = "Ahmad",
                Phone = "050",
                Address = "Beer Sheva",
                CurrentPassword = "",
                NewPassword = "newpass123"
            };

            var result = await controller.UpdateCustomerProfile(dto);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateCustomerProfile_ShouldReturnBadRequest_WhenCurrentPasswordIsWrong()
        {
            using var context = CreateContext(nameof(UpdateCustomerProfile_ShouldReturnBadRequest_WhenCurrentPasswordIsWrong));

            var customer = new Customer
            {
                FullName = "Ahmad",
                Phone = "050",
                Email = "customer@test.com",
                Address = "Beer Sheva",
                PasswordHash = HashPasswordForTest("correct-password")
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var oldHash = customer.PasswordHash;
            var controller = CreateController(context);

            var dto = new AuthController.UpdateCustomerProfileDto
            {
                Email = "customer@test.com",
                FullName = "Updated Ahmad",
                Phone = "051",
                Address = "Tel Aviv",
                CurrentPassword = "wrong-password",
                NewPassword = "newpass123"
            };

            var result = await controller.UpdateCustomerProfile(dto);

            result.Should().BeOfType<BadRequestObjectResult>();
            customer.PasswordHash.Should().Be(oldHash);
        }

        [Fact]
        public async Task UpdateCustomerProfile_ShouldUpdatePassword_WhenCurrentPasswordIsCorrect()
        {
            using var context = CreateContext(nameof(UpdateCustomerProfile_ShouldUpdatePassword_WhenCurrentPasswordIsCorrect));

            var customer = new Customer
            {
                FullName = "Ahmad",
                Phone = "050",
                Email = "customer@test.com",
                Address = "Beer Sheva",
                PasswordHash = HashPasswordForTest("oldpass123")
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var oldHash = customer.PasswordHash;
            var controller = CreateController(context);

            var dto = new AuthController.UpdateCustomerProfileDto
            {
                Email = "customer@test.com",
                FullName = "Ahmad Updated",
                Phone = "0529030244",
                Address = "Tel Aviv",
                CurrentPassword = "oldpass123",
                NewPassword = "newpass123"
            };

            var result = await controller.UpdateCustomerProfile(dto);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();

            customer.FullName.Should().Be("Ahmad Updated");
            customer.Phone.Should().Be("0529030244");
            customer.Address.Should().Be("Tel Aviv");
            customer.PasswordHash.Should().NotBe(oldHash);
            customer.PasswordHash.Should().Be(HashPasswordForTest("newpass123"));
        }

        [Fact]
        public async Task UpdateCustomerProfile_ShouldAllowUpdatingOnlyProfileFields_WhenPasswordsAreWhitespace()
        {
            using var context = CreateContext(nameof(UpdateCustomerProfile_ShouldAllowUpdatingOnlyProfileFields_WhenPasswordsAreWhitespace));

            var customer = new Customer
            {
                FullName = "Old User",
                Phone = "000",
                Email = "customer@test.com",
                Address = "Old City",
                PasswordHash = HashPasswordForTest("samepass")
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var oldHash = customer.PasswordHash;
            var controller = CreateController(context);

            var dto = new AuthController.UpdateCustomerProfileDto
            {
                Email = "customer@test.com",
                FullName = "New User",
                Phone = "1234567",
                Address = "New City",
                CurrentPassword = "   ",
                NewPassword = "   "
            };

            var result = await controller.UpdateCustomerProfile(dto);

            result.Should().BeOfType<OkObjectResult>();
            customer.FullName.Should().Be("New User");
            customer.Phone.Should().Be("1234567");
            customer.Address.Should().Be("New City");
            customer.PasswordHash.Should().Be(oldHash);
        }

        private class FakeEmailSender : IEmailSender
        {
            public Task SendAsync(string to, string subject, string body)
            {
                return Task.CompletedTask;
            }
        }
    }
}