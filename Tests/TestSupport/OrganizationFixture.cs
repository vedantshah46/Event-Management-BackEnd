using EventProject.Modules.Identity.Core.Application.Admin;
using EventProject.Modules.Organizations.Core.Application.DTOs;
using EventProject.Modules.Organizations.Core.Application.Interfaces;
using EventProject.Modules.Organizations.Core.Application.Services;
using EventProject.Modules.Organizations.Core.Domain.Entities;
using EventProject.Modules.Organizations.Infrastructure.MasterData;
using EventProject.Modules.Organizations.Infrastructure.Persistence.Repositories;
using Shared.Core.Exceptions;
using Shared.Core.Identity;

namespace EventProject.TestSupport;

// In-memory test doubles only. Never connect to a database.
public sealed class OrganizationFixture
{
    public static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public FakeOrganizationAccounts Accounts { get; } = new();
    public FakeOrganizationRepository Repository { get; } = new();
    public FakeOrganizationTransaction Transaction { get; } = new();
    public RecordingAccountIssuer Tokens { get; } = new();
    public OrganizationService Service => new(Repository, Accounts, Transaction, Tokens, TimeProvider.System);
    public static RegisterOrganizationRequest Request(string email = "owner@example.test") => new()
    {
        OwnerFullName = "Company Owner", Email = email, Password = "TestOnly@Password123", ConfirmPassword = "TestOnly@Password123",
        OrganizationName = "Ahmedabad Live", ContactEmail = "contact@example.test", ContactPhone = "+919876543210",
        Address = "Test venue road", City = "Ahmedabad", State = "Gujarat", PostalCode = "380001"
    };
}

public sealed class FakeOrganizationAccounts : IOrganizationAccountService, IAccountIdentityStore, IAdminAccountStore
{
    public Dictionary<Guid, (string Email, string Password, AccountIdentity Identity)> Accounts { get; } = new()
    {
        [OrganizationFixture.AdminId] = ("admin@example.test", "TestOnly@Password123",
            new AccountIdentity(OrganizationFixture.AdminId, PlatformRole.Admin, "admin-stamp"))
    };

    public Task<Guid> CreateOrganizerAsync(string fullName, string email, string password, CancellationToken cancellationToken)
    {
        if (Accounts.Values.Any(account => account.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessException(BusinessError.Conflict, "An account with this email already exists.");
        var id = Guid.NewGuid();
        Accounts.Add(id, (email, password, new AccountIdentity(id, PlatformRole.Organizer, "organizer-stamp")));
        return Task.FromResult(id);
    }
    public Task<AccountIdentity?> AuthenticateOrganizerAsync(string email, string password, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.Values.Where(account => account.Identity.Role == PlatformRole.Organizer
            && account.Email == email && account.Password == password).Select(account => account.Identity).SingleOrDefault());
    public Task<AccountIdentity?> FindActiveAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.TryGetValue(id, out var account) ? account.Identity : null);
    Task<AdminAccount?> IAdminAccountStore.AuthenticateAsync(string email, string password, CancellationToken cancellationToken) =>
        Task.FromResult<AdminAccount?>(email == "admin@example.test" && password == "TestOnly@Password123"
            ? new AdminAccount(OrganizationFixture.AdminId, "Admin", email, "admin-stamp") : null);
    Task<AdminAccount?> IAdminAccountStore.FindActiveAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult<AdminAccount?>(id == OrganizationFixture.AdminId
            ? new AdminAccount(id, "Admin", "admin@example.test", "admin-stamp") : null);
}

public sealed class FakeOrganizationRepository : IOrganizationRepository
{
    public Dictionary<Guid, EventOrganization> Organizations { get; } = [];
    public List<OrganizationMembership> Memberships { get; } = [];
    public List<OrganizationStatusHistory> Histories { get; } = [];
    public EventOrganizationStatusMaster[] Statuses { get; } = OrganizationMasterData.Load().Statuses;
    public Task<EventOrganizationStatusResponse[]> ListStatusesAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Statuses.Select(x => new EventOrganizationStatusResponse(x.EventOrganizationStatusId, x.EventOrganizationStatusId.ToString(), x.EventOrganizationStatus, !x.IsDeleted, (int)x.EventOrganizationStatusId)).ToArray());
    private OrganizationResponse ToResponse(EventOrganization organization)
    {
        organization.EventOrganizationStatus = Statuses.Single(x => x.EventOrganizationStatusId == organization.EventOrganizationStatusId);
        return OrganizationRepository.ToResponse(organization);
    }
    public OrganizationRole[] Roles { get; } = OrganizationMasterData.Load().Roles;
    public OrganizationRolePermission[] Permissions { get; } = OrganizationMasterData.Load().Permissions;
    public Task<OrganizationRole?> FindRoleAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(Roles.SingleOrDefault(role => role.Code == code && role.IsActive));
    public Task AddRegistrationAsync(EventOrganization organization, OrganizationMembership owner, OrganizationStatusHistory history, CancellationToken cancellationToken)
    {
        Organizations.Add(organization.Id, organization);
        Memberships.Add(owner);
        Histories.Add(history);
        return Task.CompletedTask;
    }
    public Task<OrganizationAccess?> FindAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        var membership = Memberships.SingleOrDefault(row => row.UserId == userId && row.IsActive);
        var role = membership is null ? null : Roles.SingleOrDefault(role => role.Id == membership.OrganizationRoleId && role.IsActive);
        return Task.FromResult<OrganizationAccess?>(membership is null || role is null ? null
            : new OrganizationAccess(ToResponse(Organizations[membership.EventOrganizationId]),
                new MembershipResponse(membership.Id, membership.EventOrganizationId, role.Code,
                    Permissions.Where(permission => permission.OrganizationRoleId == role.Id).Select(permission => permission.PermissionCode).ToArray())));
    }
    public Task<OrganizationResponse?> FindDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Organizations.TryGetValue(id, out var organization) ? ToResponse(organization) : null);
    public Task<OrganizationPage> ListAsync(int page, int pageSize, OrganizationStatus? status, CancellationToken cancellationToken)
    {
        var rows = Organizations.Values.Where(row => status is null || row.EventOrganizationStatusId == status).ToArray();
        return Task.FromResult(new OrganizationPage(rows.Skip((page - 1) * pageSize).Take(pageSize).Select(ToResponse).ToArray(), page, pageSize, rows.Length));
    }
    public Task<EventOrganization?> FindForReviewAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Organizations.GetValueOrDefault(id));
    public Task AddHistoryAsync(OrganizationStatusHistory history, CancellationToken cancellationToken)
    {
        Histories.Add(history);
        return Task.CompletedTask;
    }
    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class FakeOrganizationTransaction : IOrganizationTransaction
{
    public int Calls { get; private set; }
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        return await operation();
    }
}

public sealed class RecordingAccountIssuer : IAccountTokenIssuer
{
    public int Calls { get; private set; }
    public AccountAccessToken Issue(AccountIdentity account)
    {
        Calls++;
        return new AccountAccessToken("unit-test-token", DateTimeOffset.UtcNow.AddMinutes(15));
    }
}
