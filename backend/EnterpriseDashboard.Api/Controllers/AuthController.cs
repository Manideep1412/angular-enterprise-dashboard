using EnterpriseDashboard.Api.DTOs.Auth;
using EnterpriseDashboard.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDashboard.Api.Controllers;

/// <summary>Authentication — login and current-user info.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Authenticate with email and password and receive a JWT token.</summary>
    /// <remarks>
    /// Returns a signed Bearer token valid for 8 hours. Pass it in all subsequent
    /// requests via the <c>Authorization: Bearer {token}</c> header.
    ///
    /// **Demo credentials**
    ///
    /// | Role    | Email                   | Password    |
    /// |---------|-------------------------|-------------|
    /// | Admin   | admin@enterprise.dev    | Admin@123   |
    /// | Manager | sarah.j@enterprise.dev  | Manager@123 |
    ///
    /// Admins can create, update and delete users. Managers have read-only access.
    /// </remarks>
    /// <param name="request">Email and password credentials.</param>
    /// <response code="200">JWT Bearer token + authenticated user info (id, name, roles).</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await authService.LoginAsync(request, ipAddress);

        if (result is null)
            return Unauthorized(new { success = false, message = "Invalid email or password" });

        return Ok(new { success = true, data = result });
    }

    /// <summary>Return the JWT claims of the currently authenticated user.</summary>
    /// <remarks>
    /// Requires a valid Bearer token. Useful for verifying token contents and
    /// confirming which roles are embedded in the JWT payload.
    /// </remarks>
    /// <response code="200">Dictionary of claim type → value for the current user.</response>
    /// <response code="401">Missing or invalid Bearer token.</response>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var claims = User.Claims.ToDictionary(c => c.Type, c => c.Value);
        return Ok(new { success = true, data = claims });
    }
}
