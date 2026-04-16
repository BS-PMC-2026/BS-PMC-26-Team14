using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using CityFix.Api.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CityFix.Api.Tests
{
    public class AdminLoginTests : IClassFixture<AdminLoginTests.InMemoryFactory>
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
                        options.UseInMemoryDatabase("AdminLoginTestDb"));
                });
            }
        }

        private readonly HttpClient _client;
        private readonly InMemoryFactory _factory;

        public AdminLoginTests(InMemoryFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private async Task SeedAdminAsync(string email, string password, string fullName = "Test Admin")
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var adminType = db.GetType().Assembly.GetTypes().FirstOrDefault(t => t.Name == "Admin");
            adminType.Should().NotBeNull("צריך להיות מודל בשם Admin בפרויקט");

            var admin = Activator.CreateInstance(adminType!);
            admin.Should().NotBeNull();

            var hash = BuildProjectPasswordHash(password);

            SetPropertyIfExists(adminType!, admin!, "FullName", fullName);
            SetPropertyIfExists(adminType!, admin!, "Name", fullName);
            SetPropertyIfExists(adminType!, admin!, "UserName", fullName);

            SetPropertyIfExists(adminType!, admin!, "Email", email);
            SetPropertyIfExists(adminType!, admin!, "UserEmail", email);
            SetPropertyIfExists(adminType!, admin!, "Username", email);

            // שמירה של כל הווריאציות האפשריות
            SetPropertyIfExists(adminType!, admin!, "Password", password);
            SetPropertyIfExists(adminType!, admin!, "UserPassword", password);
            SetPropertyIfExists(adminType!, admin!, "PlainPassword", password);

            SetPropertyIfExists(adminType!, admin!, "PasswordHash", hash);
            SetPropertyIfExists(adminType!, admin!, "HashedPassword", hash);
            SetPropertyIfExists(adminType!, admin!, "PassHash", hash);

            SetPropertyIfExists(adminType!, admin!, "Role", "Admin");
            SetPropertyIfExists(adminType!, admin!, "IsApproved", true);
            SetPropertyIfExists(adminType!, admin!, "Approved", true);
            SetPropertyIfExists(adminType!, admin!, "IsActive", true);
            SetPropertyIfExists(adminType!, admin!, "Active", true);
            SetPropertyIfExists(adminType!, admin!, "Status", "Approved");
            SetPropertyIfExists(adminType!, admin!, "AccountStatus", "Approved");

            db.Add(admin!);
            await db.SaveChangesAsync();
        }

        private static string BuildProjectPasswordHash(string password)
        {
            var assembly = typeof(ApplicationDbContext).Assembly;

            // מחפש כל מתודה בשם HashPassword(string) בפרויקט
            foreach (var type in assembly.GetTypes())
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                    .Where(m =>
                        m.Name == "HashPassword" &&
                        m.ReturnType == typeof(string) &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(string));

                foreach (var method in methods)
                {
                    try
                    {
                        object? instance = null;
                        if (!method.IsStatic)
                        {
                            try
                            {
                                instance = Activator.CreateInstance(type);
                            }
                            catch
                            {
                                continue;
                            }
                        }

                        var result = method.Invoke(instance, new object[] { password }) as string;
                        if (!string.IsNullOrWhiteSpace(result))
                            return result;
                    }
                    catch
                    {
                    }
                }
            }

            // fallback
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private static void SetPropertyIfExists(Type type, object obj, string propertyName, object value)
        {
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || !prop.CanWrite)
                return;

            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            if (targetType == typeof(string) && value is string s)
            {
                prop.SetValue(obj, s);
                return;
            }

            if (targetType == typeof(bool) && value is bool b)
            {
                prop.SetValue(obj, b);
            }
        }

        [Fact]
        public async Task LoginAdmin_ShouldReturnSuccess_WhenCredentialsAreCorrect()
        {
            var email = $"admin_{Guid.NewGuid()}@test.com";
            var password = "123456";

            await SeedAdminAsync(email, password);

            var request = new
            {
                email,
                password
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-admin", request);
            var content = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK, content);
        }

        [Fact]
        public async Task LoginAdmin_ShouldReturnContent_WhenLoginSucceeds()
        {
            var email = $"admin_content_{Guid.NewGuid()}@test.com";
            var password = "123456";

            await SeedAdminAsync(email, password);

            var request = new
            {
                email,
                password
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-admin", request);
            var content = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK, content);
            content.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task LoginAdmin_ShouldFail_WhenEmailIsMissing()
        {
            var request = new
            {
                email = "",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-admin", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginAdmin_ShouldFail_WhenPasswordIsMissing()
        {
            var request = new
            {
                email = "admin@test.com",
                password = ""
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-admin", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginAdmin_ShouldFail_WhenBothFieldsAreMissing()
        {
            var request = new
            {
                email = "",
                password = ""
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-admin", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginAdmin_ShouldFail_WhenEmailDoesNotExist()
        {
            var request = new
            {
                email = $"notfound_{Guid.NewGuid()}@test.com",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-admin", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginAdmin_ShouldFail_WhenPasswordIsIncorrect()
        {
            var email = $"wrongpass_{Guid.NewGuid()}@test.com";
            await SeedAdminAsync(email, "123456");

            var request = new
            {
                email,
                password = "wrong123"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-admin", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginAdmin_ShouldFail_WhenEmailFormatIsInvalid()
        {
            var request = new
            {
                email = "invalid-email",
                password = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-admin", request);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginAdmin_ShouldFail_WhenBodyIsNull()
        {
            var response = await _client.PostAsJsonAsync<object?>("/api/auth/login-admin", null);

            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LoginAdmin_ShouldHandleLongInputValues()
        {
            var request = new
            {
                email = new string('a', 200) + "@test.com",
                password = new string('b', 300)
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login-admin", request);

            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.BadRequest,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.NotFound
            );
        }
    }
}