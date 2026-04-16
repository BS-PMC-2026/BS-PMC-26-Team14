using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CityFix.Api.Data;
using CityFix.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CityFix.Api.Tests
{
    public class CustomerLoginTests : IClassFixture<CustomerLoginTests.InMemoryFactory>
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
                        options.UseInMemoryDatabase("LoginTestDb"));
                });
            }
        }

        private readonly HttpClient _client;

        public CustomerLoginTests(InMemoryFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task SeedCustomerAsync(string email, string password, string fullName = "Test Customer")
        {
            var registerRequest = new
            {
                phone = "0501234567",
                fullName = fullName,
                email = email,
                address = "Beer Sheva",
                password = password
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register-customer", registerRequest);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginCustomer_ShouldReturnSuccess_WhenCredentialsAreCorrect()
        {
            var email = $"login_{Guid.NewGuid()}@test.com";
            var password = "123456";

            await SeedCustomerAsync(email, password);

            var request = new
            {
                email,
                password
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-customer", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginCustomer_ShouldReturnContent_WhenLoginSucceeds()
        {
            var email = $"content_{Guid.NewGuid()}@test.com";
            var password = "123456";

            await SeedCustomerAsync(email, password);

            var request = new
            {
                email,
                password
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-customer", request);
            var content = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task LoginCustomer_ShouldFail_WhenEmailIsMissing()
        {
            var request = new
            {
                email = "",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-customer", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginCustomer_ShouldFail_WhenPasswordIsMissing()
        {
            var request = new
            {
                email = "customer@test.com",
                password = ""
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-customer", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginCustomer_ShouldFail_WhenBothFieldsAreMissing()
        {
            var request = new
            {
                email = "",
                password = ""
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-customer", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginCustomer_ShouldFail_WhenEmailDoesNotExist()
        {
            var request = new
            {
                email = $"notfound_{Guid.NewGuid()}@test.com",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-customer", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginCustomer_ShouldFail_WhenPasswordIsIncorrect()
        {
            var email = $"wrongpass_{Guid.NewGuid()}@test.com";
            await SeedCustomerAsync(email, "123456");

            var request = new
            {
                email,
                password = "wrong123"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-customer", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginCustomer_ShouldFail_WhenEmailFormatIsInvalid()
        {
            var request = new
            {
                email = "invalid-email",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-customer", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginCustomer_ShouldFail_WhenBodyIsNull()
        {
            var response = await _client.PostAsJsonAsync<object?>("/api/auth/login-customer", null);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginCustomer_ShouldHandleLongInputValues()
        {
            var request = new
            {
                email = new string('a', 200) + "@test.com",
                password = new string('b', 300)
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-customer", request);

            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.BadRequest,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.NotFound
            );
        }
    }
}