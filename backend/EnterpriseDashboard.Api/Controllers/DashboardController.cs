using EnterpriseDashboard.Api.Data;
using EnterpriseDashboard.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDashboard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalUsers = await db.Users.CountAsync();
        var activeUsers = await db.Users.CountAsync(u => u.Status == UserStatus.Active);
        var totalRoles = await db.Roles.CountAsync();
        var auditLogsToday = await db.AuditLogs
            .CountAsync(l => l.CreatedAt >= DateTime.UtcNow.Date);

        // Activity over last 7 days
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

        // Role distribution
        var roleDistribution = await db.UserRoles
            .GroupBy(ur => ur.Role.Name)
            .Select(g => new { role = g.Key, count = g.Count() })
            .ToListAsync();

        // Recent audit logs
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
