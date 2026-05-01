using System.Net;
using System.Net.Http.Json;
using CityFix.Api.Data;
using CityFix.Api.Models;
using CityFix.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CityFix.Api.Tests;

public class WorkerReportIntegrationTests
{
    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string body)
        {
            return Task.CompletedTask;
        }
    }

   private sealed class TestCityFixFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "WorkerReportTestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var dbContextOptionConfigurations = services
                .Where(d => d.ServiceType.FullName != null &&
                            d.ServiceType.FullName.Contains("IDbContextOptionsConfiguration") &&
                            d.ServiceType.FullName.Contains("ApplicationDbContext"))
                .ToList();

            foreach (var descriptor in dbContextOptionConfigurations)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            services.RemoveAll<IEmailSender>();
            services.AddScoped<IEmailSender, FakeEmailSender>();
        });
    }
}

    private static async Task SeedCustomerAsync(
        TestCityFixFactory factory,
        string customerEmail)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.Customers.Add(new Customer
        {
            FullName = "Test Customer",
            Phone = "0501234567",
            Email = customerEmail,
            Address = "Hadera",
            PasswordHash = "hash-for-test"
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedApprovedWorkerAsync(
        TestCityFixFactory factory,
        string workerEmail)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.Workers.Add(new Worker
        {
            FullName = "Test Worker",
            NationalId = "123456789",
            Phone = "0507654321",
            Email = workerEmail,
            Department = "כבישים",
            Municipality = "חדרה",
            PasswordHash = "hash-for-test",
            ApprovalStatus = "Approved"
        });

        await db.SaveChangesAsync();
    }

    private static async Task<int> SeedOpenReportAsync(
        TestCityFixFactory factory,
        string customerEmail)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var report = new Report
        {
            CustomerEmail = customerEmail,
            Category = "נזק בכביש",
            Priority = "גבוהה",
            Description = "בור בכביש ליד הבית",
            Notes = "צריך טיפול",
            Latitude = 32.4340,
            Longitude = 34.9196,
            ImageBase64 = "data:image/jpeg;base64,AAAA",
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };

        db.Reports.Add(report);
        await db.SaveChangesAsync();

        return report.Id;
    }

    private static async Task<int> SeedInTreatmentReportAsync(
        TestCityFixFactory factory,
        string customerEmail,
        string workerEmail)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var report = new Report
        {
            CustomerEmail = customerEmail,
            Category = "נזק בכביש",
            Priority = "גבוהה",
            Description = "בור בכביש שכבר נלקח לטיפול",
            Notes = "בדיקת העלאת תמונה",
            Latitude = 32.4340,
            Longitude = 34.9196,
            ImageBase64 = "data:image/jpeg;base64,AAAA",
            Status = "In Treatment",
            AssignedWorkerEmail = workerEmail,
            AcceptedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        db.Reports.Add(report);
        await db.SaveChangesAsync();

        return report.Id;
    }

    [Fact]
    public async Task CreateReport_WithDetectedLocation_ShouldSaveReportWithLatitudeAndLongitude()
    {
        await using var factory = new TestCityFixFactory();
        var client = factory.CreateClient();

        var customerEmail = "customer.location@test.com";

        await SeedCustomerAsync(factory, customerEmail);

        var request = new
        {
            customerEmail = customerEmail,
            category = "נזק בכביש",
            priority = "גבוהה",
            description = "דיווח שנשלח אחרי זיהוי מיקום אוטומטי",
            notes = "בדיקת אינטגרציה למיקום",
            latitude = 32.4340,
            longitude = 34.9196,
            imageBase64 = "data:image/jpeg;base64,AAAA"
        };

        var response = await client.PostAsJsonAsync(
            "/api/Auth/create-report",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var savedReport = await db.Reports.FirstOrDefaultAsync(r =>
            r.CustomerEmail == customerEmail);

        savedReport.Should().NotBeNull();
        savedReport!.Status.Should().Be("Open");
        savedReport.Latitude.Should().Be(request.latitude);
        savedReport.Longitude.Should().Be(request.longitude);
        savedReport.Category.Should().Be("נזק בכביש");
        savedReport.Description.Should().Be("דיווח שנשלח אחרי זיהוי מיקום אוטומטי");
    }

    [Fact]
    public async Task AcceptReport_ByApprovedWorker_ShouldChangeReportStatusToInTreatment()
    {
        await using var factory = new TestCityFixFactory();
        var client = factory.CreateClient();

        var customerEmail = "customer.accept@test.com";
        var workerEmail = "worker.accept@test.com";

        await SeedCustomerAsync(factory, customerEmail);
        await SeedApprovedWorkerAsync(factory, workerEmail);

        var reportId = await SeedOpenReportAsync(factory, customerEmail);

        var request = new
        {
            workerEmail = workerEmail
        };

        var response = await client.PostAsJsonAsync(
            $"/api/Auth/accept-report/{reportId}",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var report = await db.Reports.FirstAsync(r => r.Id == reportId);

        report.Status.Should().Be("In Treatment");
        report.AssignedWorkerEmail.Should().Be(workerEmail);
        report.AcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task WorkerUploadImage_ForReportInTreatment_ShouldSaveImageAndKeepStatusInTreatment()
    {
        await using var factory = new TestCityFixFactory();
        var client = factory.CreateClient();

        var customerEmail = "customer.upload@test.com";
        var workerEmail = "worker.upload@test.com";

        await SeedCustomerAsync(factory, customerEmail);
        await SeedApprovedWorkerAsync(factory, workerEmail);

        var reportId = await SeedInTreatmentReportAsync(
            factory,
            customerEmail,
            workerEmail);

        var uploadedImage = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQABAAD";

        var request = new
        {
            workerEmail = workerEmail,
            imageBase64 = uploadedImage,
            note = "העובד העלה תמונה מהשטח"
        };

        var response = await client.PutAsJsonAsync(
            $"/api/Auth/worker-upload-image/{reportId}",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var report = await db.Reports.FirstAsync(r => r.Id == reportId);

        report.Status.Should().Be("In Treatment");
        report.WorkerImageBase64.Should().Be(uploadedImage);
        report.WorkerImageNote.Should().Be("העובד העלה תמונה מהשטח");
        report.WorkerImageUploadedAt.Should().NotBeNull();
    }
}