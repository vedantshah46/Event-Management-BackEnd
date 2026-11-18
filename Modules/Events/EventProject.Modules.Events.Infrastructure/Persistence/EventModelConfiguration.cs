using System.Text.Json;
using System.Text.Json.Serialization;
using EventProject.Modules.Events.Core.Domain.Entities;
using EventProject.Modules.Organizations.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Identity;
using Shared.Infrastructure.Persistence;

namespace EventProject.Modules.Events.Infrastructure.Persistence;

public sealed class EventModelConfiguration : IModuleModelConfiguration
{
    public void Configure(ModelBuilder builder)
    {
        builder.Entity<EventStatusMaster>(status =>
        {
            status.ToTable("EventStatus", "dbo");
            status.HasKey(x => x.EventStatusId);
            status.Property(x => x.EventStatusId).HasConversion<int>().ValueGeneratedNever();
            status.Property(x => x.EventStatus).HasMaxLength(100).IsRequired();
            status.HasIndex(x => x.EventStatus).IsUnique();
            status.Property(x => x.IsDeleted).IsRequired();
            status.HasData(LoadStatuses());
        });
        builder.Entity<Event>(entry =>
        {
            entry.ToTable("Events", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Events_Dates", "[EndsAtUtc] > [StartsAtUtc] AND [ApplicationsCloseAtUtc] <= [StartsAtUtc]");
                table.HasCheckConstraint("CK_Events_CrewAndPay", "[RequiredCrewMembers] > 0 AND [HourlyPayRate] > 0");
            });
            entry.HasKey(x => x.EventId);
            entry.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entry.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entry.Property(x => x.VenueName).HasMaxLength(200).IsRequired();
            entry.Property(x => x.Address).HasMaxLength(300).IsRequired();
            entry.Property(x => x.City).HasMaxLength(100).IsRequired();
            entry.Property(x => x.State).HasMaxLength(100).IsRequired();
            entry.Property(x => x.PostalCode).HasMaxLength(6).IsRequired();
            entry.Property(x => x.HourlyPayRate).HasPrecision(18, 2);
            entry.Property(x => x.EventStatusId).HasConversion<int>();
            entry.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.EventStatusId).OnDelete(DeleteBehavior.Restrict);
            entry.HasOne<EventOrganization>().WithMany().HasForeignKey(x => x.EventOrganizationId).OnDelete(DeleteBehavior.Restrict);
            entry.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entry.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entry.HasIndex(x => new { x.EventOrganizationId, x.CreatedAtUtc, x.EventId });
            entry.HasIndex(x => new { x.EventOrganizationId, x.EventStatusId });
        });
    }

    private static EventStatusMaster[] LoadStatuses()
    {
        var assembly = typeof(EventModelConfiguration).Assembly;
        using var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.MasterData.EventStatus.json")
            ?? throw new InvalidOperationException("Missing embedded EventStatus.json.");
        var statuses = JsonSerializer.Deserialize<EventStatusMaster[]>(stream, new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        }) ?? throw new InvalidOperationException("Empty EventStatus.json.");
        if (statuses.Length != Enum.GetValues<EventStatusId>().Length
            || statuses.Any(x => !Enum.IsDefined(x.EventStatusId) || string.IsNullOrWhiteSpace(x.EventStatus) || x.EventStatus.Length > 100)
            || statuses.Select(x => x.EventStatusId).Distinct().Count() != statuses.Length
            || statuses.Select(x => x.EventStatus).Distinct(StringComparer.OrdinalIgnoreCase).Count() != statuses.Length)
            throw new InvalidOperationException("EventStatus.json requires every enum ID once and unique valid status names.");
        return statuses.OrderBy(x => x.EventStatusId).ToArray();
    }
}