using EnterpriseDashboard.Api.DTOs.Users;
using EnterpriseDashboard.Api.Entities;
using EnterpriseDashboard.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDashboard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class UsersController(IUserService userService, IAuditService auditService) : ControllerBase
{
    [HttpGet]
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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await userService.GetByIdAsync(id);
        if (user is null) return NotFound(new { success = false, message = "User not found" });
        return Ok(new { success = true, data = user });
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = await userService.CreateAsync(request);
        var actorEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "system";
        var actorId = int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : (int?)null;
        await auditService.LogAsync(actorId, actorEmail, AuditAction.Create, "users",
            $"Created user {user.Email}", user.Id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new { success = true, data = user });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerOrAdmin")]
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

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
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
