using EventProject.Modules.Organizations.Core.Application.Interfaces;
using EventProject.Modules.Organizations.Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Exceptions;
using Shared.Core.Identity;
using Shared.Infrastructure.Identity;
using Shared.Infrastructure.Persistence;

namespace EventProject.Modules.Organizations.Infrastructure.Services;

public sealed class OrganizationStaffService(EventProjectDbContext db, UserManager<ApplicationUser> users,
    IOrganizationRepository organizations, IOrganizationTransaction transaction) : IOrganizationStaffService
{
    private async Task<Guid> OwnerOrganization(Guid ownerId, CancellationToken ct)
    {
        var access = await organizations.FindAccessAsync(ownerId, ct);
        if (access is null || access.Membership.Role != "OWNER" || access.Membership.IsFirstLogin
            || access.Organization.EventOrganizationStatusId != OrganizationStatus.Approved)
            throw new BusinessException(BusinessError.Forbidden, "An approved organization's Owner is required.");
        return access.Organization.Id;
    }

    public async Task<StaffResponse[]> ListAsync(Guid ownerId, CancellationToken ct)
    {
        var organizationId = await OwnerOrganization(ownerId, ct);
        return await (from member in db.Set<OrganizationMembership>().AsNoTracking()
            join user in db.Users.AsNoTracking() on member.UserId equals user.Id
            join role in db.Set<OrganizationRole>().AsNoTracking() on member.OrganizationRoleId equals role.Id
            where member.EventOrganizationId == organizationId
            orderby user.FullName, member.Id
            select new StaffResponse(member.Id, user.FullName, user.Email!, user.PhoneNumber,
                role.Id, role.Name, member.IsActive, user.IsFirstLogin, role.Code == "OWNER")).ToArrayAsync(ct);
    }

    public async Task<StaffRoleResponse[]> RolesAsync(Guid ownerId, CancellationToken ct)
    {
        await OwnerOrganization(ownerId, ct);
        return await db.Set<OrganizationRole>().AsNoTracking().Where(x => x.IsActive && x.Code != "OWNER")
            .OrderBy(x => x.Id).Select(x => new StaffRoleResponse(x.Id, x.Name)).ToArrayAsync(ct);
    }

    private async Task<OrganizationRole> AssignableRole(int id, CancellationToken ct) =>
        await db.Set<OrganizationRole>().SingleOrDefaultAsync(x => x.Id == id && x.IsActive && x.Code != "OWNER", ct)
            ?? throw new BusinessException(BusinessError.Validation, "Select an active staff role.");

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new BusinessException(result.Errors.Any(x => x.Code is "DuplicateEmail" or "DuplicateUserName" or "ConcurrencyFailure")
                ? BusinessError.Conflict : BusinessError.Validation,
                string.Join(" ", result.Errors.Select(x => x.Description)));
    }

    public Task<StaffResponse> CreateAsync(Guid ownerId, CreateStaffRequest request, CancellationToken ct) =>
        transaction.ExecuteAsync(async () =>
        {
            var organizationId = await OwnerOrganization(ownerId, ct);
            var role = await AssignableRole(request.OrganizationRoleId, ct);
            var email = request.Email.Trim();
            if (await users.FindByEmailAsync(email) is not null)
                throw new BusinessException(BusinessError.Conflict, "This email is already registered.");
            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new BusinessException(BusinessError.Validation, "Full name is required.");
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(), FullName = request.FullName.Trim(), Email = email, UserName = email,
                PhoneNumber = request.PhoneNumber.Trim(), PlatformRole = PlatformRole.Organizer,
                IsActive = true, IsFirstLogin = true, LockoutEnabled = true
            };
            Check(await users.CreateAsync(user, request.TemporaryPassword));
            var member = new OrganizationMembership
            {
                Id = Guid.NewGuid(), EventOrganizationId = organizationId, UserId = user.Id,
                OrganizationRoleId = role.Id, IsActive = true, JoinedAtUtc = DateTimeOffset.UtcNow
            };
            db.Add(member);
            await db.SaveChangesAsync(ct);
            return new StaffResponse(member.Id, user.FullName, user.Email!, user.PhoneNumber,
                role.Id, role.Name, true, true, false);
        }, ct);

    private async Task<OrganizationMembership> Target(Guid ownerId, Guid membershipId, CancellationToken ct)
    {
        var organizationId = await OwnerOrganization(ownerId, ct);
        var member = await db.Set<OrganizationMembership>().SingleOrDefaultAsync(x =>
            x.Id == membershipId && x.EventOrganizationId == organizationId, ct)
            ?? throw new BusinessException(BusinessError.NotFound, "Staff member not found.");
        var owner = await db.Set<OrganizationRole>().AnyAsync(x => x.Id == member.OrganizationRoleId && x.Code == "OWNER", ct);
        if (member.UserId == ownerId || owner)
            throw new BusinessException(BusinessError.Forbidden, "The Owner account cannot be changed here.");
        return member;
    }

    public async Task SetRoleAsync(Guid ownerId, Guid membershipId, int roleId, CancellationToken ct)
    {
        await transaction.ExecuteAsync(async () =>
        {
            var member = await Target(ownerId, membershipId, ct);
            member.OrganizationRoleId = (await AssignableRole(roleId, ct)).Id;
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);
    }

    public async Task SetAccessAsync(Guid ownerId, Guid membershipId, bool active, CancellationToken ct)
    {
        await transaction.ExecuteAsync(async () =>
        {
            var member = await Target(ownerId, membershipId, ct);
            member.IsActive = active;
            var user = await users.FindByIdAsync(member.UserId.ToString())
                ?? throw new BusinessException(BusinessError.NotFound, "Staff account not found.");
            // Revoke existing JWTs even if the membership is subsequently reactivated.
            Check(await users.UpdateSecurityStampAsync(user));
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangeOrganizerPasswordRequest request, CancellationToken ct)
    {
        await transaction.ExecuteAsync(async () =>
        {
            if (await organizations.FindAccessAsync(userId, ct) is null)
                throw new BusinessException(BusinessError.Forbidden, "An active membership is required.");
            if (request.CurrentPassword == request.NewPassword)
                throw new BusinessException(BusinessError.Validation, "Choose a different password.");
            var user = await users.FindByIdAsync(userId.ToString());
            if (user is null || !user.IsActive || user.PlatformRole != PlatformRole.Organizer)
                throw new BusinessException(BusinessError.Forbidden, "Account unavailable.");
            // Uses Identity validators, hashing, and security-stamp rotation.
            Check(await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword));
            user.IsFirstLogin = false;
            Check(await users.UpdateAsync(user));
            return true;
        }, ct);
    }
}