using EnterpriseDashboard.Api.Data;
using EnterpriseDashboard.Api.Entities;

namespace EnterpriseDashboard.Tests.Helpers;

/// <summary>Provides reusable seed helpers for unit tests.</summary>
public static class SeedHelper
{
    public static Role CreateRole(AppDbContext db, string name = "Admin", string color = "#4f8ef7")
    {
        var role = new Role { Name = name, Description = $"{name} role", Color = color };
        db.Roles.Add(role);
        db.SaveChanges();
        return role;
    }

    public static User CreateUser(
        AppDbContext db,
        string email = "test@example.com",
        string password = "Password123!",
        UserStatus status = UserStatus.Active,
        string firstName = "Test",
        string lastName = "User",
        string? department = "Engineering",
        int[]? roleIds = null)
    {
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Status = status,
            Department = department,
        };
        db.Users.Add(user);
        db.SaveChanges();

        if (roleIds is not null)
        {
            foreach (var rid in roleIds)
                db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = rid });
            db.SaveChanges();
        }

        // Reload with navigation properties
        db.Entry(user).Collection(u => u.UserRoles).Load();
        foreach (var ur in user.UserRoles)
            db.Entry(ur).Reference(u => u.Role).Load();

        return user;
    }

    public static AuditLog CreateAuditLog(
        AppDbContext db,
        string userEmail = "actor@example.com",
        AuditAction action = AuditAction.Create,
        string resource = "users",
        string description = "Test log",
        AuditSeverity severity = AuditSeverity.Info,
        DateTime? createdAt = null)
    {
        var log = new AuditLog
        {
            UserEmail = userEmail,
            Action = action,
            Resource = resource,
            Description = description,
            Severity = severity,
        };
        if (createdAt.HasValue) log.CreatedAt = createdAt.Value;
        db.AuditLogs.Add(log);
        db.SaveChanges();
        return log;
    }
}
