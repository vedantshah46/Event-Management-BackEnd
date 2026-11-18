using EventProject.Modules.Organizations.Core.Application.DTOs;
using EventProject.Modules.Organizations.Core.Domain;
using EventProject.Modules.Organizations.Core.Domain.Entities;
using EventProject.Modules.Organizations.Infrastructure.MasterData;
using EventProject.Modules.Organizations.Infrastructure.Persistence.Configurations;
using EventProject.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Shared.Core.Exceptions;
using Shared.Core.Identity;
using Shared.Infrastructure.Persistence;
using Xunit;

namespace EventProject.UnitTests;

public sealed class OrganizationTests
{
    [Fact]
    public async Task RegistrationCreatesPendingOrganizationOrganizerOwnerAndHistoryWithinTransaction()
    {
        var fixture = new OrganizationFixture();
        var response = await fixture.Service.RegisterAsync(OrganizationFixture.Request(), default);
        Assert.Equal(OrganizationStatus.PendingApproval, response.EventOrganizationStatusId);
        Assert.Equal(1, fixture.Transaction.Calls);
        Assert.Equal(PlatformRole.Organizer, fixture.Accounts.Accounts[response.UserId].Identity.Role);
        var member = Assert.Single(fixture.Repository.Memberships);
        Assert.Equal(1, member.OrganizationRoleId);
        Assert.Equal(response.OrganizationId, member.EventOrganizationId);
        Assert.Equal(response.UserId, member.UserId);
        Assert.Equal(response.UserId, Assert.Single(fixture.Repository.Histories).ChangedByUserId);
        Assert.True(fixture.Repository.Organizations[response.OrganizationId].UpdatedAtUtc > DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task DuplicateRegistrationCannotCreateSecondOrganization()
    {
        var fixture = new OrganizationFixture();
        await fixture.Service.RegisterAsync(OrganizationFixture.Request(), default);
        var error = await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.RegisterAsync(OrganizationFixture.Request(), default));
        Assert.Equal(BusinessError.Conflict, error.Error);
        Assert.Single(fixture.Repository.Organizations);
    }

    [Fact]
    public async Task PendingOwnerCanLoginButInactiveMembershipCannotReceiveToken()
    {
        var fixture = new OrganizationFixture();
        await fixture.Service.RegisterAsync(OrganizationFixture.Request(), default);
        var request = new OrganizerLoginRequest { Email = "owner@example.test", Password = "TestOnly@Password123" };
        var result = await fixture.Service.LoginAsync(request, default);
        Assert.NotNull(result);
        Assert.Equal(OrganizationStatus.PendingApproval, result.Organization.EventOrganizationStatusId);
        Assert.Contains(OrganizationPermissions.ViewProfile, result.Membership.Permissions);
        fixture.Repository.Memberships[0].IsActive = false;
        Assert.Null(await fixture.Service.LoginAsync(request, default));
        Assert.Equal(1, fixture.Tokens.Calls);
    }

    [Fact]
    public async Task OwnProfileCannotResolveAnotherUsersOrganization()
    {
        var fixture = new OrganizationFixture();
        await fixture.Service.RegisterAsync(OrganizationFixture.Request(), default);
        var error = await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.GetOwnAsync(Guid.NewGuid(), default));
        Assert.Equal(BusinessError.Forbidden, error.Error);
    }

    [Fact]
    public async Task ReviewRequiresReasonAndPendingStateAndRecordsActor()
    {
        var fixture = new OrganizationFixture();
        var registration = await fixture.Service.RegisterAsync(OrganizationFixture.Request(), default);
        await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.ReviewAsync(registration.OrganizationId,
            OrganizationFixture.AdminId, false, " ", default));
        var result = await fixture.Service.ReviewAsync(registration.OrganizationId, OrganizationFixture.AdminId, true, null, default);
        Assert.Equal(OrganizationStatus.Approved, result.EventOrganizationStatusId);
        Assert.Equal(OrganizationFixture.AdminId, fixture.Repository.Histories.Last().ChangedByUserId);
        var repeated = await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.ReviewAsync(registration.OrganizationId,
            OrganizationFixture.AdminId, false, "Rejected later", default));
        Assert.Equal(BusinessError.Conflict, repeated.Error);
        Assert.Equal(2, fixture.Repository.Histories.Count);
    }

    [Fact]
    public void ModuleModelUsesDboUniqueMembershipAndDeterministicEmbeddedSeeds()
    {
        using var context = new EventProjectDbContext(new DbContextOptionsBuilder<EventProjectDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ModelOnly;Trusted_Connection=True").Options,
            [new OrganizationModelConfiguration()]);
        var model = context.GetService<IDesignTimeModel>().Model;
        Assert.All(model.GetEntityTypes(), entity => Assert.Equal("dbo", entity.GetSchema()));
        var membership = model.FindEntityType(typeof(OrganizationMembership))!;
        Assert.Contains(membership.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == "UserId");
        var seeds = model.FindEntityType(typeof(OrganizationRole))!.GetSeedData().ToArray();
        Assert.Equal(3, seeds.Length);
        Assert.Contains(seeds, seed => (int)seed["Id"]! == 1 && (string)seed["Code"]! == "OWNER");
        Assert.Equal(14, model.FindEntityType(typeof(OrganizationRolePermission))!.GetSeedData().Count());
        Assert.Equal(OrganizationMasterData.Load().Roles.Select(role => role.Id), OrganizationMasterData.Load().Roles.Select(role => role.Id));
    }
}
