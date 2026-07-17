using EnterpriseDashboard.Api.Data;
using EnterpriseDashboard.Api.DTOs.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDashboard.Api.Controllers;

/// <summary>Role management — read seeded roles and their permissions.</summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class RolesController(AppDbContext db) : ControllerBase
{
    /// <summary>Return all roles with their permissions and member counts.</summary>
    /// <remarks>
    /// Returns every role seeded in the database, ordered alphabetically by name.
    /// Each role includes its full permission list (resource + action pairs) and
    /// the number of users currently assigned to it.
    ///
    /// Requires any authenticated role (Admin or Manager).
    /// </remarks>
    /// <response code="200">List of roles with permissions and member counts.</response>
    /// <response code="401">Missing or invalid Bearer token.</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var roles = await db.Roles
            .Include(r => r.UserRoles)
            .Include(r => r.Permissions)
            .OrderBy(r => r.Name)
            .ToListAsync();

        var dtos = roles.Select(r => new RoleDto(
            r.Id, r.Name, r.Description, r.Color,
            r.UserRoles.Count,
            r.Permissions.Select(p => new PermissionDto(p.Resource, p.Action)),
            r.CreatedAt
        ));

        return Ok(new { success = true, data = dtos });
    }

    /// <summary>Return a single role by ID, including the list of assigned users.</summary>
    /// <remarks>
    /// In addition to permissions, this endpoint includes the full user list for
    /// the role — useful for the role detail view in the dashboard.
    ///
    /// Requires any authenticated role (Admin or Manager).
    /// </remarks>
    /// <param name="id">The numeric role ID.</param>
    /// <response code="200">Role detail with permissions and assigned users.</response>
    /// <response code="401">Missing or invalid Bearer token.</response>
    /// <response code="404">No role found with the given ID.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var role = await db.Roles
            .Include(r => r.UserRoles).ThenInclude(ur => ur.User)
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role is null) return NotFound(new { success = false, message = "Role not found" });

        var dto = new RoleDto(
            role.Id, role.Name, role.Description, role.Color,
            role.UserRoles.Count,
            role.Permissions.Select(p => new PermissionDto(p.Resource, p.Action)),
            role.CreatedAt
        );

        return Ok(new { success = true, data = dto });
    }
}
