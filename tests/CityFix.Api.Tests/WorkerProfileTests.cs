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
    public class WorkerProfileTests
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
        public async Task GetWorkerProfile_ShouldReturnBadRequest_WhenEmailIsEmpty()
        {
            using var context = CreateContext(nameof(GetWorkerProfile_ShouldReturnBadRequest_WhenEmailIsEmpty));
            var controller = CreateController(context);

            var result = await controller.GetWorkerProfile("");

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetWorkerProfile_ShouldReturnNotFound_WhenWorkerDoesNotExist()
        {
            using var context = CreateContext(nameof(GetWorkerProfile_ShouldReturnNotFound_WhenWorkerDoesNotExist));
            var controller = CreateController(context);

            var result = await controller.GetWorkerProfile("missing@test.com");

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetWorkerProfile_ShouldReturnOk_WhenWorkerExists()
        {
            using var context = CreateContext(nameof(GetWorkerProfile_ShouldReturnOk_WhenWorkerExists));

            context.Workers.Add(new Worker
            {
                FullName = "Ahmad Worker",
                NationalId = "123456789",
                Phone = "0529030244",
                Email = "worker@test.com",
                Municipality = "באר שבע",
                Department = "כבישים",
                PasswordHash = HashPasswordForTest("123456"),
                ApprovalStatus = "Approved"
            });

            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result = await controller.GetWorkerProfile("worker@test.com");

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
            okResult.Value.Should().BeEquivalentTo(new
            {
                fullName = "Ahmad Worker",
                email = "worker@test.com",
                phone = "0529030244",
                municipality = "באר שבע",
                department = "כבישים"
            });
        }

        [Fact]
        public async Task UpdateWorkerProfile_ShouldReturnNotFound_WhenWorkerDoesNotExist()
        {
            using var context = CreateContext(nameof(UpdateWorkerProfile_ShouldReturnNotFound_WhenWorkerDoesNotExist));
            var controller = CreateController(context);

            var dto = new AuthController.UpdateWorkerProfileDto
            {
                Email = "missing@test.com",
                FullName = "New Name",
                Phone = "0500000000",
                Municipality = "תל אביב-יפו",
                Department = "תאורה"
            };

            var result = await controller.UpdateWorkerProfile(dto);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateWorkerProfile_ShouldUpdateBasicFields_WhenNoPasswordChangeRequested()
        {
            using var context = CreateContext(nameof(UpdateWorkerProfile_ShouldUpdateBasicFields_WhenNoPasswordChangeRequested));

            var worker = new Worker
            {
                FullName = "Old Worker",
                NationalId = "123456789",
                Phone = "1111111",
                Email = "worker@test.com",
                Municipality = "באר שבע",
                Department = "כבישים",
                PasswordHash = HashPasswordForTest("oldpass"),
                ApprovalStatus = "Approved"
            };

            context.Workers.Add(worker);
            await context.SaveChangesAsync();

            var oldHash = worker.PasswordHash;
            var controller = CreateController(context);

            var dto = new AuthController.UpdateWorkerProfileDto
            {
                Email = "worker@test.com",
                FullName = "New Worker",
                Phone = "2222222",
                Municipality = "תל אביב-יפו",
                Department = "תאורה",
                CurrentPassword = "",
                NewPassword = ""
            };

            var result = await controller.UpdateWorkerProfile(dto);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();

            worker.FullName.Should().Be("New Worker");
            worker.Phone.Should().Be("2222222");
            worker.Municipality.Should().Be("תל אביב-יפו");
            worker.Department.Should().Be("תאורה");
            worker.PasswordHash.Should().Be(oldHash);

            okResult.Value.Should().BeEquivalentTo(new
            {
                message = "פרופיל העובד עודכן בהצלחה",
                role = "Worker",
                fullName = "New Worker",
                email = "worker@test.com",
                phone = "2222222",
                municipality = "תל אביב-יפו",
                department = "תאורה"
            });
        }

        [Fact]
        public async Task UpdateWorkerProfile_ShouldReturnBadRequest_WhenNewPasswordProvidedButCurrentPasswordMissing()
        {
            using var context = CreateContext(nameof(UpdateWorkerProfile_ShouldReturnBadRequest_WhenNewPasswordProvidedButCurrentPasswordMissing));

            context.Workers.Add(new Worker
            {
                FullName = "Ahmad Worker",
                NationalId = "123456789",
                Phone = "050",
                Email = "worker@test.com",
                Municipality = "באר שבע",
                Department = "כבישים",
                PasswordHash = HashPasswordForTest("oldpass"),
                ApprovalStatus = "Approved"
            });

            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var dto = new AuthController.UpdateWorkerProfileDto
            {
                Email = "worker@test.com",
                FullName = "Ahmad Worker",
                Phone = "050",
                Municipality = "באר שבע",
                Department = "כבישים",
                CurrentPassword = "",
                NewPassword = "newpass123"
            };

            var result = await controller.UpdateWorkerProfile(dto);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateWorkerProfile_ShouldReturnBadRequest_WhenCurrentPasswordIsWrong()
        {
            using var context = CreateContext(nameof(UpdateWorkerProfile_ShouldReturnBadRequest_WhenCurrentPasswordIsWrong));

            var worker = new Worker
            {
                FullName = "Ahmad Worker",
                NationalId = "123456789",
                Phone = "050",
                Email = "worker@test.com",
                Municipality = "באר שבע",
                Department = "כבישים",
                PasswordHash = HashPasswordForTest("correct-password"),
                ApprovalStatus = "Approved"
            };

            context.Workers.Add(worker);
            await context.SaveChangesAsync();

            var oldHash = worker.PasswordHash;
            var controller = CreateController(context);

            var dto = new AuthController.UpdateWorkerProfileDto
            {
                Email = "worker@test.com",
                FullName = "Updated Worker",
                Phone = "051",
                Municipality = "ירושלים",
                Department = "תברואה",
                CurrentPassword = "wrong-password",
                NewPassword = "newpass123"
            };

            var result = await controller.UpdateWorkerProfile(dto);

            result.Should().BeOfType<BadRequestObjectResult>();
            worker.PasswordHash.Should().Be(oldHash);
        }

        [Fact]
        public async Task UpdateWorkerProfile_ShouldUpdatePassword_WhenCurrentPasswordIsCorrect()
        {
            using var context = CreateContext(nameof(UpdateWorkerProfile_ShouldUpdatePassword_WhenCurrentPasswordIsCorrect));

            var worker = new Worker
            {
                FullName = "Ahmad Worker",
                NationalId = "123456789",
                Phone = "050",
                Email = "worker@test.com",
                Municipality = "באר שבע",
                Department = "כבישים",
                PasswordHash = HashPasswordForTest("oldpass123"),
                ApprovalStatus = "Approved"
            };

            context.Workers.Add(worker);
            await context.SaveChangesAsync();

            var oldHash = worker.PasswordHash;
            var controller = CreateController(context);

            var dto = new AuthController.UpdateWorkerProfileDto
            {
                Email = "worker@test.com",
                FullName = "Ahmad Updated",
                Phone = "0529030244",
                Municipality = "תל אביב-יפו",
                Department = "פיקוח עירוני",
                CurrentPassword = "oldpass123",
                NewPassword = "newpass123"
            };

            var result = await controller.UpdateWorkerProfile(dto);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();

            worker.FullName.Should().Be("Ahmad Updated");
            worker.Phone.Should().Be("0529030244");
            worker.Municipality.Should().Be("תל אביב-יפו");
            worker.Department.Should().Be("פיקוח עירוני");
            worker.PasswordHash.Should().NotBe(oldHash);
            worker.PasswordHash.Should().Be(HashPasswordForTest("newpass123"));
        }

        [Fact]
        public async Task UpdateWorkerProfile_ShouldAllowUpdatingOnlyProfileFields_WhenPasswordsAreWhitespace()
        {
            using var context = CreateContext(nameof(UpdateWorkerProfile_ShouldAllowUpdatingOnlyProfileFields_WhenPasswordsAreWhitespace));

            var worker = new Worker
            {
                FullName = "Old Worker",
                NationalId = "123456789",
                Phone = "000",
                Email = "worker@test.com",
                Municipality = "באר שבע",
                Department = "כבישים",
                PasswordHash = HashPasswordForTest("samepass"),
                ApprovalStatus = "Approved"
            };

            context.Workers.Add(worker);
            await context.SaveChangesAsync();

            var oldHash = worker.PasswordHash;
            var controller = CreateController(context);

            var dto = new AuthController.UpdateWorkerProfileDto
            {
                Email = "worker@test.com",
                FullName = "New Worker",
                Phone = "1234567",
                Municipality = "חיפה",
                Department = "תחזוקה כללית",
                CurrentPassword = "   ",
                NewPassword = "   "
            };

            var result = await controller.UpdateWorkerProfile(dto);

            result.Should().BeOfType<OkObjectResult>();
            worker.FullName.Should().Be("New Worker");
            worker.Phone.Should().Be("1234567");
            worker.Municipality.Should().Be("חיפה");
            worker.Department.Should().Be("תחזוקה כללית");
            worker.PasswordHash.Should().Be(oldHash);
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