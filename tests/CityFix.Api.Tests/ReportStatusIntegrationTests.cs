using System.Net;
using System.Net.Http.Json;
using CityFix.Api.Data;
using CityFix.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CityFix.Api.Tests;

public class ReportStatusIntegrationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReportStatusIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApprovedWorker_Can_Update_Report_Status_To_Completed()
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var email = $"worker_{Guid.NewGuid()}@test.com";

        var worker = new Worker
        {
            FullName = "Test Worker",
            Email = email,
            Phone = "0500000000",
            NationalId = "123456789",
            Department = "Roads",
            Municipality = "Test City",
            PasswordHash = "hash",
            ApprovalStatus = "Approved"
        };

        var report = new Report
        {
            CustomerEmail = "customer@test.com",
            Category = "Road Damage",
            Priority = "High",
            Description = "Test report",
            Notes = "",
            Location = "",
            ImageBase64 = "",
            Latitude = 32.1,
            Longitude = 35.1,
            Status = "In Treatment",
            AssignedWorkerEmail = email,
            CreatedAt = DateTime.UtcNow
        };

        db.Workers.Add(worker);
        db.Reports.Add(report);

        await db.SaveChangesAsync();

        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/Auth/update-report-status/{report.Id}",
            new
            {
                workerEmail = email,
                newStatus = "Completed"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        db.ChangeTracker.Clear();

        var updatedReport = await db.Reports
            .AsNoTracking()
            .FirstAsync(r => r.Id == report.Id);

        Assert.Equal("Completed", updatedReport.Status);
    }

    [Fact]
    public async Task Updating_Status_Creates_History_Record()
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var email = $"history_{Guid.NewGuid()}@test.com";

        var worker = new Worker
        {
            FullName = "History Worker",
            Email = email,
            Phone = "0500000000",
            NationalId = "987654321",
            Department = "Roads",
            Municipality = "Test City",
            PasswordHash = "hash",
            ApprovalStatus = "Approved"
        };

        var report = new Report
        {
            CustomerEmail = "customer@test.com",
            Category = "Road Damage",
            Priority = "High",
            Description = "Test report",
            Notes = "",
            Location = "",
            ImageBase64 = "",
            Latitude = 32.1,
            Longitude = 35.1,
            Status = "Open",
            AssignedWorkerEmail = email,
            CreatedAt = DateTime.UtcNow
        };

        db.Workers.Add(worker);
        db.Reports.Add(report);

        await db.SaveChangesAsync();

        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/Auth/update-report-status/{report.Id}",
            new
            {
                workerEmail = email,
                newStatus = "In Treatment"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        db.ChangeTracker.Clear();

        var history = await db.ReportStatusHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.ReportId == report.Id);

        Assert.NotNull(history);

        Assert.Equal("Open", history!.OldStatus);

        Assert.Equal("In Treatment", history.NewStatus);

        Assert.Equal(email, history.ChangedByWorkerEmail);
    }

    [Fact]
    public async Task Invalid_Status_Returns_BadRequest()
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var email = $"invalid_{Guid.NewGuid()}@test.com";

        var worker = new Worker
        {
            FullName = "Invalid Worker",
            Email = email,
            Phone = "0500000000",
            NationalId = "111222333",
            Department = "Roads",
            Municipality = "Test City",
            PasswordHash = "hash",
            ApprovalStatus = "Approved"
        };

        var report = new Report
        {
            CustomerEmail = "customer@test.com",
            Category = "Road Damage",
            Priority = "High",
            Description = "Test report",
            Notes = "",
            Location = "",
            ImageBase64 = "",
            Latitude = 32.1,
            Longitude = 35.1,
            Status = "Open",
            AssignedWorkerEmail = email,
            CreatedAt = DateTime.UtcNow
        };

        db.Workers.Add(worker);
        db.Reports.Add(report);

        await db.SaveChangesAsync();

        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/Auth/update-report-status/{report.Id}",
            new
            {
                workerEmail = email,
                newStatus = "Closed"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}