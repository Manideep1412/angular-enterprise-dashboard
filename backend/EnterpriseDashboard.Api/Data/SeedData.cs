using EnterpriseDashboard.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDashboard.Api.Data;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (await db.Roles.AnyAsync()) return; // Already seeded

        // Seed roles
        var adminRole = new Role { Name = "Admin", Description = "Full system access", Color = "#ef4444" };
        var managerRole = new Role { Name = "Manager", Description = "Team and reporting access", Color = "#f59e0b" };
        var viewerRole = new Role { Name = "Viewer", Description = "Read-only access", Color = "#22c55e" };
        var developerRole = new Role { Name = "Developer", Description = "Development and API access", Color = "#4f8ef7" };

        db.Roles.AddRange(adminRole, managerRole, viewerRole, developerRole);

        // Seed role permissions
        var adminPermissions = new List<RolePermission>
        {
            new() { Role = adminRole, Resource = "users", Action = "read" },
            new() { Role = adminRole, Resource = "users", Action = "write" },
            new() { Role = adminRole, Resource = "users", Action = "delete" },
            new() { Role = adminRole, Resource = "roles", Action = "read" },
            new() { Role = adminRole, Resource = "roles", Action = "write" },
            new() { Role = adminRole, Resource = "roles", Action = "delete" },
            new() { Role = adminRole, Resource = "audit", Action = "read" },
            new() { Role = adminRole, Resource = "audit", Action = "export" },
        };

        var managerPermissions = new List<RolePermission>
        {
            new() { Role = managerRole, Resource = "users", Action = "read" },
            new() { Role = managerRole, Resource = "users", Action = "write" },
            new() { Role = managerRole, Resource = "roles", Action = "read" },
            new() { Role = managerRole, Resource = "audit", Action = "read" },
        };

        var viewerPermissions = new List<RolePermission>
        {
            new() { Role = viewerRole, Resource = "users", Action = "read" },
            new() { Role = viewerRole, Resource = "roles", Action = "read" },
            new() { Role = viewerRole, Resource = "audit", Action = "read" },
        };

        var devPermissions = new List<RolePermission>
        {
            new() { Role = developerRole, Resource = "users", Action = "read" },
            new() { Role = developerRole, Resource = "users", Action = "write" },
            new() { Role = developerRole, Resource = "roles", Action = "read" },
            new() { Role = developerRole, Resource = "audit", Action = "read" },
        };

        db.RolePermissions.AddRange(adminPermissions);
        db.RolePermissions.AddRange(managerPermissions);
        db.RolePermissions.AddRange(viewerPermissions);
        db.RolePermissions.AddRange(devPermissions);

        // Seed users
        var adminUser = new User
        {
            FirstName = "Manideep",
            LastName = "Salla",
            Email = "admin@enterprise.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Status = UserStatus.Active,
            Department = "Engineering",
            CreatedAt = DateTime.UtcNow.AddDays(-120)
        };

        var users = new List<User>
        {
            adminUser,
            new() { FirstName = "Sarah", LastName = "Johnson", Email = "sarah.j@enterprise.dev",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
                    Status = UserStatus.Active, Department = "Operations", CreatedAt = DateTime.UtcNow.AddDays(-90) },
            new() { FirstName = "James", LastName = "Williams", Email = "james.w@enterprise.dev",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Dev@123"),
                    Status = UserStatus.Active, Department = "Engineering", CreatedAt = DateTime.UtcNow.AddDays(-75) },
            new() { FirstName = "Emily", LastName = "Chen", Email = "emily.c@enterprise.dev",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Viewer@123"),
                    Status = UserStatus.Active, Department = "Finance", CreatedAt = DateTime.UtcNow.AddDays(-60) },
            new() { FirstName = "Michael", LastName = "Davis", Email = "michael.d@enterprise.dev",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Dev@123"),
                    Status = UserStatus.Inactive, Department = "Engineering", CreatedAt = DateTime.UtcNow.AddDays(-45) },
            new() { FirstName = "Priya", LastName = "Sharma", Email = "priya.s@enterprise.dev",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
                    Status = UserStatus.Active, Department = "Product", CreatedAt = DateTime.UtcNow.AddDays(-30) },
        };

        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        // Assign roles
        db.UserRoles.AddRange(
            new UserRole { UserId = users[0].Id, RoleId = adminRole.Id },
            new UserRole { UserId = users[1].Id, RoleId = managerRole.Id },
            new UserRole { UserId = users[2].Id, RoleId = developerRole.Id },
            new UserRole { UserId = users[3].Id, RoleId = viewerRole.Id },
            new UserRole { UserId = users[4].Id, RoleId = developerRole.Id },
            new UserRole { UserId = users[5].Id, RoleId = managerRole.Id }
        );

        // Seed audit logs
        var auditLogs = new List<AuditLog>
        {
            new() { UserId = users[0].Id, UserEmail = users[0].Email, Action = AuditAction.Login, Resource = "auth", Description = "Admin login from Canada", Severity = AuditSeverity.Info, CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new() { UserId = users[0].Id, UserEmail = users[0].Email, Action = AuditAction.Create, Resource = "users", ResourceId = users[2].Id.ToString(), Description = "Created user james.w@enterprise.dev", Severity = AuditSeverity.Info, CreatedAt = DateTime.UtcNow.AddDays(-75) },
            new() { UserId = users[1].Id, UserEmail = users[1].Email, Action = AuditAction.Login, Resource = "auth", Description = "Manager login", Severity = AuditSeverity.Info, CreatedAt = DateTime.UtcNow.AddHours(-5) },
            new() { UserId = users[0].Id, UserEmail = users[0].Email, Action = AuditAction.Update, Resource = "users", ResourceId = users[4].Id.ToString(), Description = "Deactivated user michael.d@enterprise.dev", Severity = AuditSeverity.Warning, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { UserId = users[2].Id, UserEmail = users[2].Email, Action = AuditAction.View, Resource = "audit", Description = "Viewed audit logs", Severity = AuditSeverity.Info, CreatedAt = DateTime.UtcNow.AddHours(-8) },
            new() { UserId = users[0].Id, UserEmail = users[0].Email, Action = AuditAction.Delete, Resource = "users", Description = "Deleted inactive user account", Severity = AuditSeverity.Critical, CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new() { UserId = users[1].Id, UserEmail = users[1].Email, Action = AuditAction.Export, Resource = "users", Description = "Exported user list to CSV", Severity = AuditSeverity.Info, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { UserId = users[5].Id, UserEmail = users[5].Email, Action = AuditAction.Login, Resource = "auth", Description = "Manager login from mobile", Severity = AuditSeverity.Info, CreatedAt = DateTime.UtcNow.AddHours(-1) },
        };

        db.AuditLogs.AddRange(auditLogs);
        await db.SaveChangesAsync();
    }
}
