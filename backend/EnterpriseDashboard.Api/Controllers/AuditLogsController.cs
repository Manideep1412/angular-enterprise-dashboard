using EnterpriseDashboard.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDashboard.Api.Controllers;

/// <summary>Audit log viewer — immutable record of all privileged actions.</summary>
[ApiController]
[Authorize]
[Route("api/v1/audit-logs")]
[Produces("application/json")]
public class AuditLogsController(IAuditService auditService) : ControllerBase
{
    /// <summary>Return a paginated, filtered list of audit log entries.</summary>
    /// <remarks>
    /// Every Create, Update, and Delete operation on users is recorded here automatically.
    /// Entries are immutable — they cannot be edited or deleted via the API.
    ///
    /// **Filtering**
    /// - <c>action</c>: one or more of <c>Create</c>, <c>Update</c>, <c>Delete</c>, <c>Login</c>.
    /// - <c>severity</c>: one or more of <c>Info</c>, <c>Warning</c>, <c>Critical</c>.
    ///   Delete operations are logged at **Warning** severity.
    /// - <c>search</c>: free-text match against actor email, resource, and description.
    ///
    /// **Sorting**
    /// - <c>sortBy</c>: <c>createdAt</c>, <c>userEmail</c>, <c>action</c>, <c>severity</c>.
    /// - <c>sortDir</c>: <c>asc</c> or <c>desc</c> (default: <c>desc</c>).
    ///
    /// Requires any authenticated role (Admin or Manager).
    /// </remarks>
    /// <param name="page">Page number, 1-based (default: 1).</param>
    /// <param name="pageSize">Records per page (default: 10).</param>
    /// <param name="action">Filter by action type(s): Create, Update, Delete, Login.</param>
    /// <param name="severity">Filter by severity level(s): Info, Warning, Critical.</param>
    /// <param name="search">Free-text search across actor email, resource, and description.</param>
    /// <param name="sortBy">Field to sort by (default: createdAt).</param>
    /// <param name="sortDir">Sort direction: asc or desc.</param>
    /// <response code="200">Paged list of audit log entries with total count.</response>
    /// <response code="401">Missing or invalid Bearer token.</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] List<string>? action = null,
        [FromQuery] List<string>? severity = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = "desc")
    {
        var result = await auditService.GetLogsAsync(page, pageSize, action, severity, search, sortBy, sortDir);
        return Ok(new { success = true, data = result });
    }
}
