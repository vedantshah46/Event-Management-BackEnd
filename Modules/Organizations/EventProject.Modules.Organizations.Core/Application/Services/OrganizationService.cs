using System.ComponentModel.DataAnnotations;
using EventProject.Modules.Organizations.Core.Application.DTOs;
using EventProject.Modules.Organizations.Core.Application.Interfaces;
using EventProject.Modules.Organizations.Core.Domain;
using EventProject.Modules.Organizations.Core.Domain.Entities;
using Shared.Core.Exceptions;
using Shared.Core.Identity;

namespace EventProject.Modules.Organizations.Core.Application.Services;

public sealed class OrganizationService(IOrganizationRepository organizations, IOrganizationAccountService accounts,
    IOrganizationTransaction transaction, IAccountTokenIssuer tokens, TimeProvider clock) : IOrganizationService
{
    public Task<EventOrganizationStatusResponse[]> ListStatusesAsync(CancellationToken cancellationToken) =>
        organizations.ListStatusesAsync(cancellationToken);

    public async Task<RegistrationResponse> RegisterAsync(RegisterOrganizationRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, validateAllProperties: true))
            throw new BusinessException(BusinessError.Validation, string.Join(" ", errors.Select(x => x.ErrorMessage)));
        return await transaction.ExecuteAsync(async () =>
        {
            await RequireActiveStatusAsync(OrganizationStatus.PendingApproval, cancellationToken);
            var ownerRole = await organizations.FindRoleAsync("OWNER", cancellationToken)
                ?? throw new BusinessException(BusinessError.Conflict, "The Owner role is unavailable. Apply the master-data migration first.");
            var userId = await accounts.CreateOrganizerAsync(request.OwnerFullName.Trim(), request.Email.Trim(), request.Password, cancellationToken);
            var now = clock.GetUtcNow();
            var organization = new EventOrganization
            {
                Id = Guid.NewGuid(), Name = request.OrganizationName.Trim(), ContactEmail = request.ContactEmail.Trim(),
                ContactPhone = request.ContactPhone.Trim(), Address = request.Address.Trim(), City = request.City.Trim(),
                State = request.State.Trim(), PostalCode = request.PostalCode, CreatedAtUtc = now, UpdatedAtUtc = now
            };
            await organizations.AddRegistrationAsync(organization, new OrganizationMembership
            {
                Id = Guid.NewGuid(), EventOrganizationId = organization.Id, UserId = userId,
                OrganizationRoleId = ownerRole.Id, IsActive = true, JoinedAtUtc = now
            }, new OrganizationStatusHistory
            {
                Id = Guid.NewGuid(), EventOrganizationId = organization.Id, NewEventOrganizationStatusId = OrganizationStatus.PendingApproval,
                ChangedByUserId = userId, ChangedAtUtc = now, Reason = "Organization registered."
            }, cancellationToken);
            await organizations.SaveChangesAsync(cancellationToken);
            return new RegistrationResponse(organization.Id, userId, OrganizationStatus.PendingApproval);
        }, cancellationToken);
    }

    public async Task<OrganizerLoginResponse?> LoginAsync(OrganizerLoginRequest request, CancellationToken cancellationToken)
    {
        var account = await accounts.AuthenticateOrganizerAsync(request.Email.Trim(), request.Password, cancellationToken);
        if (account is null) return null;
        var access = await organizations.FindAccessAsync(account.Id, cancellationToken);
        if (access is null || !access.Membership.Permissions.Contains(OrganizationPermissions.ViewProfile)) return null;
        var token = tokens.Issue(account);
        return new OrganizerLoginResponse(token.Value, token.ExpiresAtUtc, access.Organization, access.Membership);
    }

    public async Task<OrganizationAccess> GetOwnAsync(Guid userId, CancellationToken cancellationToken)
    {
        var access = await organizations.FindAccessAsync(userId, cancellationToken);
        if (access is null || !access.Membership.Permissions.Contains(OrganizationPermissions.ViewProfile))
            throw new BusinessException(BusinessError.Forbidden, "No active organization membership with profile access.");
        return access;
    }

    public Task<OrganizationPage> ListAsync(int page, int pageSize, OrganizationStatus? status, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue
            || (status.HasValue && !Enum.IsDefined(status.Value)))
            throw new BusinessException(BusinessError.Validation, "Use a valid status, page >= 1 and pageSize between 1 and 100.");
        return organizations.ListAsync(page, pageSize, status, cancellationToken);
    }

    public async Task<OrganizationResponse> GetDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        await organizations.FindDetailsAsync(id, cancellationToken)
            ?? throw new BusinessException(BusinessError.NotFound, "Organization not found.");

    public async Task<OrganizationResponse> ReviewAsync(Guid id, Guid adminId, bool approve, string? reason,
        CancellationToken cancellationToken)
    {
        if (!approve && (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000))
            throw new BusinessException(BusinessError.Validation, "Rejection requires a reason of at most 1000 characters.");
        return await transaction.ExecuteAsync(async () =>
        {
            var organization = await organizations.FindForReviewAsync(id, cancellationToken)
                ?? throw new BusinessException(BusinessError.NotFound, "Organization not found.");
            await RequireActiveStatusAsync(approve ? OrganizationStatus.Approved : OrganizationStatus.Rejected, cancellationToken);
            var previous = organization.EventOrganizationStatusId;
            var now = clock.GetUtcNow();
            organization.Review(approve ? OrganizationStatus.Approved : OrganizationStatus.Rejected, now);
            await organizations.AddHistoryAsync(new OrganizationStatusHistory
            {
                Id = Guid.NewGuid(), EventOrganizationId = id, PreviousEventOrganizationStatusId = previous, NewEventOrganizationStatusId = organization.EventOrganizationStatusId,
                ChangedByUserId = adminId, ChangedAtUtc = now, Reason = approve ? "Approved by platform." : reason!.Trim()
            }, cancellationToken);
            await organizations.SaveChangesAsync(cancellationToken);
            return await GetDetailsAsync(id, cancellationToken);
        }, cancellationToken);
    }

    private async Task RequireActiveStatusAsync(OrganizationStatus id, CancellationToken cancellationToken)
    {
        var statuses = await organizations.ListStatusesAsync(cancellationToken);
        if (!statuses.Any(status => status.Id == id && status.IsActive))
            throw new BusinessException(BusinessError.Conflict, "The required organization status is unavailable. Apply the master-data migration first.");
    }
}
