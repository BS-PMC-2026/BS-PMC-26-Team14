using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CityFix.Api.Data;
using CityFix.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Xunit;

namespace CityFix.Api.Tests
{
    public class WorkerNotificationsTests : IClassFixture<WorkerNotificationsTests.InMemoryFactory>
    {
        public class InMemoryFactory : WebApplicationFactory<Program>
        {
            private readonly string _dbName = "WorkerNotificationsTestDb_" + Guid.NewGuid();

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
                        options.UseInMemoryDatabase(_dbName));
                });
            }
        }

        private readonly HttpClient _client;
        private readonly InMemoryFactory _factory;

        public WorkerNotificationsTests(InMemoryFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // =========================
        // Helper Methods
        // =========================

        private async Task SeedWorkerAsync(
            string email,
            string municipality = "באר שבע",
            string department = "Roads",
            string approvalStatus = "Approved",
            bool isBlocked = false)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (!await db.Workers.AnyAsync(w => w.Email == email))
            {
                db.Workers.Add(new Worker
                {
                    FullName = "Test Worker",
                    Email = email,
                    Phone = "0501234567",
                    Municipality = municipality,
                    Department = department,
                    PasswordHash = "hashed-password",
                    ApprovalStatus = approvalStatus,
                    IsBlocked = isBlocked
                });

                await db.SaveChangesAsync();
            }
        }

        private async Task SeedReportAsync(
            int idSeed,
            string customerEmail,
            string municipality,
            string category,
            string status = "Open",
            string assignedWorkerEmail = "")
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Reports.Add(new Report
            {
                CustomerEmail = customerEmail,
                Category = category,
                Priority = "High",
                Description = "Test report description",
                Notes = "",
                Location = $"Test location in {municipality}",
                Municipality = municipality,
                Latitude = 31.251 + idSeed,
                Longitude = 34.791 + idSeed,
                LocationPoint = new Point(34.791 + idSeed, 31.251 + idSeed) { SRID = 4326 },
                Status = status,
                AssignedWorkerEmail = assignedWorkerEmail,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        private static bool IsMatchingNotification(
            Worker worker,
            Report report,
            List<string> allowedCategories)
        {
            return report.Status == "Open"
                   && string.IsNullOrWhiteSpace(report.AssignedWorkerEmail)
                   && report.Municipality == worker.Municipality
                   && allowedCategories.Contains(report.Category);
        }

        // =========================
        // Unit Tests
        // =========================

        [Fact]
        public void Worker_ShouldStoreMunicipalityAndDepartment()
        {
            var worker = new Worker
            {
                FullName = "Test Worker",
                Email = "worker@test.com",
                Municipality = "באר שבע",
                Department = "Roads"
            };

            worker.Municipality.Should().Be("באר שבע");
            worker.Department.Should().Be("Roads");
        }

        [Fact]
        public void Report_ShouldStoreMunicipality()
        {
            var report = new Report
            {
                Municipality = "באר שבע",
                Location = "Rager Street, Beer Sheva"
            };

            report.Municipality.Should().Be("באר שבע");
            report.Location.Should().Contain("Beer Sheva");
        }

        [Fact]
        public void NotificationMatch_ShouldReturnTrue_WhenSameMunicipalityAndCategory()
        {
            var worker = new Worker
            {
                Municipality = "באר שבע",
                Department = "Roads"
            };

            var report = new Report
            {
                Municipality = "באר שבע",
                Category = "Road Damage",
                Status = "Open",
                AssignedWorkerEmail = ""
            };

            var allowedCategories = new List<string> { "Road Damage", "נזק בכביש" };

            var result = IsMatchingNotification(worker, report, allowedCategories);

            result.Should().BeTrue();
        }

        [Fact]
        public void NotificationMatch_ShouldReturnFalse_WhenMunicipalityIsDifferent()
        {
            var worker = new Worker
            {
                Municipality = "באר שבע",
                Department = "Roads"
            };

            var report = new Report
            {
                Municipality = "תל אביב-יפו",
                Category = "Road Damage",
                Status = "Open",
                AssignedWorkerEmail = ""
            };

            var allowedCategories = new List<string> { "Road Damage", "נזק בכביש" };

            var result = IsMatchingNotification(worker, report, allowedCategories);

            result.Should().BeFalse();
        }

        [Fact]
        public void NotificationMatch_ShouldReturnFalse_WhenCategoryIsDifferent()
        {
            var worker = new Worker
            {
                Municipality = "באר שבע",
                Department = "Roads"
            };

            var report = new Report
            {
                Municipality = "באר שבע",
                Category = "Street Lighting",
                Status = "Open",
                AssignedWorkerEmail = ""
            };

            var allowedCategories = new List<string> { "Road Damage", "נזק בכביש" };

            var result = IsMatchingNotification(worker, report, allowedCategories);

            result.Should().BeFalse();
        }

        [Fact]
        public void NotificationMatch_ShouldReturnFalse_WhenReportAlreadyAssigned()
        {
            var worker = new Worker
            {
                Municipality = "באר שבע",
                Department = "Roads"
            };

            var report = new Report
            {
                Municipality = "באר שבע",
                Category = "Road Damage",
                Status = "Open",
                AssignedWorkerEmail = "otherworker@test.com"
            };

            var allowedCategories = new List<string> { "Road Damage", "נזק בכביש" };

            var result = IsMatchingNotification(worker, report, allowedCategories);

            result.Should().BeFalse();
        }

        [Fact]
        public void NotificationMatch_ShouldReturnFalse_WhenReportIsNotOpen()
        {
            var worker = new Worker
            {
                Municipality = "באר שבע",
                Department = "Roads"
            };

            var report = new Report
            {
                Municipality = "באר שבע",
                Category = "Road Damage",
                Status = "In Treatment",
                AssignedWorkerEmail = ""
            };

            var allowedCategories = new List<string> { "Road Damage", "נזק בכביש" };

            var result = IsMatchingNotification(worker, report, allowedCategories);

            result.Should().BeFalse();
        }

        // =========================
        // Integration Tests - API
        // =========================

        [Fact]
        public async Task WorkerNotifications_ShouldReturnOk_WhenWorkerExistsAndApproved()
        {
            var workerEmail = $"worker_{Guid.NewGuid()}@test.com";

            await SeedWorkerAsync(workerEmail);
            await SeedReportAsync(1, "customer@test.com", "באר שבע", "Road Damage");

            var response = await _client.GetAsync(
                "/api/Auth/worker-notifications?workerEmail=" + workerEmail
            );

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task WorkerNotifications_ShouldReturnMatchingReport_WhenMunicipalityAndDepartmentMatch()
        {
            var workerEmail = $"worker_{Guid.NewGuid()}@test.com";

            await SeedWorkerAsync(workerEmail, "באר שבע", "Roads");
            await SeedReportAsync(2, "customer@test.com", "באר שבע", "Road Damage");

            var response = await _client.GetAsync(
                "/api/Auth/worker-notifications?workerEmail=" + workerEmail
            );

            var json = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            json.Should().Contain("Road Damage");
            json.Should().Contain("באר שבע");
        }

        [Fact]
        public async Task WorkerNotifications_ShouldNotReturnReport_WhenMunicipalityDoesNotMatch()
        {
            var workerEmail = $"worker_{Guid.NewGuid()}@test.com";

            await SeedWorkerAsync(workerEmail, "באר שבע", "Roads");
            await SeedReportAsync(3, "customer@test.com", "תל אביב-יפו", "Road Damage");

            var response = await _client.GetAsync(
                "/api/Auth/worker-notifications?workerEmail=" + workerEmail
            );

            var json = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            json.Should().NotContain("תל אביב-יפו");
        }

        [Fact]
        public async Task WorkerNotifications_ShouldNotReturnReport_WhenDepartmentDoesNotMatch()
        {
            var workerEmail = $"worker_{Guid.NewGuid()}@test.com";

            await SeedWorkerAsync(workerEmail, "באר שבע", "Lighting");
            await SeedReportAsync(4, "customer@test.com", "באר שבע", "Road Damage");

            var response = await _client.GetAsync(
                "/api/Auth/worker-notifications?workerEmail=" + workerEmail
            );

            var json = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            json.Should().NotContain("Road Damage");
        }

        [Fact]
        public async Task WorkerNotifications_ShouldReturnLightingReport_WhenWorkerDepartmentIsLighting()
        {
            var workerEmail = $"worker_{Guid.NewGuid()}@test.com";

            await SeedWorkerAsync(workerEmail, "באר שבע", "Lighting");
            await SeedReportAsync(5, "customer@test.com", "באר שבע", "Street Lighting");

            var response = await _client.GetAsync(
                "/api/Auth/worker-notifications?workerEmail=" + workerEmail
            );

            var json = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            json.Should().Contain("Street Lighting");
        }

        [Fact]
        public async Task WorkerNotifications_ShouldNotReturnReport_WhenReportAlreadyAssigned()
        {
            var workerEmail = $"worker_{Guid.NewGuid()}@test.com";

            await SeedWorkerAsync(workerEmail, "באר שבע", "Roads");
            await SeedReportAsync(
                6,
                "customer@test.com",
                "באר שבע",
                "Road Damage",
                "Open",
                "anotherworker@test.com"
            );

            var response = await _client.GetAsync(
                "/api/Auth/worker-notifications?workerEmail=" + workerEmail
            );

            var json = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            json.Should().NotContain("anotherworker@test.com");
        }

        [Fact]
        public async Task WorkerNotifications_ShouldNotReturnReport_WhenReportStatusIsInTreatment()
        {
            var workerEmail = $"worker_{Guid.NewGuid()}@test.com";

            await SeedWorkerAsync(workerEmail, "באר שבע", "Roads");
            await SeedReportAsync(7, "customer@test.com", "באר שבע", "Road Damage", "In Treatment");

            var response = await _client.GetAsync(
                "/api/Auth/worker-notifications?workerEmail=" + workerEmail
            );

            var json = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            json.Should().NotContain("In Treatment");
        }

        [Fact]
        public async Task WorkerNotifications_ShouldReturnBadRequest_WhenWorkerEmailMissing()
        {
            var response = await _client.GetAsync("/api/Auth/worker-notifications");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task WorkerNotifications_ShouldReturnNotFound_WhenWorkerDoesNotExist()
        {
            var response = await _client.GetAsync(
                "/api/Auth/worker-notifications?workerEmail=notfound@test.com"
            );

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task WorkerNotifications_ShouldReturnBadRequest_WhenWorkerIsBlocked()
        {
            var workerEmail = $"blocked_{Guid.NewGuid()}@test.com";

            await SeedWorkerAsync(
                email: workerEmail,
                municipality: "באר שבע",
                department: "Roads",
                approvalStatus: "Approved",
                isBlocked: true
            );

            var response = await _client.GetAsync(
                "/api/Auth/worker-notifications?workerEmail=" + workerEmail
            );

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task WorkerNotifications_ShouldReturnBadRequest_WhenWorkerIsNotApproved()
        {
            var workerEmail = $"pending_{Guid.NewGuid()}@test.com";

            await SeedWorkerAsync(
                email: workerEmail,
                municipality: "באר שבע",
                department: "Roads",
                approvalStatus: "Pending",
                isBlocked: false
            );

            var response = await _client.GetAsync(
                "/api/Auth/worker-notifications?workerEmail=" + workerEmail
            );

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}