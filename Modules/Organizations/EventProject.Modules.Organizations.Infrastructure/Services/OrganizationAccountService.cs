using EventProject.Modules.Organizations.Core.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Shared.Core.Exceptions;
using Shared.Core.Identity;
using Shared.Infrastructure.Identity;

namespace EventProject.Modules.Organizations.Infrastructure.Services;

public sealed class OrganizationAccountService(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
    : IOrganizationAccountService
{
    public async Task<Guid> CreateOrganizerAsync(string fullName, string email, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (await users.FindByEmailAsync(email) is not null)
            throw new BusinessException(BusinessError.Conflict, "An account with this email already exists.");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), FullName = fullName, Email = email, UserName = email,
            PlatformRole = PlatformRole.Organizer, IsActive = true, LockoutEnabled = true
        };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var duplicate = result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName");
            throw new BusinessException(duplicate ? BusinessError.Conflict : BusinessError.Validation,
                duplicate ? "An account with this email already exists."
                    : "Account validation failed: " + string.Join(" ", result.Errors.Select(error => error.Description)));
        }
        cancellationToken.ThrowIfCancellationRequested();
        return user.Id;
    }

    public async Task<AccountIdentity?> AuthenticateOrganizerAsync(string email, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(email);
        if (user is null || !user.IsActive || user.PlatformRole != PlatformRole.Organizer) return null;
        var result = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        cancellationToken.ThrowIfCancellationRequested();
        return result.Succeeded ? new AccountIdentity(user.Id, user.PlatformRole, user.SecurityStamp!) : null;
    }
}
