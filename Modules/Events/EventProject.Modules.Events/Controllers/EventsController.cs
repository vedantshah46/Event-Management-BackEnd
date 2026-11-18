using EventProject.Modules.Events.Core.Application;
using EventProject.Modules.Events.Core.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventProject.Modules.Events.Controllers;

[ApiController]
[Authorize(Policy = "ApprovedOrganization")]
[Route("api/organizations/me/events")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EventsController(IEventService service) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirst("sub")!.Value);

    [HttpGet]
    public async Task<ActionResult<EventPage>> List(CancellationToken ct,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] EventStatusId? eventStatusId = null) =>
        Ok(await service.ListAsync(UserId, page, pageSize, eventStatusId, ct));

    [HttpGet("statuses")]
    public async Task<ActionResult<EventStatusResponse[]>> Statuses(CancellationToken ct) =>
        Ok(await service.StatusesAsync(UserId, ct));

    [HttpGet("{eventId:guid}")]
    public async Task<ActionResult<EventResponse>> Details(Guid eventId, CancellationToken ct) =>
        Ok(await service.DetailsAsync(UserId, eventId, ct));

    [HttpPost]
    public async Task<ActionResult<EventResponse>> Create(SaveDraftEventRequest request, CancellationToken ct)
    {
        var row = await service.CreateAsync(UserId, request, ct);
        return CreatedAtAction(nameof(Details), new { eventId = row.EventId }, row);
    }

    [HttpPut("{eventId:guid}")]
    public async Task<ActionResult<EventResponse>> Update(Guid eventId, SaveDraftEventRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(UserId, eventId, request, ct));
}