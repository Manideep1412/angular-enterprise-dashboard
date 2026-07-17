namespace EnterpriseDashboard.Api.DTOs.AuditLogs;

public record AuditLogDto(
    int Id,
    string UserEmail,
    string Action,
    string Resource,
    string? ResourceId,
    string Description,
    string? IpAddress,
    string Severity,
    DateTime CreatedAt
);
