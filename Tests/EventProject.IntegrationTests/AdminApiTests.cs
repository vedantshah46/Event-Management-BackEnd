using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EventProject.Modules.Identity.Core.Application.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace EventProject.IntegrationTests;

public sealed class AdminApiTests
{
    [Fact]
    public async Task ProtectedProfileRequiresAuthentication()
    {
        await using var factory = new AdminApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/me")).StatusCode);
    }

    [Fact]
    public async Task LoginRejectsMalformedAndIncorrectCredentials()
    {
        await using var factory = new AdminApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var malformed = await client.PostAsJsonAsync("/api/admin/auth/login", new { Email = "invalid", Password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        var rejected = await client.PostAsJsonAsync("/api/admin/auth/login", new { Email = "admin@example.test", Password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
    }

    [Fact]
    public async Task LoginAllowsProfileButDeactivationRevokesExistingToken()
    {
        await using var factory = new AdminApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var login = await client.PostAsJsonAsync("/api/admin/auth/login", new { Email = "admin@example.test", Password = "test-password" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var response = await login.Content.ReadFromJsonAsync<AdminLoginResponse>();
        Assert.NotNull(response);
        Assert.DoesNotContain("test-stamp", System.Text.Json.JsonSerializer.Serialize(response.Admin));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", response.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin/me")).StatusCode);
        factory.Accounts.Active = false;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/me")).StatusCode);
    }

    [Fact]
    public async Task SecurityStampChangeRevokesExistingToken()
    {
        await using var factory = new AdminApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var login = await client.PostAsJsonAsync("/api/admin/auth/login", new { Email = "admin@example.test", Password = "test-password" });
        var response = await login.Content.ReadFromJsonAsync<AdminLoginResponse>();
        Assert.NotNull(response);
        factory.Accounts.Stamp = "changed-stamp";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", response.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/me")).StatusCode);
    }

    [Fact]
    public async Task DevelopmentSwaggerServesUiAndDescribesAdminBearerAuthentication()
    {
        await using var factory = new AdminApiFactory("Development");
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal("bearer", root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString());
        Assert.True(root.GetProperty("paths").TryGetProperty("/api/admin/me", out _));
        Assert.Equal(0, root.GetProperty("paths").GetProperty("/api/admin/auth/login").GetProperty("post").GetProperty("security").GetArrayLength());
    }

    [Fact]
    public async Task SwaggerIsUnavailableOutsideDevelopment()
    {
        await using var factory = new AdminApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }
    private sealed class AdminApiFactory(string environment = "Testing") : WebApplicationFactory<Program>
    {
        public FakeAccounts Accounts { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=NeverConnected;Trusted_Connection=True",
                    ["Jwt:Issuer"] = "test-issuer",
                    ["Jwt:Audience"] = "test-admin",
                    ["Jwt:SigningKey"] = "TEST-ONLY-NOT-FOR-APP-0123456789abcdefghijklmnopqrstuvwxyz-0123456789"
                }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAdminAccountStore>();
                services.AddSingleton<IAdminAccountStore>(Accounts);
            });
        }
    }

    // Exercises real routing, DTO validation, JWT middleware and authorization, with no database access.
    private sealed class FakeAccounts : IAdminAccountStore
    {
        private readonly Guid id = Guid.NewGuid();
        public bool Active { get; set; } = true;
        public string Stamp { get; set; } = "test-stamp";
        private AdminAccount Account => new(id, "Admin", "admin@example.test", Stamp);
        public Task<AdminAccount?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken) =>
            Task.FromResult<AdminAccount?>(Active && email == "admin@example.test" && password == "test-password" ? Account : null);
        public Task<AdminAccount?> FindActiveAsync(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult<AdminAccount?>(Active && accountId == id ? Account : null);
    }
}
