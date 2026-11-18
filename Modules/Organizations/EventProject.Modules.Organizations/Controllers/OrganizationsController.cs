using EventProject.Modules.Organizations.Core.Application.DTOs;
using EventProject.Modules.Organizations.Core.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventProject.Modules.Organizations.Controllers;

[ApiController]
[Route("api/organizations")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrganizationsController(IOrganizationService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("statuses")]
    public async Task<ActionResult<EventOrganizationStatusResponse[]>> Statuses(CancellationToken cancellationToken) =>
        Ok(await service.ListStatusesAsync(cancellationToken));

    [AllowAnonymous]
    [EnableRateLimiting("organization-registration")]
    [HttpPost("register")]
    public async Task<ActionResult<RegistrationResponse>> Register(RegisterOrganizationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RegisterAsync(request, cancellationToken);
        // The authenticated owner can resolve their organization through /me after login.
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [Authorize(Policy = "OrganizationProfile")]
    [HttpGet("me")]
    public async Task<ActionResult<OrganizationResponse>> Me(CancellationToken cancellationToken) =>
        Ok((await service.GetOwnAsync(Guid.Parse(User.FindFirst("sub")!.Value), cancellationToken)).Organization);

    [Authorize(Policy = "OrganizationProfile")]
    [HttpGet("me/membership")]
    public async Task<ActionResult<MembershipResponse>> Membership(CancellationToken cancellationToken) =>
        Ok((await service.GetOwnAsync(Guid.Parse(User.FindFirst("sub")!.Value), cancellationToken)).Membership);
}
