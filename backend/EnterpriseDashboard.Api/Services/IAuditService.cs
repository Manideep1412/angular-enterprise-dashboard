using EnterpriseDashboard.Api.DTOs.AuditLogs;
using EnterpriseDashboard.Api.DTOs.Common;
using EnterpriseDashboard.Api.Entities;

namespace EnterpriseDashboard.Api.Services;

public interface IAuditService
{
    Task<PagedResult<AuditLogDto>> GetLogsAsync(int page, int pageSize, List<string>? actions, List<string>? severities, string? search, string? sortBy, string? sortDir);
    Task LogAsync(int? userId, string userEmail, AuditAction action, string resource, string description, string? resourceId = null, string? ipAddress = null, AuditSeverity severity = AuditSeverity.Info);
}
