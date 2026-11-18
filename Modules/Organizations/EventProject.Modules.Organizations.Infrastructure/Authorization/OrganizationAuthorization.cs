using EventProject.Modules.Organizations.Core.Application.Interfaces;
using EventProject.Modules.Organizations.Core.Domain;
using EventProject.Modules.Organizations.Core.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace EventProject.Modules.Organizations.Infrastructure.Authorization;

public sealed record OrganizationPermissionRequirement(string Permission, bool RequireApproved) : IAuthorizationRequirement;

public sealed class OrganizationPermissionHandler(IOrganizationRepository organizations)
    : AuthorizationHandler<OrganizationPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OrganizationPermissionRequirement requirement)
    {
        if (!context.User.IsInRole("Organizer")) return;
        if (!Guid.TryParse(context.User.FindFirst("sub")?.Value, out var userId)) return;
        var access = await organizations.FindAccessAsync(userId, CancellationToken.None);
        if (access is not null && !access.Membership.IsFirstLogin && access.Membership.Permissions.Contains(requirement.Permission)
            && (!requirement.RequireApproved || access.Organization.EventOrganizationStatusId == OrganizationStatus.Approved))
            context.Succeed(requirement);
    }
}
