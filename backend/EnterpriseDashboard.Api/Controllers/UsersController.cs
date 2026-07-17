using EnterpriseDashboard.Api.DTOs.Users;
using EnterpriseDashboard.Api.Entities;
using EnterpriseDashboard.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDashboard.Api.Controllers;

/// <summary>User management — CRUD operations with RBAC enforcement.</summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class UsersController(IUserService userService, IAuditService auditService) : ControllerBase
{
    /// <summary>Return a paginated, filtered, and sorted list of users.</summary>
    /// <remarks>
    /// All query parameters are optional. Combine them freely.
    ///
    /// **Filtering**
    /// - <c>search</c> matches against first name, last name, and email (case-insensitive).
    /// - <c>status</c> accepts multiple values: <c>active</c>, <c>inactive</c>, <c>suspended</c>.
    /// - <c>role</c> accepts multiple role names: <c>Admin</c>, <c>Manager</c>, <c>Viewer</c>.
    ///
    /// **Sorting**
    /// - <c>sortBy</c>: <c>firstName</c>, <c>lastName</c>, <c>email</c>, <c>createdAt</c>, <c>lastLoginAt</c>.
    /// - <c>sortDir</c>: <c>asc</c> or <c>desc</c> (default: <c>desc</c>).
    ///
    /// Requires any authenticated role (Admin or Manager).
    /// </remarks>
    /// <param name="page">Page number, 1-based (default: 1).</param>
    /// <param name="pageSize">Number of records per page (default: 10, max: 100).</param>
    /// <param name="search">Free-text search across name and email fields.</param>
    /// <param name="status">Filter by one or more statuses: active, inactive, suspended.</param>
    /// <param name="role">Filter by one or more role names.</param>
    /// <param name="sortBy">Field to sort by.</param>
    /// <param name="sortDir">Sort direction: asc or desc.</param>
    /// <response code="200">Paged result containing matching users and total count.</response>
    /// <response code="401">Missing or invalid Bearer token.</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] List<string>? status = null,
        [FromQuery] List<string>? role = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = "desc")
    {
        var result = await userService.GetUsersAsync(page, pageSize, search, status, role, sortBy, sortDir);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Return a single user by their numeric ID.</summary>
    /// <remarks>
    /// Returns full profile including roles and last login timestamp.
    /// Requires any authenticated role.
    /// </remarks>
    /// <param name="id">The numeric user ID.</param>
    /// <response code="200">Full user profile.</response>
    /// <response code="401">Missing or invalid Bearer token.</response>
    /// <response code="404">No user found with the given ID.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await userService.GetByIdAsync(id);
        if (user is null) return NotFound(new { success = false, message = "User not found" });
        return Ok(new { success = true, data = user });
    }

    /// <summary>Create a new user and assign them to one or more roles.</summary>
    /// <remarks>
    /// **Required role: Admin or Manager.**
    ///
    /// The password is hashed server-side before storage — never stored in plain text.
    /// Provide one or more <c>roleIds</c> from the <c>GET /api/v1/roles</c> response.
    /// The action is recorded in the audit log.
    ///
    /// Example request body:
    /// ```json
    /// {
    ///   "firstName": "Jane",
    ///   "lastName": "Smith",
    ///   "email": "jane.smith@enterprise.dev",
    ///   "password": "SecurePass@123",
    ///   "department": "Engineering",
    ///   "roleIds": [2]
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">New user details including role assignments.</param>
    /// <response code="201">User created. Location header points to the new resource.</response>
    /// <response code="400">Validation failed (missing required fields, weak password, etc.).</response>
    /// <response code="401">Missing or invalid Bearer token.</response>
    /// <response code="403">Caller does not have Manager or Admin role.</response>
    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = await userService.CreateAsync(request);
        var actorEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "system";
        var actorId = int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : (int?)null;
        await auditService.LogAsync(actorId, actorEmail, AuditAction.Create, "users",
            $"Created user {user.Email}", user.Id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new { success = true, data = user });
    }

    /// <summary>Update an existing user's profile and role assignments.</summary>
    /// <remarks>
    /// **Required role: Admin or Manager.**
    ///
    /// Replaces the user's department, status, and role list in full (not partial patch).
    /// The action is recorded in the audit log with the caller's identity.
    ///
    /// Accepted values for <c>status</c>: <c>active</c>, <c>inactive</c>, <c>suspended</c>.
    /// </remarks>
    /// <param name="id">The numeric ID of the user to update.</param>
    /// <param name="request">Updated fields — all fields are required.</param>
    /// <response code="200">Updated user profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Missing or invalid Bearer token.</response>
    /// <response code="403">Caller does not have Manager or Admin role.</response>
    /// <response code="404">No user found with the given ID.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await userService.UpdateAsync(id, request);
        if (user is null) return NotFound(new { success = false, message = "User not found" });
        var actorEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "system";
        var actorId = int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var aId) ? aId : (int?)null;
        await auditService.LogAsync(actorId, actorEmail, AuditAction.Update, "users",
            $"Updated user {user.Email}", id.ToString());
        return Ok(new { success = true, data = user });
    }

    /// <summary>Permanently delete a user by ID.</summary>
    /// <remarks>
    /// **Required role: Admin only.** Managers cannot delete users.
    ///
    /// This is a hard delete — the record is removed from the database.
    /// The deletion is recorded as a **Warning**-severity audit log entry.
    /// </remarks>
    /// <param name="id">The numeric ID of the user to delete.</param>
    /// <response code="200">User deleted successfully.</response>
    /// <response code="401">Missing or invalid Bearer token.</response>
    /// <response code="403">Caller does not have Admin role.</response>
    /// <response code="404">No user found with the given ID.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await userService.DeleteAsync(id);
        if (!deleted) return NotFound(new { success = false, message = "User not found" });
        var actorEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "system";
        var actorId = int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var aId) ? aId : (int?)null;
        await auditService.LogAsync(actorId, actorEmail, AuditAction.Delete, "users",
            $"Deleted user ID {id}", id.ToString(), severity: AuditSeverity.Warning);
        return Ok(new { success = true, message = "User deleted" });
    }
}
