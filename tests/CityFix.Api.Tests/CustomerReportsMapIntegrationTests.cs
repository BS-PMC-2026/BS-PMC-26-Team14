using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

public class CustomerReportsMapIntegrationTests
{
    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string body) => Task.CompletedTask;
    }

    private sealed class MapTestFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"MapIntegrationTestDb_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var toRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                             || (d.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration") == true
                                 && d.ServiceType.FullName.Contains("ApplicationDbContext")))
                    .ToList();

                foreach (var d in toRemove) services.Remove(d);

                var name = _dbName;
                services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(name));

                services.RemoveAll<IEmailSender>();
                services.AddScoped<IEmailSender, FakeEmailSender>();
            });
        }
    }

    private static async Task SeedCustomerAsync(MapTestFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Customers.Add(new Customer
        {
            FullName = "Test Customer",
            Phone = "0501234567",
            Email = email,
            Address = "Tel Aviv",
            PasswordHash = "hash"
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedApprovedWorkerAsync(MapTestFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Workers.Add(new Worker
        {
            FullName = "Test Worker",
            NationalId = "123456789",
            Phone = "0507654321",
            Email = email,
            Department = "כבישים",
            Municipality = "תל אביב",
            PasswordHash = "hash",
            ApprovalStatus = "Approved"
        });
        await db.SaveChangesAsync();
    }

    // ─── Test 1 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateReport_ThenGetMap_ReportAppearsWithCorrectFields()
    {
        await using var factory = new MapTestFactory();
        var client = factory.CreateClient();

        var email = "map.customer1@test.com";
        await SeedCustomerAsync(factory, email);

        var createRequest = new
        {
            customerEmail = email,
            category = "תאורת רחוב",
            priority = "בינונית",
            description = "עמוד תאורה שבור",
            notes = "",
            latitude = 32.0853,
            longitude = 34.7818,
            imageBase64 = ""
        };

        var createResponse = await client.PostAsJsonAsync("/api/Auth/create-report", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var mapResponse = await client.GetAsync("/api/Auth/reports-map");
        var reports = await mapResponse.Content.ReadFromJsonAsync<JsonElement[]>();

        reports.Should().HaveCount(1);
        var r = reports![0];
        r.GetProperty("category").GetString().Should().Be("תאורת רחוב");
        r.GetProperty("status").GetString().Should().Be("Open");
        r.GetProperty("latitude").GetDouble().Should().BeApproximately(32.0853, 0.0001);
        r.GetProperty("longitude").GetDouble().Should().BeApproximately(34.7818, 0.0001);
        r.GetProperty("priority").GetString().Should().Be("בינונית");
    }

    // ─── Test 2 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateReport_ThenWorkerAccepts_MapShowsInTreatmentStatus()
    {
        await using var factory = new MapTestFactory();
        var client = factory.CreateClient();

        var customerEmail = "map.customer2@test.com";
        var workerEmail = "map.worker2@test.com";
        await SeedCustomerAsync(factory, customerEmail);
        await SeedApprovedWorkerAsync(factory, workerEmail);

        var createRequest = new
        {
            customerEmail,
            category = "נזק בכביש",
            priority = "גבוהה",
            description = "בור עמוק",
            notes = "",
            latitude = 31.7683,
            longitude = 35.2137,
            imageBase64 = ""
        };

        var createResponse = await client.PostAsJsonAsync("/api/Auth/create-report", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var reportId = createJson.GetProperty("reportId").GetInt32();

        var acceptResponse = await client.PostAsJsonAsync(
            $"/api/Auth/accept-report/{reportId}",
            new { workerEmail });
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var mapResponse = await client.GetAsync("/api/Auth/reports-map");
        var reports = await mapResponse.Content.ReadFromJsonAsync<JsonElement[]>();

        reports.Should().HaveCount(1);
        reports![0].GetProperty("status").GetString().Should().Be("In Treatment");
    }

    // ─── Test 3 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MultipleReports_FilterByStatus_ReturnsOnlyMatchingReports()
    {
        await using var factory = new MapTestFactory();
        var client = factory.CreateClient();

        var email = "map.customer3@test.com";
        var workerEmail = "map.worker3@test.com";
        await SeedCustomerAsync(factory, email);
        await SeedApprovedWorkerAsync(factory, workerEmail);

        var createAndGetId = async (string category) =>
        {
            var res = await client.PostAsJsonAsync("/api/Auth/create-report", new
            {
                customerEmail = email,
                category,
                priority = "נמוכה",
                description = "תיאור",
                notes = "",
                latitude = 31.5,
                longitude = 34.8,
                imageBase64 = ""
            });
            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("reportId").GetInt32();
        };

        var id1 = await createAndGetId("נזק בכביש");
        var id2 = await createAndGetId("גינון");
        var id3 = await createAndGetId("מים / ביוב");

        await client.PostAsJsonAsync($"/api/Auth/accept-report/{id1}", new { workerEmail });

        var openResponse = await client.GetAsync("/api/Auth/reports-map?status=Open");
        var openReports = await openResponse.Content.ReadFromJsonAsync<JsonElement[]>();
        openReports.Should().HaveCount(2);
        openReports!.Should().OnlyContain(r => r.GetProperty("status").GetString() == "Open");

        var inTreatmentResponse = await client.GetAsync("/api/Auth/reports-map?status=In Treatment");
        var inTreatmentReports = await inTreatmentResponse.Content.ReadFromJsonAsync<JsonElement[]>();
        inTreatmentReports.Should().HaveCount(1);
        inTreatmentReports![0].GetProperty("status").GetString().Should().Be("In Treatment");
    }

    // ─── Test 4 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReportsCreatedBeforeFromDate_AreExcludedFromMap()
    {
        await using var factory = new MapTestFactory();
        var client = factory.CreateClient();

        var email = "map.customer4@test.com";
        await SeedCustomerAsync(factory, email);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.Reports.Add(new Report
        {
            CustomerEmail = email,
            Category = "גינון",
            Priority = "נמוכה",
            Description = "ישן",
            Status = "Open",
            Latitude = 31.5,
            Longitude = 34.8,
            CreatedAt = new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        });
        db.Reports.Add(new Report
        {
            CustomerEmail = email,
            Category = "תאורת רחוב",
            Priority = "גבוהה",
            Description = "חדש",
            Status = "Open",
            Latitude = 31.5,
            Longitude = 34.8,
            CreatedAt = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();

        var response = await client.GetAsync("/api/Auth/reports-map?fromDate=2024-01-01");
        var reports = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        reports.Should().HaveCount(1);
        reports![0].GetProperty("category").GetString().Should().Be("תאורת רחוב");
    }

    // ─── Test 5 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullLifecycle_CreateAcceptUploadImage_AllStatusesVisibleOnMap()
    {
        await using var factory = new MapTestFactory();
        var client = factory.CreateClient();

        var customerEmail = "map.customer5@test.com";
        var workerEmail = "map.worker5@test.com";
        await SeedCustomerAsync(factory, customerEmail);
        await SeedApprovedWorkerAsync(factory, workerEmail);

        // Step 1: customer creates report → Open on map
        var createRes = await client.PostAsJsonAsync("/api/Auth/create-report", new
        {
            customerEmail,
            category = "מים / ביוב",
            priority = "גבוהה",
            description = "צנור שבור",
            notes = "",
            latitude = 32.0853,
            longitude = 34.7818,
            imageBase64 = ""
        });
        createRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var createJson = await createRes.Content.ReadFromJsonAsync<JsonElement>();
        var reportId = createJson.GetProperty("reportId").GetInt32();

        var mapOpen = await (await client.GetAsync("/api/Auth/reports-map"))
            .Content.ReadFromJsonAsync<JsonElement[]>();
        mapOpen.Should().HaveCount(1);
        mapOpen![0].GetProperty("status").GetString().Should().Be("Open");

        // Step 2: worker accepts → In Treatment on map
        var acceptRes = await client.PostAsJsonAsync($"/api/Auth/accept-report/{reportId}", new { workerEmail });
        acceptRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var mapTreatment = await (await client.GetAsync("/api/Auth/reports-map"))
            .Content.ReadFromJsonAsync<JsonElement[]>();
        mapTreatment![0].GetProperty("status").GetString().Should().Be("In Treatment");

        // Step 3: worker uploads image → still In Treatment on map
        var uploadRes = await client.PutAsJsonAsync($"/api/Auth/worker-upload-image/{reportId}", new
        {
            workerEmail,
            imageBase64 = "data:image/jpeg;base64,AAAA",
            note = "סיום עבודה"
        });
        uploadRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var mapAfterUpload = await (await client.GetAsync("/api/Auth/reports-map"))
            .Content.ReadFromJsonAsync<JsonElement[]>();
        mapAfterUpload![0].GetProperty("status").GetString().Should().Be("In Treatment");
    }
}