using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using CityFix.Api.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CityFix.Api.Tests
{
    public class WorkerLoginTests : IClassFixture<WorkerLoginTests.InMemoryFactory>
    {
        public class InMemoryFactory : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
            {
                builder.ConfigureTestServices(services =>
                {
                    var configType = typeof(IDbContextOptionsConfiguration<ApplicationDbContext>);
                    var toRemove = services
                        .Where(d => d.ServiceType == configType
                                 || d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                                 || d.ServiceType == typeof(ApplicationDbContext))
                        .ToList();

                    foreach (var d in toRemove)
                        services.Remove(d);

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase("WorkerLoginTestDb"));
                });
            }
        }

        private readonly HttpClient _client;
        private readonly InMemoryFactory _factory;

        public WorkerLoginTests(InMemoryFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private async Task SeedWorkerAsync(string email, string password, string fullName = "Test Worker")
        {
            var registerRequest = new
            {
                fullName = fullName,
                email = email,
                phone = "0501234567",
                municipality = "Haifa",
                department = "Sanitation",
                password = password
            };

            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register-worker", registerRequest);
            var registerContent = await registerResponse.Content.ReadAsStringAsync();

            registerResponse.StatusCode.Should().Be(HttpStatusCode.OK, registerContent);

            await ApproveWorkerAsync(email);
        }

        private async Task ApproveWorkerAsync(string email)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var worker = await db.Workers.FirstOrDefaultAsync(w => w.Email == email);
            worker.Should().NotBeNull();

            var workerType = worker!.GetType();

            SetPropertyIfExists(workerType, worker, "IsApproved", true);
            SetPropertyIfExists(workerType, worker, "Approved", true);
            SetPropertyIfExists(workerType, worker, "IsPending", false);
            SetPropertyIfExists(workerType, worker, "ApprovalStatus", "Approved");
            SetPropertyIfExists(workerType, worker, "Status", "Approved");
            SetPropertyIfExists(workerType, worker, "AccountStatus", "Approved");

            await db.SaveChangesAsync();
        }

        private static void SetPropertyIfExists(Type type, object obj, string propertyName, object value)
        {
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || !prop.CanWrite)
                return;

            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            if (targetType == typeof(bool) && value is bool boolValue)
            {
                prop.SetValue(obj, boolValue);
                return;
            }

            if (targetType == typeof(string) && value is string stringValue)
            {
                prop.SetValue(obj, stringValue);
            }
        }

        [Fact]
        public async Task LoginWorker_ShouldReturnSuccess_WhenCredentialsAreCorrect()
        {
            var email = $"worker_{Guid.NewGuid()}@test.com";
            var password = "123456";

            await SeedWorkerAsync(email, password);

            var request = new
            {
                email = email,
                password = password
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-worker", request);
            var content = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK, content);
        }

        [Fact]
        public async Task LoginWorker_ShouldReturnContent_WhenLoginSucceeds()
        {
            var email = $"worker_content_{Guid.NewGuid()}@test.com";
            var password = "123456";

            await SeedWorkerAsync(email, password);

            var request = new
            {
                email = email,
                password = password
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-worker", request);
            var content = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK, content);
            content.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task LoginWorker_ShouldFail_WhenEmailIsMissing()
        {
            var request = new
            {
                email = "",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-worker", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginWorker_ShouldFail_WhenPasswordIsMissing()
        {
            var request = new
            {
                email = "worker@test.com",
                password = ""
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-worker", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginWorker_ShouldFail_WhenBothFieldsAreMissing()
        {
            var request = new
            {
                email = "",
                password = ""
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-worker", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginWorker_ShouldFail_WhenEmailDoesNotExist()
        {
            var request = new
            {
                email = $"notfound_{Guid.NewGuid()}@test.com",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-worker", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginWorker_ShouldFail_WhenPasswordIsIncorrect()
        {
            var email = $"wrongpass_{Guid.NewGuid()}@test.com";
            await SeedWorkerAsync(email, "123456");

            var request = new
            {
                email = email,
                password = "wrong123"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-worker", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginWorker_ShouldFail_WhenEmailFormatIsInvalid()
        {
            var request = new
            {
                email = "invalid-email",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-worker", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginWorker_ShouldFail_WhenBodyIsNull()
        {
            var response = await _client.PostAsJsonAsync<object?>("/api/auth/login-worker", null);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginWorker_ShouldHandleLongInputValues()
        {
            var request = new
            {
                email = new string('a', 200) + "@test.com",
                password = new string('b', 300)
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-worker", request);

            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.BadRequest,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.NotFound
            );
        }
    }
}