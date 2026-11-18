using EventProject.Modules.Identity.Core.Application.Admin;
using Xunit;

namespace EventProject.UnitTests;

public sealed class AdminAuthTests
{
    [Fact]
    public async Task RejectedCredentialsNeverIssueToken()
    {
        var issuer = new RecordingIssuer();
        var service = new AdminAuthService(new StubAccounts(null), issuer);
        var result = await service.LoginAsync(new AdminLoginRequest { Email = "admin@example.test", Password = "bad" }, default);
        Assert.Null(result);
        Assert.Equal(0, issuer.CallCount);
    }

    [Fact]
    public async Task SuccessfulLoginReturnsOnlyPublicProfileAndToken()
    {
        var account = new AdminAccount(Guid.NewGuid(), "Admin", "admin@example.test", "private-stamp");
        var store = new StubAccounts(account);
        var issuer = new RecordingIssuer();
        var service = new AdminAuthService(store, issuer);
        var response = await service.LoginAsync(new AdminLoginRequest
        { Email = "  admin@example.test  ", Password = "test-password" }, default);
        Assert.NotNull(response);
        Assert.Equal("admin@example.test", store.LastEmail);
        Assert.Equal(account.Id, response.Admin.Id);
        Assert.Equal(1, issuer.CallCount);
        Assert.DoesNotContain("private-stamp", System.Text.Json.JsonSerializer.Serialize(response));
    }

    private sealed class StubAccounts(AdminAccount? account) : IAdminAccountStore
    {
        public string? LastEmail { get; private set; }
        public Task<AdminAccount?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken)
        {
            LastEmail = email;
            return Task.FromResult(account);
        }
        public Task<AdminAccount?> FindActiveAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(account);
    }

    private sealed class RecordingIssuer : IAccessTokenIssuer
    {
        public int CallCount { get; private set; }
        public AccessToken Issue(AdminAccount account)
        {
            CallCount++;
            return new AccessToken("test-token", DateTimeOffset.UtcNow.AddMinutes(15));
        }
    }
}
