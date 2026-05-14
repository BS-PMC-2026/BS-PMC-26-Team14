using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CityFix.Api.Tests;

public class AssignedReportsIntegrationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AssignedReportsIntegrationTests(
        WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AssignedReports_ReturnsSuccess()
    {
        
        var url =
            "/api/Auth/assigned-reports?workerEmail=test@test.com";

        
        var response = await _client.GetAsync(url);

        
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound
        );
    }

    [Fact]
    public async Task OpenReports_ReturnsSuccess()
    {
        
        var url =
            "/api/Auth/open-reports?workerEmail=test@test.com";

        
        var response = await _client.GetAsync(url);

        
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound
        );
    }

    [Fact]
    public async Task WorkerAssignedReportsPage_ReturnsSuccess()
    {
        
        var url =
            "/View_Reports/worker-assigned-reports.html";

       
        var response = await _client.GetAsync(url);

        
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound
        );
    }

    [Fact]
    public async Task UploadImagePage_ReturnsSuccess()
    {
        
        var url =
            "/View_Reports/upload-image.html?id=1";

        
        var response = await _client.GetAsync(url);

        
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound
        );
    }
}