using System.ComponentModel.DataAnnotations;
using EventProject.Modules.Events.Core.Application;
using EventProject.Modules.Events.Core.Domain.Entities;
using EventProject.Modules.Organizations.Core.Application.Interfaces;
using EventProject.Modules.Organizations.Core.Domain;
using EventProject.Modules.Organizations.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Exceptions;
using Shared.Infrastructure.Persistence;

namespace EventProject.Modules.Events.Infrastructure.Services;

public sealed class EventService(EventProjectDbContext db, IOrganizationRepository organizations,
    IOrganizationTransaction transaction, TimeProvider clock) : IEventService
{
    private async Task<Guid> Organization(Guid userId, CancellationToken ct)
    {
        var access = await organizations.FindAccessAsync(userId, ct);
        if (access is null || access.Membership.IsFirstLogin
            || access.Organization.EventOrganizationStatusId != OrganizationStatus.Approved
            || !access.Membership.Permissions.Contains(OrganizationPermissions.ManageEvents))
            throw new BusinessException(BusinessError.Forbidden, "An approved organization and event management permission are required.");
        return access.Organization.Id;
    }

    public async Task<EventStatusResponse[]> StatusesAsync(Guid userId, CancellationToken ct)
    {
        await Organization(userId, ct);
        return await db.Set<EventStatusMaster>().AsNoTracking().OrderBy(x => x.EventStatusId)
            .Select(x => new EventStatusResponse(x.EventStatusId, x.EventStatus, x.IsDeleted)).ToArrayAsync(ct);
    }

    public async Task<EventPage> ListAsync(Guid userId, int page, int pageSize, EventStatusId? eventStatusId, CancellationToken ct)
    {
        var organizationId = await Organization(userId, ct);
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue
            || (eventStatusId.HasValue && !Enum.IsDefined(eventStatusId.Value)))
            throw new BusinessException(BusinessError.Validation, "Use a valid status, page >= 1 and pageSize between 1 and 100.");
        IQueryable<Event> query = db.Set<Event>().AsNoTracking().Include(x => x.Status)
            .Where(x => x.EventOrganizationId == organizationId);
        if (eventStatusId.HasValue) query = query.Where(x => x.EventStatusId == eventStatusId.Value);
        var count = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.EventId)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return new EventPage(rows.Select(Response).ToArray(), page, pageSize, count);
    }

    public async Task<EventResponse> DetailsAsync(Guid userId, Guid eventId, CancellationToken ct)
    {
        var organizationId = await Organization(userId, ct);
        var row = await db.Set<Event>().AsNoTracking().Include(x => x.Status)
            .SingleOrDefaultAsync(x => x.EventId == eventId && x.EventOrganizationId == organizationId, ct)
            ?? throw new BusinessException(BusinessError.NotFound, "Event not found.");
        return Response(row);
    }

    private static void Validate(SaveDraftEventRequest request, DateTimeOffset now)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
            throw new BusinessException(BusinessError.Validation, string.Join(" ", errors.Select(x => x.ErrorMessage)));
        if (request.StartsAtUtc <= now || request.EndsAtUtc <= request.StartsAtUtc
            || request.ApplicationsCloseAtUtc == default || request.ApplicationsCloseAtUtc > request.StartsAtUtc)
            throw new BusinessException(BusinessError.Validation, "Start must be in the future, end must follow start, and application closing time must not be after start.");
        if (request.HourlyPayRate <= 0 || request.HourlyPayRate > 9999999999999999.99m
            || decimal.Round(request.HourlyPayRate, 2) != request.HourlyPayRate)
            throw new BusinessException(BusinessError.Validation, "Hourly pay must be a positive INR amount with at most two decimal places.");
    }

    private async Task<EventStatusMaster> DraftStatus(CancellationToken ct) =>
        await db.Set<EventStatusMaster>().SingleOrDefaultAsync(x => x.EventStatusId == EventStatusId.Draft && !x.IsDeleted, ct)
            ?? throw new BusinessException(BusinessError.Conflict, "Draft status is unavailable. Apply the status master migration first.");

    public Task<EventResponse> CreateAsync(Guid userId, SaveDraftEventRequest request, CancellationToken ct) =>
        transaction.ExecuteAsync(async () =>
        {
            var organizationId = await Organization(userId, ct);
            var now = clock.GetUtcNow();
            Validate(request, now);
            var row = new Event
            {
                EventId = Guid.NewGuid(), EventOrganizationId = organizationId, EventStatusId = EventStatusId.Draft,
                Status = await DraftStatus(ct), CreatedByUserId = userId, CreatedAtUtc = now
            };
            Apply(row, request, userId, now);
            db.Add(row);
            await db.SaveChangesAsync(ct);
            return Response(row);
        }, ct);

    public Task<EventResponse> UpdateAsync(Guid userId, Guid eventId, SaveDraftEventRequest request, CancellationToken ct) =>
        transaction.ExecuteAsync(async () =>
        {
            var organizationId = await Organization(userId, ct);
            var row = await db.Set<Event>().SingleOrDefaultAsync(x => x.EventId == eventId && x.EventOrganizationId == organizationId, ct)
                ?? throw new BusinessException(BusinessError.NotFound, "Event not found.");
            if (row.EventStatusId != EventStatusId.Draft)
                throw new BusinessException(BusinessError.Conflict, "Only draft events can be edited.");
            var now = clock.GetUtcNow();
            Validate(request, now);
            row.Status = await DraftStatus(ct);
            Apply(row, request, userId, now);
            await db.SaveChangesAsync(ct);
            return Response(row);
        }, ct);

    private static void Apply(Event row, SaveDraftEventRequest request, Guid userId, DateTimeOffset now)
    {
        row.Name = request.Name.Trim(); row.Description = request.Description.Trim();
        row.VenueName = request.VenueName.Trim(); row.Address = request.Address.Trim();
        row.City = request.City.Trim(); row.State = request.State.Trim(); row.PostalCode = request.PostalCode;
        row.StartsAtUtc = request.StartsAtUtc.ToUniversalTime(); row.EndsAtUtc = request.EndsAtUtc.ToUniversalTime();
        row.ApplicationsCloseAtUtc = request.ApplicationsCloseAtUtc.ToUniversalTime();
        row.RequiredCrewMembers = request.RequiredCrewMembers; row.HourlyPayRate = request.HourlyPayRate;
        row.AreBreaksPaid = request.AreBreaksPaid!.Value;
        row.UpdatedByUserId = userId; row.UpdatedAtUtc = now;
    }

    private static EventResponse Response(Event row) => new(row.EventId, row.EventOrganizationId, row.EventStatusId,
        row.Status.EventStatus, row.Name, row.Description, row.VenueName, row.Address, row.City, row.State,
        row.PostalCode, row.StartsAtUtc, row.EndsAtUtc, row.RequiredCrewMembers, row.HourlyPayRate, row.AreBreaksPaid,
        row.ApplicationsCloseAtUtc, row.CreatedByUserId, row.UpdatedByUserId, row.CreatedAtUtc, row.UpdatedAtUtc);
}