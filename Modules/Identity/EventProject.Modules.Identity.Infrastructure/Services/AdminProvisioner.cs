using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Shared.Core.Identity;
using Shared.Infrastructure.Identity;

namespace EventProject.Modules.Identity.Infrastructure.Services;

public sealed class AdminProvisioner(UserManager<ApplicationUser> users)
{
    public async Task CreateAsync(string email, string fullName, string password)
    {
        email = email.Trim();
        fullName = fullName.Trim();
        if (email.Length > 256 || !new EmailAddressAttribute().IsValid(email)
            || fullName.Length is < 1 or > 150 || password.Length is < 12 or > 256)
            throw new InvalidOperationException("Admin provisioning values are invalid. Check the setup documentation.");

        // Never promote an existing account or overwrite its password.
        if (await users.FindByEmailAsync(email) is not null)
            throw new InvalidOperationException("An account with this email already exists. No changes made.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), UserName = email, Email = email, FullName = fullName,
            PlatformRole = PlatformRole.Admin, IsActive = true, LockoutEnabled = true
        };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException("Admin creation failed: " + string.Join(", ", result.Errors.Select(x => x.Code)));
    }
}
