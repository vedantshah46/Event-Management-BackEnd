using EventProject.Modules.Organizations.Core.Application.DTOs;
using EventProject.Modules.Organizations.Core.Domain.Entities;
using Shared.Core.Identity;

namespace EventProject.Modules.Organizations.Core.Application.Interfaces;

public interface IOrganizationRepository
{
    Task<EventOrganizationStatusResponse[]> ListStatusesAsync(CancellationToken cancellationToken);
    Task<OrganizationRole?> FindRoleAsync(string code, CancellationToken cancellationToken);
    Task AddRegistrationAsync(EventOrganization organization, OrganizationMembership owner,
        OrganizationStatusHistory history, CancellationToken cancellationToken);
    Task<OrganizationAccess?> FindAccessAsync(Guid userId, CancellationToken cancellationToken);
    Task<OrganizationResponse?> FindDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<OrganizationPage> ListAsync(int page, int pageSize, OrganizationStatus? status, CancellationToken cancellationToken);
    Task<EventOrganization?> FindForReviewAsync(Guid id, CancellationToken cancellationToken);
    Task AddHistoryAsync(OrganizationStatusHistory history, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IOrganizationAccountService
{
    Task<Guid> CreateOrganizerAsync(string fullName, string email, string password, CancellationToken cancellationToken);
    Task<AccountIdentity?> AuthenticateOrganizerAsync(string email, string password, CancellationToken cancellationToken);
}

public interface IOrganizationTransaction
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken);
}

public interface IOrganizationService
{
    Task<EventOrganizationStatusResponse[]> ListStatusesAsync(CancellationToken cancellationToken);
    Task<RegistrationResponse> RegisterAsync(RegisterOrganizationRequest request, CancellationToken cancellationToken);
    Task<OrganizerLoginResponse?> LoginAsync(OrganizerLoginRequest request, CancellationToken cancellationToken);
    Task<OrganizationAccess> GetOwnAsync(Guid userId, CancellationToken cancellationToken);
    Task<OrganizationPage> ListAsync(int page, int pageSize, OrganizationStatus? status, CancellationToken cancellationToken);
    Task<OrganizationResponse> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<OrganizationResponse> ReviewAsync(Guid id, Guid adminId, bool approve, string? reason,
        CancellationToken cancellationToken);
}
