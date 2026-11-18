using EventProject.Modules.Identity.Core.Application.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Identity;
using Shared.Infrastructure.Identity;

namespace EventProject.Modules.Identity.Infrastructure.Services;

public sealed class AdminAccountStore(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
    : IAdminAccountStore
{
    public async Task<AdminAccount?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(email);
        if (user is null || !user.IsActive || user.PlatformRole != PlatformRole.Admin) return null;

        // Uses Identity password hashing, failed-attempt tracking and lockout.
        var result = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        cancellationToken.ThrowIfCancellationRequested();
        return result.Succeeded ? ToAccount(user) : null;
    }

    public async Task<AdminAccount?> FindActiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await users.Users.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == id && x.IsActive && x.PlatformRole == PlatformRole.Admin, cancellationToken);
        return user is null ? null : ToAccount(user);
    }

    private static AdminAccount ToAccount(ApplicationUser user) =>
        new(user.Id, user.FullName, user.Email!, user.SecurityStamp!);
}
