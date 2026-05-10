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

// ─── Unit Tests ──────────────────────────────────────────────────────────────

public class WorkerReportsMapUnitTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WorkerReportsMapUnitTests()
    {
        var dbName = $"WorkerMapUnitDb_{Guid.NewGuid()}";
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var toRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                             || (d.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration") == true
                                 && d.ServiceType.FullName.Contains("ApplicationDbContext")))
                    .ToList();
                foreach (var d in toRemove) services.Remove(d);
                services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
            });
        });
        _client = _factory.CreateClient();
    }

    public void Dispose() { _client.Dispose(); _factory.Dispose(); }

    private async Task SeedReportAsync(string status, string category = "נזק בכביש", string priority = "גבוהה")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Reports.Add(new Report
        {
            CustomerEmail = $"u_{Guid.NewGuid()}@test.com",
            Category = category,
            Priority = priority,
            Description = "תיאור",
            Status = status,
            Latitude = 31.5,
            Longitude = 34.8,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    // The worker popup needs id, category, status, priority, createdAt, latitude, longitude.
    [Fact]
    public async Task ReportsMap_ReturnsId_RequiredForViewReportRedirect()
    {
        await SeedReportAsync("Open");

        var response = await _client.GetAsync("/api/Auth/reports-map");
        var reports = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        reports.Should().HaveCount(1);
        reports![0].TryGetProperty("id", out var idProp).Should().BeTrue();
        idProp.GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ReportsMap_ReturnsPriority_RequiredForWorkerPopup()
    {
        await SeedReportAsync("Open", priority: "בינונית");

        var response = await _client.GetAsync("/api/Auth/reports-map");
        var reports = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        reports.Should().HaveCount(1);
        reports![0].TryGetProperty("priority", out var p).Should().BeTrue();
        p.GetString().Should().Be("בינונית");
    }

    [Fact]
    public async Task ReportsMap_ReturnsCompletedReports_WorkerMapFiltersClientSide()
    {
        await SeedReportAsync("Open");
        await SeedReportAsync("In Treatment");
        await SeedReportAsync("Completed");

        var response = await _client.GetAsync("/api/Auth/reports-map");
        var reports = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        // Endpoint returns all statuses — worker map filters to active in the browser.
        reports.Should().HaveCount(3);
        reports!.Select(r => r.GetProperty("status").GetString())
            .Should().Contain(new[] { "Open", "In Treatment", "Completed" });
    }

    [Fact]
    public async Task ReportsMap_ActiveStatusFilter_ExcludesCompleted()
    {
        await SeedReportAsync("Open");
        await SeedReportAsync("In Treatment");
        await SeedReportAsync("Completed");

        // Simulate what the worker map does: pass both active statuses.
        var response = await _client.GetAsync("/api/Auth/reports-map?status=Open,In Treatment");
        var reports = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        reports.Should().HaveCount(2);
        reports!.Should().NotContain(r => r.GetProperty("status").GetString() == "Completed");
    }

    [Fact]
    public async Task ReportsMap_OpenReportHasCoordinates_MarkerCanBeRendered()
    {
        await SeedReportAsync("Open");

        var response = await _client.GetAsync("/api/Auth/reports-map");
        var reports = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        var r = reports![0];
        r.GetProperty("latitude").GetDouble().Should().NotBe(0);
        r.GetProperty("longitude").GetDouble().Should().NotBe(0);
    }
}

// ─── Integration Tests ───────────────────────────────────────────────────────

public class WorkerReportsMapIntegrationTests
{
    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string body) => Task.CompletedTask;
    }

    private sealed class WorkerMapFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"WorkerMapIntDb_{Guid.NewGuid()}";

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

    private static async Task SeedCustomerAsync(WorkerMapFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Customers.Add(new Customer
        {
            FullName = "Test Customer", Phone = "0501234567",
            Email = email, Address = "Tel Aviv", PasswordHash = "hash"
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedApprovedWorkerAsync(WorkerMapFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Workers.Add(new Worker
        {
            FullName = "Test Worker", NationalId = "123456789", Phone = "0507654321",
            Email = email, Department = "כבישים", Municipality = "תל אביב",
            PasswordHash = "hash", ApprovalStatus = "Approved"
        });
        await db.SaveChangesAsync();
    }

    // ── Test 1 ─────────────────────────────────────────────────────────────
    // Two different workers each accept a report. The map must show BOTH
    // In Treatment reports — not just one worker's assignment.
    [Fact]
    public async Task TwoWorkersEachAcceptReport_MapShowsBothInTreatment()
    {
        await using var factory = new WorkerMapFactory();
        var client = factory.CreateClient();

        var customerEmail = "map.cust.multi@test.com";
        var worker1Email = "map.worker1@test.com";
        var worker2Email = "map.worker2@test.com";

        await SeedCustomerAsync(factory, customerEmail);
        await SeedApprovedWorkerAsync(factory, worker1Email);
        await SeedApprovedWorkerAsync(factory, worker2Email);

        async Task<int> CreateReport(string category)
        {
            var res = await client.PostAsJsonAsync("/api/Auth/create-report", new
            {
                customerEmail,
                category,
                priority = "גבוהה",
                description = "תיאור",
                notes = "",
                latitude = 31.5,
                longitude = 34.8,
                imageBase64 = ""
            });
            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("reportId").GetInt32();
        }

        var id1 = await CreateReport("נזק בכביש");
        var id2 = await CreateReport("תאורת רחוב");

        await client.PostAsJsonAsync($"/api/Auth/accept-report/{id1}", new { workerEmail = worker1Email });
        await client.PostAsJsonAsync($"/api/Auth/accept-report/{id2}", new { workerEmail = worker2Email });

        var mapResponse = await client.GetAsync("/api/Auth/reports-map?status=In Treatment");
        var reports = await mapResponse.Content.ReadFromJsonAsync<JsonElement[]>();

        reports.Should().HaveCount(2);
        reports!.Should().OnlyContain(r => r.GetProperty("status").GetString() == "In Treatment");
    }

    // ── Test 2 ─────────────────────────────────────────────────────────────
    // The reportId returned by the map endpoint matches the one in the
    // open-reports endpoint — confirming the "View Report" redirect will work.
    [Fact]
    public async Task ReportIdFromMap_MatchesReportIdInOpenReports_RedirectWillWork()
    {
        await using var factory = new WorkerMapFactory();
        var client = factory.CreateClient();

        var customerEmail = "map.cust.id@test.com";
        var workerEmail = "map.worker.id@test.com";
        await SeedCustomerAsync(factory, customerEmail);
        await SeedApprovedWorkerAsync(factory, workerEmail);

        var createRes = await client.PostAsJsonAsync("/api/Auth/create-report", new
        {
            customerEmail,
            category = "גינון",
            priority = "נמוכה",
            description = "עץ שבור",
            notes = "",
            latitude = 31.7,
            longitude = 34.9,
            imageBase64 = ""
        });
        var createJson = await createRes.Content.ReadFromJsonAsync<JsonElement>();
        var createdId = createJson.GetProperty("reportId").GetInt32();

        var mapRes = await client.GetAsync("/api/Auth/reports-map");
        var mapReports = await mapRes.Content.ReadFromJsonAsync<JsonElement[]>();
        var mapId = mapReports![0].GetProperty("id").GetInt32();

        var openRes = await client.GetAsync($"/api/Auth/open-reports?workerEmail={Uri.EscapeDataString(workerEmail)}");
        var openReports = await openRes.Content.ReadFromJsonAsync<JsonElement[]>();
        var openIds = openReports!.Select(r => r.GetProperty("id").GetInt32()).ToList();

        mapId.Should().Be(createdId);
        openIds.Should().Contain(mapId);
    }

    // ── Test 3 ─────────────────────────────────────────────────────────────
    // Map with mixed statuses: worker's view should be able to filter to
    // active only (Open + In Treatment) and exclude Completed.
    [Fact]
    public async Task MixedStatuses_ActiveFilter_ExcludesCompletedFromWorkerView()
    {
        await using var factory = new WorkerMapFactory();
        var client = factory.CreateClient();

        var customerEmail = "map.cust.mix@test.com";
        var workerEmail = "map.worker.mix@test.com";
        await SeedCustomerAsync(factory, customerEmail);
        await SeedApprovedWorkerAsync(factory, workerEmail);

        async Task<int> CreateReport()
        {
            var res = await client.PostAsJsonAsync("/api/Auth/create-report", new
            {
                customerEmail,
                category = "נזק בכביש",
                priority = "גבוהה",
                description = "תיאור",
                notes = "",
                latitude = 31.5,
                longitude = 34.8,
                imageBase64 = ""
            });
            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("reportId").GetInt32();
        }

        // Seed directly as Completed (no endpoint to complete via API currently)
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Reports.Add(new Report
            {
                CustomerEmail = customerEmail,
                Category = "מים / ביוב",
                Priority = "גבוהה",
                Description = "הושלם",
                Status = "Completed",
                Latitude = 31.5,
                Longitude = 34.8,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var openId = await CreateReport();
        var inTreatmentId = await CreateReport();
        await client.PostAsJsonAsync($"/api/Auth/accept-report/{inTreatmentId}", new { workerEmail });

        // Simulate what the worker map does: request both active statuses.
        var activeRes = await client.GetAsync("/api/Auth/reports-map?status=Open,In Treatment");
        var activeReports = await activeRes.Content.ReadFromJsonAsync<JsonElement[]>();

        activeReports.Should().HaveCount(2);
        activeReports!.Should().OnlyContain(r =>
            r.GetProperty("status").GetString() == "Open" ||
            r.GetProperty("status").GetString() == "In Treatment");
    }

    // ── Test 4 ─────────────────────────────────────────────────────────────
    // After a worker accepts a report, the status change is immediately
    // reflected in the map endpoint — the worker sees it move from Open
    // to In Treatment without any delay.
    [Fact]
    public async Task AcceptReport_StatusImmediatelyReflectedOnMap()
    {
        await using var factory = new WorkerMapFactory();
        var client = factory.CreateClient();

        var customerEmail = "map.cust.immediate@test.com";
        var workerEmail = "map.worker.immediate@test.com";
        await SeedCustomerAsync(factory, customerEmail);
        await SeedApprovedWorkerAsync(factory, workerEmail);

        var createRes = await client.PostAsJsonAsync("/api/Auth/create-report", new
        {
            customerEmail,
            category = "תאורת רחוב",
            priority = "בינונית",
            description = "תיאור",
            notes = "",
            latitude = 32.0,
            longitude = 34.7,
            imageBase64 = ""
        });
        var createJson = await createRes.Content.ReadFromJsonAsync<JsonElement>();
        var reportId = createJson.GetProperty("reportId").GetInt32();

        // Before accept: map shows Open
        var beforeMap = await (await client.GetAsync("/api/Auth/reports-map"))
            .Content.ReadFromJsonAsync<JsonElement[]>();
        beforeMap!.First(r => r.GetProperty("id").GetInt32() == reportId)
            .GetProperty("status").GetString().Should().Be("Open");

        await client.PostAsJsonAsync($"/api/Auth/accept-report/{reportId}", new { workerEmail });

        // After accept: map shows In Treatment
        var afterMap = await (await client.GetAsync("/api/Auth/reports-map"))
            .Content.ReadFromJsonAsync<JsonElement[]>();
        afterMap!.First(r => r.GetProperty("id").GetInt32() == reportId)
            .GetProperty("status").GetString().Should().Be("In Treatment");
    }
}