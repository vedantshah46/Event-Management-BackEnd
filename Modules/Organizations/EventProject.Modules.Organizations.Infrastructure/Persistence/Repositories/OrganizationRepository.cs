using EventProject.Modules.Organizations.Core.Application.DTOs;
using EventProject.Modules.Organizations.Core.Application.Interfaces;
using EventProject.Modules.Organizations.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Identity;
using Shared.Infrastructure.Identity;
using Shared.Infrastructure.Persistence;

namespace EventProject.Modules.Organizations.Infrastructure.Persistence.Repositories;

public sealed class OrganizationRepository(EventProjectDbContext context) : IOrganizationRepository
{
    public Task<EventOrganizationStatusResponse[]> ListStatusesAsync(CancellationToken cancellationToken) =>
        context.Set<EventOrganizationStatusMaster>().AsNoTracking().OrderBy(x => x.EventOrganizationStatusId)
            .Select(x => new EventOrganizationStatusResponse(x.EventOrganizationStatusId, x.EventOrganizationStatusId.ToString(), x.EventOrganizationStatus, !x.IsDeleted, (int)x.EventOrganizationStatusId))
            .ToArrayAsync(cancellationToken);

    public Task<OrganizationRole?> FindRoleAsync(string code, CancellationToken cancellationToken) =>
        context.Set<OrganizationRole>().AsNoTracking().SingleOrDefaultAsync(x => x.Code == code && x.IsActive, cancellationToken);

    public async Task AddRegistrationAsync(EventOrganization organization, OrganizationMembership owner,
        OrganizationStatusHistory history, CancellationToken cancellationToken)
    {
        await context.AddRangeAsync([organization, owner, history], cancellationToken);
    }

    public async Task<OrganizationAccess?> FindAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await (from membership in context.Set<OrganizationMembership>().AsNoTracking()
            join organization in context.Set<EventOrganization>().AsNoTracking().Include(x => x.EventOrganizationStatus) on membership.EventOrganizationId equals organization.Id
            join role in context.Set<OrganizationRole>().AsNoTracking() on membership.OrganizationRoleId equals role.Id
            join user in context.Set<ApplicationUser>().AsNoTracking() on membership.UserId equals user.Id
            where membership.UserId == userId && membership.IsActive && role.IsActive
                && user.IsActive && user.PlatformRole == PlatformRole.Organizer
            select new { Membership = membership, Organization = organization, Role = role, IsFirstLogin = user.IsFirstLogin }).SingleOrDefaultAsync(cancellationToken);
        if (row is null) return null;
        var permissions = await context.Set<OrganizationRolePermission>().AsNoTracking()
            .Where(x => x.OrganizationRoleId == row.Role.Id).OrderBy(x => x.PermissionCode)
            .Select(x => x.PermissionCode).ToArrayAsync(cancellationToken);
        return new OrganizationAccess(ToResponse(row.Organization),
            new MembershipResponse(row.Membership.Id, row.Organization.Id, row.Role.Code, permissions, row.IsFirstLogin));
    }

    public async Task<OrganizationResponse?> FindDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var organization = await context.Set<EventOrganization>().AsNoTracking().Include(x => x.EventOrganizationStatus).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return organization is null ? null : ToResponse(organization);
    }

    public async Task<OrganizationPage> ListAsync(int page, int pageSize, OrganizationStatus? status, CancellationToken cancellationToken)
    {
        IQueryable<EventOrganization> query = context.Set<EventOrganization>().AsNoTracking().Include(x => x.EventOrganizationStatus);
        if (status.HasValue) query = query.Where(x => x.EventOrganizationStatusId == status);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        return new OrganizationPage(rows.Select(ToResponse).ToArray(), page, pageSize, total);
    }

    public Task<EventOrganization?> FindForReviewAsync(Guid id, CancellationToken cancellationToken) =>
        // Review runs in a transaction: serialize reviewers without a concurrency column.
        context.Set<EventOrganization>()
            .FromSqlInterpolated($"SELECT * FROM [dbo].[EventOrganizations] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}")
            .SingleOrDefaultAsync(cancellationToken);
    public async Task AddHistoryAsync(OrganizationStatusHistory history, CancellationToken cancellationToken) =>
        await context.Set<OrganizationStatusHistory>().AddAsync(history, cancellationToken);
    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await context.SaveChangesAsync(cancellationToken);

    public static OrganizationResponse ToResponse(EventOrganization organization) => new(organization.Id,
        organization.Name, organization.ContactEmail, organization.ContactPhone, organization.Address, organization.City,
        organization.State, organization.PostalCode, organization.EventOrganizationStatusId, organization.CreatedAtUtc,
        organization.UpdatedAtUtc,
        organization.EventOrganizationStatus.EventOrganizationStatusId.ToString(), organization.EventOrganizationStatus.EventOrganizationStatus);
}
