using Microsoft.EntityFrameworkCore;
using Shared.Core.Identity;
using Shared.Infrastructure.Persistence;

namespace EventProject.Modules.Identity.Infrastructure.Services;

public sealed class AccountIdentityStore(EventProjectDbContext context) : IAccountIdentityStore
{
    public async Task<AccountIdentity?> FindActiveAsync(Guid id, CancellationToken cancellationToken) =>
        await context.Users.AsNoTracking().Where(user => user.Id == id && user.IsActive)
            .Select(user => new AccountIdentity(user.Id, user.PlatformRole, user.SecurityStamp!))
            .SingleOrDefaultAsync(cancellationToken);
}
