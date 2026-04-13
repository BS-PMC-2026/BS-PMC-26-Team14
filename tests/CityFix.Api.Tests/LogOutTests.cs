using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    /// <summary>
    /// Tests for the Log Out feature.
    /// Logout is a client-side operation (clears localStorage: userRole, userName, userEmail).
    /// These tests verify the login endpoints return correct session data, and that
    /// after "logging out" (clearing state), re-login works as expected.
    /// </summary>
    public class LogOutTests : IClassFixture<LogOutTests.InMemoryFactory>
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
                        options.UseInMemoryDatabase("LogOutTestDb"));
                });
            }
        }

        private readonly HttpClient _client;

        public LogOutTests(InMemoryFactory factory)
        {
            _client = factory.CreateClient();
        }

        // ── Customer login / logout cycle ────────────────────────────────────

        [Fact]
        public async Task CustomerLogin_ShouldReturnOk_WithSessionData()
        {
            var email = $"customer_{Guid.NewGuid()}@test.com";
            await RegisterCustomer(email, "Test Customer", "123456");

            var response = await _client.PostAsJsonAsync("/api/auth/login-customer",
                new { email, password = "123456" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("role");
            body.Should().Contain("email");
            body.Should().Contain("fullName");
        }

        [Fact]
        public async Task CustomerLogin_ShouldReturnCustomerRole()
        {
            var email = $"customer_{Guid.NewGuid()}@test.com";
            await RegisterCustomer(email, "Test Customer", "123456");

            var response = await _client.PostAsJsonAsync("/api/auth/login-customer",
                new { email, password = "123456" });

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            json.RootElement.GetProperty("role").GetString().Should().Be("Customer");
        }

        [Fact]
        public async Task CustomerLogin_AfterLogout_ShouldSucceedAgain()
        {
            // Simulate logout (client clears localStorage) then re-login
            var email = $"relogin_{Guid.NewGuid()}@test.com";
            await RegisterCustomer(email, "Re-Login User", "123456");

            // First login
            var first = await _client.PostAsJsonAsync("/api/auth/login-customer",
                new { email, password = "123456" });
            first.StatusCode.Should().Be(HttpStatusCode.OK);

            // Simulate logout: client clears state — no server call needed
            // Re-login should still work
            var second = await _client.PostAsJsonAsync("/api/auth/login-customer",
                new { email, password = "123456" });
            second.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CustomerLogin_ShouldFail_WhenPasswordIsWrong()
        {
            var email = $"wrong_pass_{Guid.NewGuid()}@test.com";
            await RegisterCustomer(email, "Test Customer", "correctpass");

            var response = await _client.PostAsJsonAsync("/api/auth/login-customer",
                new { email, password = "wrongpass" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CustomerLogin_ShouldFail_WhenEmailDoesNotExist()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login-customer",
                new { email = "nobody@test.com", password = "123456" });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ── Worker login / logout cycle ──────────────────────────────────────

        [Fact]
        public async Task WorkerLogin_ShouldReturnOk_WhenApproved()
        {
            var email = $"worker_{Guid.NewGuid()}@test.com";
            var workerId = await RegisterAndApproveWorker(email, "123456");

            var response = await _client.PostAsJsonAsync("/api/auth/login-worker",
                new { email, password = "123456" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            json.RootElement.GetProperty("role").GetString().Should().Be("Worker");
        }

        [Fact]
        public async Task WorkerLogin_AfterLogout_ShouldSucceedAgain()
        {
            var email = $"worker_relogin_{Guid.NewGuid()}@test.com";
            await RegisterAndApproveWorker(email, "123456");

            var first = await _client.PostAsJsonAsync("/api/auth/login-worker",
                new { email, password = "123456" });
            first.StatusCode.Should().Be(HttpStatusCode.OK);

            // Simulate logout then re-login
            var second = await _client.PostAsJsonAsync("/api/auth/login-worker",
                new { email, password = "123456" });
            second.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task WorkerLogin_ShouldFail_WhenPasswordIsWrong()
        {
            var email = $"worker_wrong_{Guid.NewGuid()}@test.com";
            await RegisterAndApproveWorker(email, "correctpass");

            var response = await _client.PostAsJsonAsync("/api/auth/login-worker",
                new { email, password = "wrongpass" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ── Helper methods ───────────────────────────────────────────────────

        private async Task RegisterCustomer(string email, string fullName, string password)
        {
            await _client.PostAsJsonAsync("/api/auth/register-customer", new
            {
                phone = "0501234567",
                fullName,
                email,
                address = "Test Address",
                password
            });
        }

        private async Task<int> RegisterAndApproveWorker(string email, string password)
        {
            await _client.PostAsJsonAsync("/api/auth/register-worker", new
            {
                fullName = "Test Worker",
                nationalId = "123456789",
                phone = "0501234567",
                email,
                department = "Roads",
                municipality = "TestCity",
                password
            });

            // Get worker id from pending workers list
            var pendingResponse = await _client.GetAsync("/api/auth/pending-workers");
            var json = JsonDocument.Parse(await pendingResponse.Content.ReadAsStringAsync());
            var workers = json.RootElement.EnumerateArray();
            int workerId = 0;
            foreach (var w in workers)
            {
                if (w.GetProperty("email").GetString() == email)
                {
                    workerId = w.GetProperty("id").GetInt32();
                    break;
                }
            }

            await _client.PostAsync($"/api/auth/approve-worker/{workerId}", null);
            return workerId;
        }
    }
}