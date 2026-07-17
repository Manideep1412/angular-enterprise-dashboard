namespace EnterpriseDashboard.Api.DTOs.Auth;

public record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    UserInfo User
);

public record UserInfo(
    int Id,
    string FullName,
    string Email,
    string Department,
    IEnumerable<string> Roles
);
