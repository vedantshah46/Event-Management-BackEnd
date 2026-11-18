using EventProject.Modules.Organizations.Core.Application.DTOs;
using EventProject.Modules.Organizations.Core.Application.Interfaces;
using EventProject.Modules.Organizations.Core.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventProject.Modules.Organizations.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/organizations")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminOrganizationsController(IOrganizationService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OrganizationPage>> List(CancellationToken cancellationToken,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] OrganizationStatus? eventOrganizationStatusId = null) =>
        Ok(await service.ListAsync(page, pageSize, eventOrganizationStatusId, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrganizationResponse>> Details(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetDetailsAsync(id, cancellationToken));

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<OrganizationResponse>> Approve(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.ReviewAsync(id, Guid.Parse(User.FindFirst("sub")!.Value), true, null, cancellationToken));

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<OrganizationResponse>> Reject(Guid id, RejectOrganizationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.ReviewAsync(id, Guid.Parse(User.FindFirst("sub")!.Value), false, request.Reason, cancellationToken));
}
