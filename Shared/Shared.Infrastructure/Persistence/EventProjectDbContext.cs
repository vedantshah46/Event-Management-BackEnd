using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Identity;

namespace Shared.Infrastructure.Persistence;

// IdentityUserContext deliberately omits many-to-many Identity roles.
public sealed class EventProjectDbContext(DbContextOptions<EventProjectDbContext> options,
    IEnumerable<IModuleModelConfiguration>? modules = null)
    : IdentityUserContext<ApplicationUser, Guid>(options)
{
    private readonly IModuleModelConfiguration[] moduleConfigurations = modules?.ToArray() ?? [];
    public string ModuleModelKey => string.Join(";", moduleConfigurations
        .Select(module => module.GetType().AssemblyQualifiedName).Order(StringComparer.Ordinal));

    protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
        builder.ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, ModuleModelCacheKeyFactory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("dbo");
        builder.Entity<ApplicationUser>(user =>
        {
            user.ToTable("Users", "dbo", table => table.HasCheckConstraint(
                "CK_Users_PlatformRole", "[PlatformRole] IN ('Admin', 'Organizer', 'CrewMember')"));
            user.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            user.Property(x => x.Email).IsRequired();
            user.Property(x => x.NormalizedEmail).IsRequired();
            user.Property(x => x.PlatformRole).HasConversion<string>().HasMaxLength(20).IsRequired();
            user.HasIndex(x => x.NormalizedEmail).IsUnique();
        });
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", "dbo");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins", "dbo");
        // Persisted Identity tokens are not used. JWTs are issued and validated separately.
        builder.Ignore<IdentityUserToken<Guid>>();
        foreach (var module in moduleConfigurations) module.Configure(builder);
    }
}
