using System.ComponentModel.DataAnnotations;

namespace EventProject.Modules.Identity.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;
    [Required, MinLength(64)] public string SigningKey { get; set; } = string.Empty;
    [Range(1, 30)] public int AccessTokenMinutes { get; set; } = 15;
}
