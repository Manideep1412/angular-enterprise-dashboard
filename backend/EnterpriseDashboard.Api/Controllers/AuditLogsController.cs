using EnterpriseDashboard.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDashboard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/audit-logs")]
public class AuditLogsController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
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
