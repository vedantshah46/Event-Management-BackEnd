using EventProject.Modules.Organizations.Core.Application.DTOs;
using EventProject.Modules.Organizations.Core.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventProject.Modules.Organizations.Controllers;

[ApiController]
[Route("api/organizer/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrganizerAuthController(IOrganizationService service, IOrganizationStaffService staff) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("organizer-login")]
    [HttpPost("login")]
    public async Task<ActionResult<OrganizerLoginResponse>> Login(OrganizerLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await service.LoginAsync(request, cancellationToken);
        return result is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials or unavailable account.")
            : Ok(result);
    }
    [Authorize(Roles = "Organizer")]
    [HttpGet("session")]
    public async Task<ActionResult<OrganizationAccess>> Session(CancellationToken ct) =>
        Ok(await service.GetOwnAsync(Guid.Parse(User.FindFirst("sub")!.Value), ct));

    [Authorize(Roles = "Organizer")]
    [EnableRateLimiting("organizer-login")]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangeOrganizerPasswordRequest request, CancellationToken ct)
    {
        await staff.ChangePasswordAsync(Guid.Parse(User.FindFirst("sub")!.Value), request, ct);
        return Ok(new { message = "Password changed. Sign in again." });
    }
}
