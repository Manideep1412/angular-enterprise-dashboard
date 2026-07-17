using EnterpriseDashboard.Api.DTOs.Auth;

namespace EnterpriseDashboard.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, string? ipAddress);
}
