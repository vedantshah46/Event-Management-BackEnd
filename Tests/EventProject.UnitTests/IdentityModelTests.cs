using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Identity;
using Shared.Infrastructure.Persistence;
using Xunit;

namespace EventProject.UnitTests;

public sealed class IdentityModelTests
{
    [Fact]
    public void ModelRequiresUniqueEmailAndSingleRoleWithoutRoleJoinTables()
    {
        using var context = new EventProjectDbContext(new DbContextOptionsBuilder<EventProjectDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ModelOnly;Trusted_Connection=True").Options);
        var entity = context.Model.FindEntityType(typeof(ApplicationUser))!;
        Assert.Equal("Users", entity.GetTableName());
        Assert.Equal("dbo", context.Model.GetDefaultSchema());
        Assert.All(context.Model.GetEntityTypes().Where(type => type.GetTableName() is not null),
            type => Assert.Equal("dbo", type.GetSchema()));
        Assert.False(entity.FindProperty(nameof(ApplicationUser.PlatformRole))!.IsNullable);
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique
            && index.Properties.Count == 1 && index.Properties[0].Name == nameof(ApplicationUser.NormalizedEmail));
        Assert.DoesNotContain(context.Model.GetEntityTypes(), type => type.ClrType.Name.StartsWith("IdentityUserRole", StringComparison.Ordinal));
        Assert.Null(context.Model.FindEntityType(typeof(Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>)));
        // Model inspection only: no connection opened and no schema created.
    }
}
