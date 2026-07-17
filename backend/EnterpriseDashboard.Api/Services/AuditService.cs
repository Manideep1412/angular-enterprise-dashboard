using EnterpriseDashboard.Api.Data;
using EnterpriseDashboard.Api.DTOs.AuditLogs;
using EnterpriseDashboard.Api.DTOs.Common;
using EnterpriseDashboard.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDashboard.Api.Services;

public class AuditService(AppDbContext db) : IAuditService
{
    public async Task<PagedResult<AuditLogDto>> GetLogsAsync(int page, int pageSize, List<string>? actions, List<string>? severities, string? search, string? sortBy, string? sortDir)
    {
        var query = db.AuditLogs.AsQueryable();

        if (actions?.Any() == true)
        {
            var parsed = actions
                .Where(a => Enum.TryParse<AuditAction>(a, true, out _))
                .Select(a => Enum.Parse<AuditAction>(a, true))
                .ToList();
            if (parsed.Any()) query = query.Where(l => parsed.Contains(l.Action));
        }

        if (severities?.Any() == true)
        {
            var parsed = severities
                .Where(s => Enum.TryParse<AuditSeverity>(s, true, out _))
                .Select(s => Enum.Parse<AuditSeverity>(s, true))
                .ToList();
            if (parsed.Any()) query = query.Where(l => parsed.Contains(l.Severity));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            query = query.Where(l =>
                l.UserEmail.ToLower().Contains(q) ||
                l.Description.ToLower().Contains(q) ||
                l.Resource.ToLower().Contains(q));
        }

        bool desc = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase); // default desc

        query = (sortBy?.ToLower() switch
        {
            "useremail" or "user" => desc ? query.OrderByDescending(l => l.UserEmail) : query.OrderBy(l => l.UserEmail),
            "action"              => desc ? query.OrderByDescending(l => l.Action)    : query.OrderBy(l => l.Action),
            "resource"            => desc ? query.OrderByDescending(l => l.Resource)  : query.OrderBy(l => l.Resource),
            "severity"            => desc ? query.OrderByDescending(l => l.Severity)  : query.OrderBy(l => l.Severity),
            "createdat" or "time" => desc ? query.OrderByDescending(l => l.CreatedAt) : query.OrderBy(l => l.CreatedAt),
            _                     => query.OrderByDescending(l => l.CreatedAt),
        });

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AuditLogDto>(
            items.Select(l => new AuditLogDto(
                l.Id, l.UserEmail, l.Action.ToString(), l.Resource,
                l.ResourceId, l.Description, l.IpAddress, l.Severity.ToString(), l.CreatedAt)),
            totalCount, page, pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task LogAsync(int? userId, string userEmail, AuditAction action, string resource,
        string description, string? resourceId = null, string? ipAddress = null,
        AuditSeverity severity = AuditSeverity.Info)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId, UserEmail = userEmail, Action = action,
            Resource = resource, ResourceId = resourceId, Description = description,
            IpAddress = ipAddress, Severity = severity
        });
        await db.SaveChangesAsync();
    }
}
