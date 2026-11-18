using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EventProject.Modules.Events.Core.Domain.Entities;

namespace EventProject.Modules.Events.Core.Application;

public sealed class SaveDraftEventRequest
{
    [Required, StringLength(200)] public string Name { get; init; } = string.Empty;
    [Required, StringLength(4000)] public string Description { get; init; } = string.Empty;
    [Required, StringLength(200)] public string VenueName { get; init; } = string.Empty;
    [Required, StringLength(300)] public string Address { get; init; } = string.Empty;
    [Required, StringLength(100)] public string City { get; init; } = string.Empty;
    [Required, StringLength(100)] public string State { get; init; } = string.Empty;
    [Required, RegularExpression(@"\d{6}")] public string PostalCode { get; init; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; init; }
    public DateTimeOffset EndsAtUtc { get; init; }
    [Range(1, int.MaxValue)] public int RequiredCrewMembers { get; init; }
    public decimal HourlyPayRate { get; init; }
    [Required] public bool? AreBreaksPaid { get; init; }
    public DateTimeOffset ApplicationsCloseAtUtc { get; init; }
}
public sealed record EventStatusResponse(
    [property: JsonConverter(typeof(JsonNumberEnumConverter<EventStatusId>))] EventStatusId EventStatusId,
    string EventStatus, bool IsDeleted);
public sealed record EventResponse(Guid EventId, Guid EventOrganizationId,
    [property: JsonConverter(typeof(JsonNumberEnumConverter<EventStatusId>))] EventStatusId EventStatusId,
    string EventStatus, string Name, string Description, string VenueName, string Address,
    string City, string State, string PostalCode, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc,
    int RequiredCrewMembers, decimal HourlyPayRate, bool AreBreaksPaid, DateTimeOffset ApplicationsCloseAtUtc,
    Guid CreatedByUserId, Guid UpdatedByUserId, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
public sealed record EventPage(EventResponse[] Items, int Page, int PageSize, int TotalCount);
public interface IEventService
{
    Task<EventStatusResponse[]> StatusesAsync(Guid userId, CancellationToken ct);
    Task<EventPage> ListAsync(Guid userId, int page, int pageSize, EventStatusId? eventStatusId, CancellationToken ct);
    Task<EventResponse> DetailsAsync(Guid userId, Guid eventId, CancellationToken ct);
    Task<EventResponse> CreateAsync(Guid userId, SaveDraftEventRequest request, CancellationToken ct);
    Task<EventResponse> UpdateAsync(Guid userId, Guid eventId, SaveDraftEventRequest request, CancellationToken ct);
}