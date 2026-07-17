using EnterpriseDashboard.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDashboard.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Status).HasConversion<string>();
        });

        // UserRole composite PK
        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(ur => new { ur.UserId, ur.RoleId });
            e.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
            e.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);
        });

        // RolePermission
        modelBuilder.Entity<RolePermission>(e =>
        {
            e.HasOne(rp => rp.Role).WithMany(r => r.Permissions).HasForeignKey(rp => rp.RoleId);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(a => a.Action).HasConversion<string>();
            e.Property(a => a.Severity).HasConversion<string>();
            e.HasOne(a => a.User).WithMany(u => u.AuditLogs)
             .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
