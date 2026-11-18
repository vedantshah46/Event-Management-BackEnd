using EventProject.Modules.Organizations.Core.Domain.Entities;
using EventProject.Modules.Organizations.Infrastructure.MasterData;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Identity;
using Shared.Infrastructure.Persistence;

namespace EventProject.Modules.Organizations.Infrastructure.Persistence.Configurations;

public sealed class OrganizationModelConfiguration : IModuleModelConfiguration
{
    public void Configure(ModelBuilder builder)
    {
        builder.Entity<EventOrganization>(organization =>
        {
            organization.ToTable("EventOrganizations", "dbo");
            organization.HasKey(x => x.Id);
            organization.Property(x => x.Name).HasMaxLength(200).IsRequired();
            organization.Property(x => x.ContactEmail).HasMaxLength(256).IsRequired();
            organization.Property(x => x.ContactPhone).HasMaxLength(20).IsRequired();
            organization.Property(x => x.Address).HasMaxLength(300).IsRequired();
            organization.Property(x => x.City).HasMaxLength(100).IsRequired();
            organization.Property(x => x.State).HasMaxLength(100).IsRequired();
            organization.Property(x => x.PostalCode).HasMaxLength(6).IsRequired();
            organization.Property(x => x.EventOrganizationStatusId).HasConversion<int>().IsRequired();
            organization.HasOne(x => x.EventOrganizationStatus).WithMany().HasForeignKey(x => x.EventOrganizationStatusId).OnDelete(DeleteBehavior.Restrict);
            organization.HasIndex(x => new { x.EventOrganizationStatusId, x.CreatedAtUtc });
        });
        builder.Entity<OrganizationRole>(role =>
        {
            role.ToTable("OrganizationRoles", "dbo");
            role.HasKey(x => x.Id);
            role.Property(x => x.Id).ValueGeneratedNever();
            role.Property(x => x.Code).HasMaxLength(50).IsRequired();
            role.Property(x => x.Name).HasMaxLength(100).IsRequired();
            role.HasIndex(x => x.Code).IsUnique();
        });
        builder.Entity<OrganizationRolePermission>(permission =>
        {
            permission.ToTable("OrganizationRolePermissions", "dbo");
            permission.HasKey(x => new { x.OrganizationRoleId, x.PermissionCode });
            permission.Property(x => x.PermissionCode).HasMaxLength(100).IsRequired();
            permission.HasOne<OrganizationRole>().WithMany().HasForeignKey(x => x.OrganizationRoleId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<OrganizationMembership>(membership =>
        {
            membership.ToTable("OrganizationMemberships", "dbo");
            membership.HasKey(x => x.Id);
            membership.HasIndex(x => x.UserId).IsUnique();
            membership.HasIndex(x => new { x.EventOrganizationId, x.IsActive });
            membership.HasOne<EventOrganization>().WithMany().HasForeignKey(x => x.EventOrganizationId).OnDelete(DeleteBehavior.Restrict);
            membership.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            membership.HasOne<OrganizationRole>().WithMany().HasForeignKey(x => x.OrganizationRoleId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<OrganizationPaymentDetails>(payment =>
        {
            payment.ToTable("OrganizationPaymentDetails", "dbo");
            payment.HasKey(x => x.EventOrganizationId);
            payment.Property(x => x.UpiId).HasMaxLength(256).IsRequired();
            payment.HasOne<EventOrganization>().WithOne().HasForeignKey<OrganizationPaymentDetails>(x => x.EventOrganizationId).OnDelete(DeleteBehavior.Restrict);
            payment.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<OrganizationStatusHistory>(history =>
        {
            history.ToTable("OrganizationStatusHistory", "dbo");
            history.HasKey(x => x.Id);
            history.Property(x => x.PreviousEventOrganizationStatusId).HasConversion<int>();
            history.HasOne<EventOrganizationStatusMaster>().WithMany().HasForeignKey(x => x.PreviousEventOrganizationStatusId).OnDelete(DeleteBehavior.Restrict);
            history.Property(x => x.NewEventOrganizationStatusId).HasConversion<int>().IsRequired();
            history.HasOne<EventOrganizationStatusMaster>().WithMany().HasForeignKey(x => x.NewEventOrganizationStatusId).OnDelete(DeleteBehavior.Restrict);
            history.Property(x => x.Reason).HasMaxLength(1000);
            history.HasIndex(x => new { x.EventOrganizationId, x.ChangedAtUtc });
            history.HasOne<EventOrganization>().WithMany().HasForeignKey(x => x.EventOrganizationId).OnDelete(DeleteBehavior.Restrict);
            history.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<EventOrganizationStatusMaster>(status =>
        {
            status.ToTable("EventOrganizationStatus", "dbo");
            status.HasKey(x => x.EventOrganizationStatusId);
            status.Property(x => x.EventOrganizationStatusId).HasConversion<int>().ValueGeneratedNever();
            status.Property(x => x.EventOrganizationStatus).HasMaxLength(100).IsRequired();
            status.Property(x => x.IsDeleted).IsRequired();
            status.HasIndex(x => x.EventOrganizationStatus).IsUnique();
        });
        var master = OrganizationMasterData.Load();
        builder.Entity<EventOrganizationStatusMaster>().HasData(master.Statuses);
        builder.Entity<OrganizationRole>().HasData(master.Roles);
        builder.Entity<OrganizationRolePermission>().HasData(master.Permissions);
    }
}
