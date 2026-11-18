using System.IdentityModel.Tokens.Jwt;
using System.Text;
using EventProject.Modules.Identity.Core.Application.Admin;
using EventProject.Modules.Identity.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace EventProject.UnitTests;

public sealed class JwtTests
{
    private const string Key = "TEST-ONLY-SIGNING-KEY-DO-NOT-USE-IN-APP-0123456789abcdefghijklmnopqrstuvwxyz";

    [Fact]
    public void IssuedTokenHasExpectedIdentityAndRejectsWrongAudience()
    {
        var settings = new JwtOptions { Issuer = "test-issuer", Audience = "test-admin", SigningKey = Key };
        var account = new AdminAccount(Guid.NewGuid(), "Admin", "admin@example.test", "stamp");
        var issued = new JwtTokenIssuer(Options.Create(settings), TimeProvider.System).Issue(account);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = settings.Issuer,
            ValidateAudience = true, ValidAudience = settings.Audience,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256], RoleClaimType = "role"
        };
        var principal = handler.ValidateToken(issued.Value, parameters, out _);
        Assert.True(principal.IsInRole("Admin"));
        Assert.Equal(account.Id.ToString(), principal.FindFirst("sub")!.Value);
        parameters.ValidAudience = "different-audience";
        Assert.Throws<SecurityTokenInvalidAudienceException>(() => handler.ValidateToken(issued.Value, parameters, out _));
        parameters.ValidAudience = settings.Audience;
        parameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('X', 64)));
        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(issued.Value, parameters, out _));
    }
}
