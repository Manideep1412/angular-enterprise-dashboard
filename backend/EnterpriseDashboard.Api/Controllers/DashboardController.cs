using EnterpriseDashboard.Api.Data;
using EnterpriseDashboard.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDashboard.Api.Controllers;

/// <summary>Dashboard statistics — KPIs, charts, and recent activity feed.</summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class DashboardController(AppDbContext db) : ControllerBase
{
    /// <summary>Return aggregated KPIs, activity chart data, and recent audit events.</summary>
    /// <remarks>
    /// This is the primary data source for the dashboard home page. Returns:
    ///
    /// - **kpis**: total users, active users, total roles, and audit events logged today.
    /// - **activityChart**: event count per day for the last 7 days (for the line/bar chart).
    /// - **roleDistribution**: number of users in each role (for the pie/doughnut chart).
    /// - **recentActivity**: the 5 most recent audit log entries for the activity feed.
    ///
    /// All counts are computed in real time from the database.
    /// Requires any authenticated role (Admin or Manager).
    /// </remarks>
    /// <response code="200">Dashboard payload with KPIs, chart series, and recent activity.</response>
    /// <response code="401">Missing or invalid Bearer token.</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStats()
    {
        var totalUsers = await db.Users.CountAsync();
        var activeUsers = await db.Users.CountAsync(u => u.Status == UserStatus.Active);
        var totalRoles = await db.Roles.CountAsync();
        var auditLogsToday = await db.AuditLogs
            .CountAsync(l => l.CreatedAt >= DateTime.UtcNow.Date);

        var last7Days = Enumerable.Range(0, 7)
            .Select(i => DateTime.UtcNow.Date.AddDays(-i))
            .Reverse()
            .ToList();

        var activityData = await db.AuditLogs
            .Where(l => l.CreatedAt >= DateTime.UtcNow.Date.AddDays(-6))
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var activityChart = last7Days.Select(d => new
        {
            date = d.ToString("MMM dd"),
            count = activityData.FirstOrDefault(a => a.Date == d)?.Count ?? 0
        });

        var roleDistribution = await db.UserRoles
            .GroupBy(ur => ur.Role.Name)
            .Select(g => new { role = g.Key, count = g.Count() })
            .ToListAsync();

        var recentActivity = await db.AuditLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(5)
            .Select(l => new
            {
                l.Id,
                l.UserEmail,
                action = l.Action.ToString(),
                l.Resource,
                l.Description,
                severity = l.Severity.ToString(),
                l.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                kpis = new
                {
                    totalUsers,
                    activeUsers,
                    totalRoles,
                    auditLogsToday,
                    userGrowth = "+12%",
                    activeSessionsGrowth = "+5%"
                },
                activityChart,
                roleDistribution,
                recentActivity
            }
        });
    }
}
