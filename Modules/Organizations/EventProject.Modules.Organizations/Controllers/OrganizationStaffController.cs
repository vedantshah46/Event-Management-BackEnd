using EventProject.Modules.Organizations.Core.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventProject.Modules.Organizations.Controllers;

[ApiController]
[Authorize(Policy = "ApprovedOrganization")]
[Route("api/organizations/me/staff")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrganizationStaffController(IOrganizationStaffService service) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirst("sub")!.Value);
    [HttpGet]
    public async Task<ActionResult<StaffResponse[]>> List(CancellationToken ct) => Ok(await service.ListAsync(UserId, ct));
    [HttpGet("roles")]
    public async Task<ActionResult<StaffRoleResponse[]>> Roles(CancellationToken ct) => Ok(await service.RolesAsync(UserId, ct));
    [HttpPost]
    public async Task<ActionResult<StaffResponse>> Create(CreateStaffRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreateAsync(UserId, request, ct));
    [HttpPatch("{membershipId:guid}/role")]
    public async Task<IActionResult> Role(Guid membershipId, ChangeStaffRoleRequest request, CancellationToken ct)
    {
        await service.SetRoleAsync(UserId, membershipId, request.OrganizationRoleId, ct);
        return NoContent();
    }
    [HttpPatch("{membershipId:guid}/access")]
    public async Task<IActionResult> Access(Guid membershipId, ChangeStaffAccessRequest request, CancellationToken ct)
    {
        await service.SetAccessAsync(UserId, membershipId, request.IsActive, ct);
        return NoContent();
    }
}