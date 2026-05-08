using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
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
    public class CreateReportTests : IClassFixture<CreateReportTests.InMemoryFactory>
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
                        options.UseInMemoryDatabase("CreateReportTestDb"));
                });
            }
        }

        private readonly HttpClient _client;
        private readonly InMemoryFactory _factory;

        public CreateReportTests(InMemoryFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private static IList<ValidationResult> Validate(object obj)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(obj);
            Validator.TryValidateObject(obj, context, results, true);
            return results;
        }

        private async Task SeedCustomerAsync(string email)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (!await db.Customers.AnyAsync(c => c.Email == email))
            {
                db.Customers.Add(new Customer
                {
                    FullName = "Ahmad Akhras",
                    Email = email,
                    Phone = "0501234567",
                    Address = "Beer Sheva",
                    PasswordHash = "hashed-password"
                });

                await db.SaveChangesAsync();
            }
        }

        // =========================
        // Unit Tests - DTO
        // =========================

        [Fact]
        public void CreateReportDto_ValidInput_PassesValidation()
        {
            var dto = new CreateReportDto
            {
                CustomerEmail = "customer@test.com",
                Category = "נזק בכביש",
                Priority = "גבוהה",
                Description = "יש בור בכביש",
                Notes = "ליד הכניסה",
                ImageBase64 = "data:image/png;base64,test",
                Latitude = 31.251,
                Longitude = 34.791
            };

            Validate(dto).Should().BeEmpty();
        }

        [Fact]
        public void CreateReportDto_MissingCustomerEmail_FailsValidation()
        {
            var dto = new CreateReportDto
            {
                CustomerEmail = "",
                Category = "נזק בכביש",
                Priority = "גבוהה",
                Description = "בעיה",
                Latitude = 31.251,
                Longitude = 34.791
            };

            Validate(dto).Should().NotBeEmpty();
        }

        [Fact]
        public void CreateReportDto_MissingCategory_FailsValidation()
        {
            var dto = new CreateReportDto
            {
                CustomerEmail = "customer@test.com",
                Category = "",
                Priority = "גבוהה",
                Description = "בעיה",
                Latitude = 31.251,
                Longitude = 34.791
            };

            Validate(dto).Should().NotBeEmpty();
        }

        [Fact]
        public void CreateReportDto_MissingPriority_FailsValidation()
        {
            var dto = new CreateReportDto
            {
                CustomerEmail = "customer@test.com",
                Category = "נזק בכביש",
                Priority = "",
                Description = "בעיה",
                Latitude = 31.251,
                Longitude = 34.791
            };

            Validate(dto).Should().NotBeEmpty();
        }

        [Fact]
        public void CreateReportDto_MissingDescription_FailsValidation()
        {
            var dto = new CreateReportDto
            {
                CustomerEmail = "customer@test.com",
                Category = "נזק בכביש",
                Priority = "גבוהה",
                Description = "",
                Latitude = 31.251,
                Longitude = 34.791
            };

            Validate(dto).Should().NotBeEmpty();
        }

        [Fact]
        public void CreateReportDto_NotesAndImageAreOptional_PassesValidation()
        {
            var dto = new CreateReportDto
            {
                CustomerEmail = "customer@test.com",
                Category = "נזק בכביש",
                Priority = "גבוהה",
                Description = "בעיה",
                Notes = "",
                ImageBase64 = "",
                Latitude = 31.251,
                Longitude = 34.791
            };

            Validate(dto).Should().BeEmpty();
        }

        // =========================
        // Unit Tests - Report Model
        // =========================

        [Fact]
        public void Report_DefaultStatus_ShouldBeOpen()
        {
            var report = new Report();

            report.Status.Should().Be("Open");
        }

        [Fact]
        public void Report_DefaultCreatedAt_ShouldNotBeDefault()
        {
            var report = new Report();

            report.CreatedAt.Should().NotBe(default);
        }

        [Fact]
        public void Report_ShouldStoreGisPointCorrectly()
        {
            var point = new Point(34.791, 31.251) { SRID = 4326 };

            var report = new Report
            {
                Latitude = 31.251,
                Longitude = 34.791,
                LocationPoint = point
            };

            report.LocationPoint.Should().NotBeNull();
            report.LocationPoint!.X.Should().Be(34.791);
            report.LocationPoint.Y.Should().Be(31.251);
            report.LocationPoint.SRID.Should().Be(4326);
        }

        // =========================
        // Integration Tests - API
        // =========================

        [Fact]
        public async Task CreateReport_ShouldReturnOk_WhenDataIsValid()
        {
            var email = $"customer_{Guid.NewGuid()}@test.com";
            await SeedCustomerAsync(email);

            var request = new
            {
                customerEmail = email,
                category = "נזק בכביש",
                priority = "גבוהה",
                description = "יש בור בכביש",
                notes = "ליד תחנת אוטובוס",
                latitude = 31.251,
                longitude = 34.791,
                imageBase64 = "data:image/png;base64,test"
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/create-report", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CreateReport_ShouldReturnReportId_WhenReportCreated()
        {
            var email = $"customer_{Guid.NewGuid()}@test.com";
            await SeedCustomerAsync(email);

            var request = new
            {
                customerEmail = email,
                category = "תאורת רחוב",
                priority = "בינונית",
                description = "עמוד תאורה לא עובד",
                notes = "",
                latitude = 31.252,
                longitude = 34.792,
                imageBase64 = ""
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/create-report", request);
            var json = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            json.Should().Contain("reportId");
        }

        [Fact]
        public async Task CreateReport_ShouldSaveReportInDatabase_WhenDataIsValid()
        {
            var email = $"customer_{Guid.NewGuid()}@test.com";
            await SeedCustomerAsync(email);

            var request = new
            {
                customerEmail = email,
                category = "מים / ביוב",
                priority = "גבוהה",
                description = "נזילת מים ברחוב",
                notes = "דחוף",
                latitude = 31.253,
                longitude = 34.793,
                imageBase64 = "data:image/png;base64,test"
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/create-report", request);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var saved = await db.Reports.FirstOrDefaultAsync(r => r.CustomerEmail == email);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            saved.Should().NotBeNull();
            saved!.Category.Should().Be("מים / ביוב");
            saved.Priority.Should().Be("גבוהה");
            saved.Description.Should().Be("נזילת מים ברחוב");
            saved.Notes.Should().Be("דחוף");
            saved.Latitude.Should().Be(31.253);
            saved.Longitude.Should().Be(34.793);
            saved.Status.Should().Be("Open");
        }

        [Fact]
        public async Task CreateReport_ShouldSaveGisPoint_WhenDataIsValid()
        {
            var email = $"customer_{Guid.NewGuid()}@test.com";
            await SeedCustomerAsync(email);

            var request = new
            {
                customerEmail = email,
                category = "גינון",
                priority = "נמוכה",
                description = "עץ שבור",
                notes = "",
                latitude = 31.254,
                longitude = 34.794,
                imageBase64 = ""
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/create-report", request);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var saved = await db.Reports.FirstOrDefaultAsync(r => r.CustomerEmail == email);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            saved.Should().NotBeNull();
            saved!.LocationPoint.Should().NotBeNull();
            saved.LocationPoint!.Y.Should().Be(31.254);
            saved.LocationPoint.X.Should().Be(34.794);
            saved.LocationPoint.SRID.Should().Be(4326);
        }

        [Fact]
        public async Task CreateReport_ShouldSaveCreatedAt_WhenDataIsValid()
        {
            var email = $"customer_{Guid.NewGuid()}@test.com";
            await SeedCustomerAsync(email);

            var before = DateTime.UtcNow.AddSeconds(-5);

            var request = new
            {
                customerEmail = email,
                category = "תחזוקה כללית",
                priority = "נמוכה",
                description = "בעיה כללית",
                notes = "",
                latitude = 31.255,
                longitude = 34.795,
                imageBase64 = ""
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/create-report", request);

            var after = DateTime.UtcNow.AddSeconds(5);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var saved = await db.Reports.FirstOrDefaultAsync(r => r.CustomerEmail == email);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            saved.Should().NotBeNull();
            saved!.CreatedAt.Should().BeOnOrAfter(before);
            saved.CreatedAt.Should().BeOnOrBefore(after);
        }

        [Fact]
        public async Task CreateReport_ShouldAllowMultipleReports_ForSameCustomer()
        {
            var email = $"customer_{Guid.NewGuid()}@test.com";
            await SeedCustomerAsync(email);

            var request1 = new
            {
                customerEmail = email,
                category = "נזק בכביש",
                priority = "גבוהה",
                description = "בור ראשון",
                notes = "",
                latitude = 31.251,
                longitude = 34.791,
                imageBase64 = ""
            };

            var request2 = new
            {
                customerEmail = email,
                category = "תאורת רחוב",
                priority = "בינונית",
                description = "תאורה לא עובדת",
                notes = "",
                latitude = 31.252,
                longitude = 34.792,
                imageBase64 = ""
            };

            var response1 = await _client.PostAsJsonAsync("/api/Auth/create-report", request1);
            var response2 = await _client.PostAsJsonAsync("/api/Auth/create-report", request2);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var count = await db.Reports.CountAsync(r => r.CustomerEmail == email);

            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
            count.Should().Be(2);
        }

        [Fact]
        public async Task CreateReport_ShouldWork_WhenNotesAndImageAreEmpty()
        {
            var email = $"customer_{Guid.NewGuid()}@test.com";
            await SeedCustomerAsync(email);

            var request = new
            {
                customerEmail = email,
                category = "תחזוקה כללית",
                priority = "נמוכה",
                description = "שלט שבור",
                notes = "",
                latitude = 31.255,
                longitude = 34.795,
                imageBase64 = ""
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/create-report", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CreateReport_ShouldReturnNotFound_WhenCustomerDoesNotExist()
        {
            var request = new
            {
                customerEmail = $"not_exists_{Guid.NewGuid()}@test.com",
                category = "נזק בכביש",
                priority = "גבוהה",
                description = "בעיה",
                notes = "",
                latitude = 31.251,
                longitude = 34.791,
                imageBase64 = ""
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/create-report", request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateReport_ShouldReturnBadRequest_WhenCustomerEmailMissing()
        {
            var request = new
            {
                customerEmail = "",
                category = "נזק בכביש",
                priority = "גבוהה",
                description = "בעיה",
                notes = "",
                latitude = 31.251,
                longitude = 34.791,
                imageBase64 = ""
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/create-report", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateReport_ShouldReturnBadRequest_WhenCategoryMissing()
        {
            var email = $"customer_{Guid.NewGuid()}@test.com";
            await SeedCustomerAsync(email);

            var request = new
            {
                customerEmail = email,
                category = "",
                priority = "גבוהה",
                description = "בעיה",
                notes = "",
                latitude = 31.251,
                longitude = 34.791,
                imageBase64 = ""
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/create-report", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateReport_ShouldReturnBadRequest_WhenPriorityMissing()
        {
            var email = $"customer_{Guid.NewGuid()}@test.com";
            await SeedCustomerAsync(email);

            var request = new
            {
                customerEmail = email,
                category = "נזק בכביש",
                priority = "",
                description = "בעיה",
                notes = "",
                latitude = 31.251,
                longitude = 34.791,
                imageBase64 = ""
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/create-report", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateReport_ShouldReturnBadRequest_WhenDescriptionMissing()
        {
            var email = $"customer_{Guid.NewGuid()}@test.com";
            await SeedCustomerAsync(email);

            var request = new
            {
                customerEmail = email,
                category = "נזק בכביש",
                priority = "גבוהה",
                description = "",
                notes = "",
                latitude = 31.251,
                longitude = 34.791,
                imageBase64 = ""
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/create-report", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateReport_ShouldReturnBadRequest_WhenLatitudeAndLongitudeAreZero()
        {
            var email = $"customer_{Guid.NewGuid()}@test.com";
            await SeedCustomerAsync(email);

            var request = new
            {
                customerEmail = email,
                category = "נזק בכביש",
                priority = "גבוהה",
                description = "בעיה",
                notes = "",
                latitude = 0,
                longitude = 0,
                imageBase64 = ""
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/create-report", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateReport_ShouldReturnBadRequest_WhenCoordinatesAreInvalid()
        {
            var email = $"customer_{Guid.NewGuid()}@test.com";
            await SeedCustomerAsync(email);

            var request = new
            {
                customerEmail = email,
                category = "נזק בכביש",
                priority = "גבוהה",
                description = "בעיה",
                notes = "",
                latitude = 100,
                longitude = 200,
                imageBase64 = ""
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/create-report", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateReport_ShouldReturnBadRequest_WhenLocationOutsideIsrael()
        {
            var email = $"customer_{Guid.NewGuid()}@test.com";
            await SeedCustomerAsync(email);

            var request = new
            {
                customerEmail = email,
                category = "נזק בכביש",
                priority = "גבוהה",
                description = "בעיה מחוץ לישראל",
                notes = "",
                latitude = 40.7128,
                longitude = -74.0060,
                imageBase64 = ""
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/create-report", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateReport_ShouldReturnBadRequest_WhenBodyIsEmpty()
        {
            var response = await _client.PostAsync(
                "/api/Auth/create-report",
                new StringContent("", Encoding.UTF8, "application/json")
            );

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateReport_ShouldReturnBadRequest_WhenJsonIsInvalid()
        {
            var invalidJson = new StringContent(
                "{ invalid json }",
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync("/api/Auth/create-report", invalidJson);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateReport_ShouldReturnUnsupportedMediaType_WhenContentTypeIsWrong()
        {
            var body = new StringContent("plain text", Encoding.UTF8, "text/plain");

            var response = await _client.PostAsync("/api/Auth/create-report", body);

            response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        }

        [Fact]
        public async Task CreateReport_ShouldReturnMethodNotAllowed_WhenUsingGet()
        {
            var response = await _client.GetAsync("/api/Auth/create-report");

            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }
    }
}