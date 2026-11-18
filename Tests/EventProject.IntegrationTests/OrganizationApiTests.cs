using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using EventProject.Modules.Identity.Core.Application.Admin;
using EventProject.Modules.Organizations.Core.Application.DTOs;
using EventProject.Modules.Organizations.Core.Application.Interfaces;
using EventProject.Modules.Organizations.Core.Domain.Entities;
using EventProject.TestSupport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Core.Identity;
using Xunit;

namespace EventProject.IntegrationTests;

public sealed class OrganizationApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task AnonymousRequestsCannotReadOrganizations()
    {
        await using var factory = new OrganizationApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/organizations/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/organizations")).StatusCode);
    }

    [Fact]
    public async Task RegistrationLoginAdminApprovalAndCurrentProfileWorkEndToEndWithoutDatabase()
    {
        await using var factory = new OrganizationApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var registration = await Register(client);
        Assert.Equal(OrganizationStatus.PendingApproval, registration.EventOrganizationStatusId);
        var token = await Login(client, "owner@example.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/organizations")).StatusCode);
        var own = await client.GetFromJsonAsync<OrganizationResponse>("/api/organizations/me", Json);
        Assert.Equal(registration.OrganizationId, own!.Id);
        var membership = await client.GetFromJsonAsync<MembershipResponse>("/api/organizations/me/membership", Json);
        Assert.Equal("OWNER", membership!.Role);
        using (var scope = factory.Services.CreateScope())
        {
            var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
            var principal = Principal(registration.UserId);
            Assert.False((await authorization.AuthorizeAsync(principal, null, "ApprovedOrganization")).Succeeded);
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AdminToken(client));
        var list = await client.GetFromJsonAsync<OrganizationPage>("/api/admin/organizations?eventOrganizationStatusId=1", Json);
        Assert.Single(list!.Items);
        var approved = await client.PostAsync($"/api/admin/organizations/{registration.OrganizationId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        Assert.Equal(OrganizationStatus.Approved, (await approved.Content.ReadFromJsonAsync<OrganizationResponse>(Json))!.EventOrganizationStatusId);
        Assert.Equal(OrganizationFixture.AdminId, factory.Fixture.Repository.Histories.Last().ChangedByUserId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(OrganizationStatus.Approved, (await client.GetFromJsonAsync<OrganizationResponse>("/api/organizations/me", Json))!.EventOrganizationStatusId);
        using var approvedScope = factory.Services.CreateScope();
        Assert.True((await approvedScope.ServiceProvider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(Principal(registration.UserId), null, "ApprovedOrganization")).Succeeded);
    }

    [Fact]
    public async Task RegistrationValidationAndDuplicateEmailProduceConsistentErrors()
    {
        await using var factory = new OrganizationApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/organizations/register", new { Email = "invalid" })).StatusCode);
        await Register(client);
        var duplicate = await client.PostAsJsonAsync("/api/organizations/register", OrganizationFixture.Request());
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("application/problem+json", duplicate.Content.Headers.ContentType!.MediaType);
        Assert.Single(factory.Fixture.Repository.Organizations);
    }

    [Fact]
    public async Task OrganizerCannotReviewOtherCompanyOrOverrideOwnOrganizationAndInactiveMembershipLosesAccess()
    {
        await using var factory = new OrganizationApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var first = await Register(client);
        var second = await Register(client, "second@example.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await Login(client, "owner@example.test"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/admin/organizations/{second.OrganizationId}/approve", null)).StatusCode);
        var own = await client.GetFromJsonAsync<OrganizationResponse>($"/api/organizations/me?id={second.OrganizationId}", Json);
        Assert.Equal(first.OrganizationId, own!.Id);
        factory.Fixture.Repository.Memberships.Single(row => row.UserId == first.UserId).IsActive = false;
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/organizations/me")).StatusCode);
    }

    [Fact]
    public async Task RejectionRequiresReasonAndCannotBeRepeated()
    {
        await using var factory = new OrganizationApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var registration = await Register(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AdminToken(client));
        var path = $"/api/admin/organizations/{registration.OrganizationId}/reject";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path, new { Reason = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(path, new { Reason = "Company details require correction." })).StatusCode);
        Assert.Equal("Company details require correction.", factory.Fixture.Repository.Histories.Last().Reason);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/admin/organizations/{registration.OrganizationId}/approve", null)).StatusCode);
    }

    [Fact]
    public async Task ChangingPlatformRoleInvalidatesExistingOrganizerToken()
    {
        await using var factory = new OrganizationApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var registration = await Register(client);
        var token = await Login(client, "owner@example.test");
        var account = factory.Fixture.Accounts.Accounts[registration.UserId];
        factory.Fixture.Accounts.Accounts[registration.UserId] =
            (account.Email, account.Password, account.Identity with { Role = PlatformRole.Admin });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/organizations/me")).StatusCode);
    }

    private static ClaimsPrincipal Principal(Guid id) => new(new ClaimsIdentity(
        [new Claim("sub", id.ToString()), new Claim(ClaimTypes.Role, "Organizer")], "test"));

    private static async Task<RegistrationResponse> Register(HttpClient client, string email = "owner@example.test")
    {
        var response = await client.PostAsJsonAsync("/api/organizations/register", OrganizationFixture.Request(email));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RegistrationResponse>(Json))!;
    }
    private static async Task<string> Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/organizer/auth/login", new { Email = email, Password = "TestOnly@Password123" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrganizerLoginResponse>(Json))!.AccessToken;
    }
    private static async Task<string> AdminToken(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/admin/auth/login", new { Email = "admin@example.test", Password = "TestOnly@Password123" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AdminLoginResponse>())!.AccessToken;
    }

    private sealed class OrganizationApiFactory : WebApplicationFactory<Program>
    {
        public OrganizationFixture Fixture { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=NeverConnected;Trusted_Connection=True",
                ["Jwt:Issuer"] = "test-issuer", ["Jwt:Audience"] = "test-api",
                ["Jwt:SigningKey"] = "TEST-ONLY-NOT-FOR-APP-0123456789abcdefghijklmnopqrstuvwxyz-0123456789"
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IOrganizationRepository>();
                services.RemoveAll<IOrganizationAccountService>();
                services.RemoveAll<IOrganizationTransaction>();
                services.RemoveAll<IAccountIdentityStore>();
                services.RemoveAll<IAdminAccountStore>();
                services.AddSingleton<IOrganizationRepository>(Fixture.Repository);
                services.AddSingleton<IOrganizationAccountService>(Fixture.Accounts);
                services.AddSingleton<IOrganizationTransaction>(Fixture.Transaction);
                services.AddSingleton<IAccountIdentityStore>(Fixture.Accounts);
                services.AddSingleton<IAdminAccountStore>(Fixture.Accounts);
            });
        }
    }
}
