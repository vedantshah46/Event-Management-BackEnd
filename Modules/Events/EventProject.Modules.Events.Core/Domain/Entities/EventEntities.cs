namespace EventProject.Modules.Events.Core.Domain.Entities;

public enum EventStatusId { Draft = 1 }

public sealed class EventStatusMaster
{
    public EventStatusId EventStatusId { get; set; }
    public string EventStatus { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}

public sealed class Event
{
    public Guid EventId { get; set; }
    public Guid EventOrganizationId { get; set; }
    public EventStatusId EventStatusId { get; set; }
    public EventStatusMaster Status { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public int RequiredCrewMembers { get; set; }
    public decimal HourlyPayRate { get; set; }
    public bool AreBreaksPaid { get; set; }
    public DateTimeOffset ApplicationsCloseAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid UpdatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}