using EnterpriseDashboard.Api.Data;
using EnterpriseDashboard.Api.DTOs.Common;
using EnterpriseDashboard.Api.DTOs.Users;
using EnterpriseDashboard.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDashboard.Api.Services;

public class UserService(AppDbContext db, ILogger<UserService> logger) : IUserService
{
    public async Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search, List<string>? statuses, List<string>? roles, string? sortBy, string? sortDir)
    {
        var query = db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s) ||
                (u.Department != null && u.Department.ToLower().Contains(s)));
        }

        if (statuses?.Any() == true)
        {
            var parsed = statuses
                .Where(s => Enum.TryParse<UserStatus>(s, true, out _))
                .Select(s => Enum.Parse<UserStatus>(s, true))
                .ToList();
            if (parsed.Any()) query = query.Where(u => parsed.Contains(u.Status));
        }

        if (roles?.Any() == true)
        {
            var rolesLower = roles.Select(r => r.ToLower()).ToList();
            query = query.Where(u => u.UserRoles.Any(ur => rolesLower.Contains(ur.Role.Name.ToLower())));
        }

        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        query = (sortBy?.ToLower() switch
        {
            "firstname" or "name" => desc ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
            "email"               => desc ? query.OrderByDescending(u => u.Email)     : query.OrderBy(u => u.Email),
            "department"          => desc ? query.OrderByDescending(u => u.Department) : query.OrderBy(u => u.Department),
            "status"              => desc ? query.OrderByDescending(u => u.Status)    : query.OrderBy(u => u.Status),
            "lastloginat"         => desc ? query.OrderByDescending(u => u.LastLoginAt) : query.OrderBy(u => u.LastLoginAt),
            "createdat"           => desc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            _                     => query.OrderByDescending(u => u.CreatedAt),
        });

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<UserDto>(
            items.Select(ToDto),
            totalCount, page, pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? null : ToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Department = request.Department,
            Status = UserStatus.Active
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        foreach (var roleId in request.RoleIds)
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        await db.SaveChangesAsync();

        return await GetByIdAsync(user.Id) ?? ToDto(user);
    }

    public async Task<UserDto?> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return null;

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Department = request.Department;
        if (Enum.TryParse<UserStatus>(request.Status, true, out var s)) user.Status = s;

        db.UserRoles.RemoveRange(user.UserRoles);
        foreach (var roleId in request.RoleIds)
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });

        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return false;
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        logger.LogInformation("Deleted user {UserId}", id);
        return true;
    }

    public async Task<object> GetStatsAsync()
    {
        var total  = await db.Users.CountAsync();
        var active = await db.Users.CountAsync(u => u.Status == UserStatus.Active);
        var inactive = await db.Users.CountAsync(u => u.Status == UserStatus.Inactive);
        var recentLogins = await db.Users.CountAsync(u => u.LastLoginAt >= DateTime.UtcNow.AddDays(-7));
        var roleDistribution = await db.UserRoles
            .GroupBy(ur => ur.Role.Name)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync();
        return new { total, active, inactive, recentLogins, roleDistribution };
    }

    private static UserDto ToDto(User u) => new(
        u.Id, u.FirstName, u.LastName, u.FullName, u.Email,
        u.Status.ToString(), u.Department,
        u.UserRoles.Select(ur => ur.Role.Name),
        u.CreatedAt, u.LastLoginAt);
}
