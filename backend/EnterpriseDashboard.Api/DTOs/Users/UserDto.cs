namespace EnterpriseDashboard.Api.DTOs.Users;

public record UserDto(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string Status,
    string? Department,
    IEnumerable<string> Roles,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Department,
    IEnumerable<int> RoleIds
);

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    string? Department,
    string Status,
    IEnumerable<int> RoleIds
);
