using EnterpriseDashboard.Api.Data;
using EnterpriseDashboard.Api.DTOs.Auth;
using EnterpriseDashboard.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EnterpriseDashboard.Api.Services;

public class AuthService(AppDbContext db, IConfiguration config, ILogger<AuthService> logger) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, string? ipAddress)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return null;
        }

        if (user.Status != UserStatus.Active)
        {
            logger.LogWarning("Login attempt for inactive user {Email}", request.Email);
            return null;
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;

        // Log audit
        db.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            UserEmail = user.Email,
            Action = AuditAction.Login,
            Resource = "auth",
            Description = $"Successful login from {ipAddress ?? "unknown"}",
            IpAddress = ipAddress,
            Severity = AuditSeverity.Info
        });

        await db.SaveChangesAsync();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var token = GenerateJwtToken(user, roles);

        var expiryMinutes = config.GetValue<int>("Jwt:ExpiryMinutes", 60);

        return new LoginResponse(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: expiryMinutes * 60,
            User: new UserInfo(
                Id: user.Id,
                FullName: user.FullName,
                Email: user.Email,
                Department: user.Department ?? string.Empty,
                Roles: roles
            )
        );
    }

    private string GenerateJwtToken(Entities.User user, List<string> roles)
    {
        var jwtConfig = config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = jwtConfig.GetValue<int>("ExpiryMinutes", 60);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Add role claims
        foreach (var role in roles)
            claims.Add(new Claim("role", role));

        var token = new JwtSecurityToken(
            issuer: jwtConfig["Issuer"],
            audience: jwtConfig["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
