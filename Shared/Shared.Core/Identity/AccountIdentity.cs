namespace Shared.Core.Identity;

public sealed record AccountIdentity(Guid Id, PlatformRole Role, string SecurityStamp);
public sealed record AccountAccessToken(string Value, DateTimeOffset ExpiresAtUtc);

public interface IAccountTokenIssuer
{
    AccountAccessToken Issue(AccountIdentity account);
}

public interface IAccountIdentityStore
{
    Task<AccountIdentity?> FindActiveAsync(Guid id, CancellationToken cancellationToken);
}
