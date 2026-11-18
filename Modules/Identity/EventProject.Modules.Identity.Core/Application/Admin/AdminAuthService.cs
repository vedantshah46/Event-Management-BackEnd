namespace EventProject.Modules.Identity.Core.Application.Admin;

public sealed class AdminAuthService(IAdminAccountStore accounts, IAccessTokenIssuer tokens)
{
    public async Task<AdminLoginResponse?> LoginAsync(AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var account = await accounts.AuthenticateAsync(request.Email.Trim(), request.Password, cancellationToken);
        if (account is null) return null;
        var token = tokens.Issue(account);
        return new AdminLoginResponse(token.Value, token.ExpiresAtUtc, ToProfile(account));
    }

    public async Task<AdminProfileResponse?> GetProfileAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await accounts.FindActiveAsync(id, cancellationToken);
        return account is null ? null : ToProfile(account);
    }

    private static AdminProfileResponse ToProfile(AdminAccount account) =>
        new(account.Id, account.FullName, account.Email);
}
