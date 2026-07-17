namespace EnterpriseDashboard.Api.Entities;

public enum AuditAction { Login, Logout, Create, Update, Delete, View, Export }
public enum AuditSeverity { Info, Warning, Critical }

public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public AuditSeverity Severity { get; set; } = AuditSeverity.Info;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
