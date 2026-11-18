using Microsoft.AspNetCore.Identity;
using Shared.Core.Identity;

namespace Shared.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public PlatformRole PlatformRole { get; set; }
    public bool IsFirstLogin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
