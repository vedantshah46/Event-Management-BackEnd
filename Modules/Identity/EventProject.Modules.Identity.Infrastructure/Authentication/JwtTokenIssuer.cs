using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventProject.Modules.Identity.Core.Application.Admin;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Core.Identity;

namespace EventProject.Modules.Identity.Infrastructure.Authentication;

public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider clock) : IAccessTokenIssuer, IAccountTokenIssuer
{
    public AccessToken Issue(AdminAccount account)
    {
        var token = Issue(new AccountIdentity(account.Id, PlatformRole.Admin, account.SecurityStamp));
        return new AccessToken(token.Value, token.ExpiresAtUtc);
    }

    public AccountAccessToken Issue(AccountIdentity account)
    {
        if (!Enum.IsDefined(account.Role)) throw new ArgumentException("Invalid platform role.", nameof(account));
        var settings = options.Value;
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(settings.AccessTokenMinutes);
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("role", account.Role.ToString()),
            new("security_stamp", account.SecurityStamp)
        ];
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims,
            now.UtcDateTime, expires.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                SecurityAlgorithms.HmacSha256));
        return new AccountAccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
