using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CityFix.Api.Data;
using CityFix.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CityFix.Api.Tests;

public class ReportsMapTests : IDisposable
{
    private readonly string _dbName = $"ReportsMapTestDb_{Guid.NewGuid()}";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ReportsMapTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
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

                var dbName = _dbName;
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        });

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task SeedReportAsync(string status, DateTime createdAt, string category = "נזק בכביש", double lat = 31.5, double lng = 34.8)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.Reports.Add(new Report
        {
            CustomerEmail = $"map_{Guid.NewGuid()}@test.com",
            Category = category,
            Priority = "גבוהה",
            Description = "תיאור",
            Status = status,
            Latitude = lat,
            Longitude = lng,
            CreatedAt = createdAt
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetReportsMap_ReturnsOk_WhenNoReports()
    {
        var response = await _client.GetAsync("/api/Auth/reports-map");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Be("[]");
    }

    [Fact]
    public async Task GetReportsMap_ReturnsAllReports_WhenNoFiltersApplied()
    {
        var now = DateTime.UtcNow;
        await SeedReportAsync("Open", now);
        await SeedReportAsync("Completed", now);

        var response = await _client.GetAsync("/api/Auth/reports-map");
        var reports = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        reports.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetReportsMap_ReturnsExpectedFields()
    {
        await SeedReportAsync("Open", DateTime.UtcNow, "נזק בכביש", 31.2, 34.9);

        var response = await _client.GetAsync("/api/Auth/reports-map");
        var reports = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        reports.Should().HaveCount(1);
        var report = reports![0];
        report.TryGetProperty("id", out _).Should().BeTrue();
        report.TryGetProperty("category", out _).Should().BeTrue();
        report.TryGetProperty("status", out _).Should().BeTrue();
        report.TryGetProperty("createdAt", out _).Should().BeTrue();
        report.TryGetProperty("latitude", out _).Should().BeTrue();
        report.TryGetProperty("longitude", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetReportsMap_FilterByStatus_ReturnsOnlyMatchingReports()
    {
        var now = DateTime.UtcNow;
        await SeedReportAsync("Open", now);
        await SeedReportAsync("Completed", now);
        await SeedReportAsync("In Treatment", now);

        var response = await _client.GetAsync("/api/Auth/reports-map?status=Open");
        var reports = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        reports.Should().HaveCount(1);
        reports!.Should().OnlyContain(r => r.GetProperty("status").GetString() == "Open");
    }

    [Fact]
    public async Task GetReportsMap_FilterByFromDate_ExcludesOlderReports()
    {
        var oldDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        await SeedReportAsync("Open", oldDate);
        await SeedReportAsync("Open", newDate);

        var response = await _client.GetAsync("/api/Auth/reports-map?fromDate=2025-01-01");
        var reports = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        reports.Should().HaveCount(1);
        reports![0].GetProperty("createdAt").GetDateTime().Year.Should().Be(2025);
    }

    [Fact]
    public async Task GetReportsMap_FilterByToDate_ExcludesNewerReports()
    {
        var oldDate = new DateTime(2023, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var newDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        await SeedReportAsync("Open", oldDate);
        await SeedReportAsync("Open", newDate);

        var response = await _client.GetAsync("/api/Auth/reports-map?toDate=2024-01-01");
        var reports = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        reports.Should().HaveCount(1);
        reports![0].GetProperty("createdAt").GetDateTime().Year.Should().Be(2023);
    }

    [Fact]
    public async Task GetReportsMap_FilterByStatusAndDate_ReturnsCorrectSubset()
    {
        var targetDate = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var oldDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await SeedReportAsync("Open", targetDate);
        await SeedReportAsync("Completed", targetDate);
        await SeedReportAsync("Open", oldDate);

        var response = await _client.GetAsync("/api/Auth/reports-map?status=Open&fromDate=2025-01-01");
        var reports = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        reports.Should().HaveCount(1);
        reports![0].GetProperty("status").GetString().Should().Be("Open");
        reports[0].GetProperty("createdAt").GetDateTime().Year.Should().Be(2025);
    }

    [Fact]
    public async Task GetReportsMap_ReturnsOrderedByCreatedAtDescending()
    {
        var earlier = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var later = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        await SeedReportAsync("Open", earlier);
        await SeedReportAsync("Open", later);

        var response = await _client.GetAsync("/api/Auth/reports-map");
        var reports = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        reports.Should().HaveCount(2);
        var dates = reports!.Select(r => r.GetProperty("createdAt").GetDateTime()).ToList();
        dates.Should().BeInDescendingOrder();
    }
}