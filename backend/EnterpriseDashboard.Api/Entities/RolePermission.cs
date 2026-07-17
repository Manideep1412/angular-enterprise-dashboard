namespace EnterpriseDashboard.Api.Entities;

public class RolePermission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public string Resource { get; set; } = string.Empty; // e.g. "users", "roles", "audit"
    public string Action { get; set; } = string.Empty;   // e.g. "read", "write", "delete"
}
