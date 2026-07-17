using EnterpriseDashboard.Api.Data;
using EnterpriseDashboard.Api.DTOs.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDashboard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class RolesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
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

    [HttpGet("{id:int}")]
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
