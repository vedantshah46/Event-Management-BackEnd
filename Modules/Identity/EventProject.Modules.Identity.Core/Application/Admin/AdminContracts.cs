using System.ComponentModel.DataAnnotations;

namespace EventProject.Modules.Identity.Core.Application.Admin;

public sealed class AdminLoginRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(256)]
    public string Password { get; init; } = string.Empty;
}

public sealed record AdminProfileResponse(Guid Id, string FullName, string Email);
public sealed record AdminLoginResponse(string AccessToken, DateTimeOffset ExpiresAtUtc,
    AdminProfileResponse Admin, string TokenType = "Bearer");

// Internal application contract; never returned by a controller.
public sealed record AdminAccount(Guid Id, string FullName, string Email, string SecurityStamp);
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);

public interface IAdminAccountStore
{
    Task<AdminAccount?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken);
    Task<AdminAccount?> FindActiveAsync(Guid id, CancellationToken cancellationToken);
}

public interface IAccessTokenIssuer
{
    AccessToken Issue(AdminAccount account);
}
