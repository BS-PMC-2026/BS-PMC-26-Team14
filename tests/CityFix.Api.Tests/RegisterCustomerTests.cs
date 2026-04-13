using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CityFix.Api.Tests
{
    public class RegisterCustomerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public RegisterCustomerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task RegisterCustomer_ShouldReturnSuccess_WhenDataIsValid()
        {
            var request = new
            {
                phone = "0501234567",
                fullName = "Ahmad Akhras",
                email = $"ahmad_{Guid.NewGuid()}@test.com",
                address = "Beer Sheva",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register-customer", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RegisterCustomer_ShouldReturnContent_WhenRegistrationSucceeds()
        {
            var request = new
            {
                phone = "0501234567",
                fullName = "Ahmad Akhras",
                email = $"success_{Guid.NewGuid()}@test.com",
                address = "Beer Sheva",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register-customer", request);
            var content = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task RegisterCustomer_ShouldFail_WhenRequiredFieldsAreMissing()
        {
            var request = new
            {
                phone = "",
                fullName = "",
                email = "",
                address = "",
                password = ""
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register-customer", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RegisterCustomer_ShouldFail_WhenPhoneIsMissing()
        {
            var request = new
            {
                phone = "",
                fullName = "Ahmad Akhras",
                email = $"phone_missing_{Guid.NewGuid()}@test.com",
                address = "Beer Sheva",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register-customer", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RegisterCustomer_ShouldFail_WhenFullNameIsMissing()
        {
            var request = new
            {
                phone = "0501234567",
                fullName = "",
                email = $"fullname_missing_{Guid.NewGuid()}@test.com",
                address = "Beer Sheva",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register-customer", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RegisterCustomer_ShouldFail_WhenEmailIsMissing()
        {
            var request = new
            {
                phone = "0501234567",
                fullName = "Ahmad Akhras",
                email = "",
                address = "Beer Sheva",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register-customer", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RegisterCustomer_ShouldFail_WhenAddressIsMissing()
        {
            var request = new
            {
                phone = "0501234567",
                fullName = "Ahmad Akhras",
                email = $"address_missing_{Guid.NewGuid()}@test.com",
                address = "",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register-customer", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RegisterCustomer_ShouldFail_WhenPasswordIsMissing()
        {
            var request = new
            {
                phone = "0501234567",
                fullName = "Ahmad Akhras",
                email = $"password_missing_{Guid.NewGuid()}@test.com",
                address = "Beer Sheva",
                password = ""
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register-customer", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RegisterCustomer_ShouldFail_WhenEmailFormatIsInvalid()
        {
            var request = new
            {
                phone = "0501234567",
                fullName = "Ahmad Akhras",
                email = "invalid-email-format",
                address = "Beer Sheva",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register-customer", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RegisterCustomer_ShouldFail_WhenEmailAlreadyExists()
        {
            var email = $"duplicate_{Guid.NewGuid()}@test.com";

            var request = new
            {
                phone = "0501234567",
                fullName = "Ahmad Akhras",
                email = email,
                address = "Beer Sheva",
                password = "123456"
            };

            var firstResponse = await _client.PostAsJsonAsync("/api/auth/register-customer", request);
            var secondResponse = await _client.PostAsJsonAsync("/api/auth/register-customer", request);

            firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            secondResponse.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RegisterCustomer_ShouldFail_WhenPasswordIsTooShort()
        {
            var request = new
            {
                phone = "0501234567",
                fullName = "Ahmad Akhras",
                email = $"shortpass_{Guid.NewGuid()}@test.com",
                address = "Beer Sheva",
                password = "123"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register-customer", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RegisterCustomer_ShouldFail_WhenBodyIsNull()
        {
            var response = await _client.PostAsJsonAsync<object?>("/api/auth/register-customer", null);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RegisterCustomer_ShouldHandleLongInputValues()
        {
            var request = new
            {
                phone = "0501234567",
                fullName = new string('A', 200),
                email = $"long_{Guid.NewGuid()}@test.com",
                address = new string('B', 300),
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register-customer", request);

            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.BadRequest
            );
        }
    }
}