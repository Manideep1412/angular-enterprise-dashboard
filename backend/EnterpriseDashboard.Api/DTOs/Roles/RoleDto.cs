namespace EnterpriseDashboard.Api.DTOs.Roles;

public record RoleDto(
    int Id,
    string Name,
    string Description,
    string Color,
    int UserCount,
    IEnumerable<PermissionDto> Permissions,
    DateTime CreatedAt
);

public record PermissionDto(string Resource, string Action);
