using EnterpriseDashboard.Api.DTOs.Common;
using EnterpriseDashboard.Api.DTOs.Users;

namespace EnterpriseDashboard.Api.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search, List<string>? statuses, List<string>? roles, string? sortBy, string? sortDir);
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserDto> CreateAsync(CreateUserRequest request);
    Task<UserDto?> UpdateAsync(int id, UpdateUserRequest request);
    Task<bool> DeleteAsync(int id);
    Task<object> GetStatsAsync();
}
