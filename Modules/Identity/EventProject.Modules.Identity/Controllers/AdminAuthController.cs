using EventProject.Modules.Identity.Core.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventProject.Modules.Identity.Controllers;

[ApiController]
[Route("api/admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminAuthController(AdminAuthService service) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("admin-login")]
    [HttpPost("auth/login")]
    public async Task<ActionResult<AdminLoginResponse>> Login(
        AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await service.LoginAsync(request, cancellationToken);
        return result is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials or unavailable account.")
            : Ok(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("me")]
    public async Task<ActionResult<AdminProfileResponse>> Me(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var id)) return Unauthorized();
        var profile = await service.GetProfileAsync(id, cancellationToken);
        return profile is null ? Unauthorized() : Ok(profile);
    }
}
