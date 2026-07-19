using EnterpriseDashboard.Api.DTOs.AuditLogs;
using EnterpriseDashboard.Api.DTOs.Auth;
using EnterpriseDashboard.Api.DTOs.Common;
using EnterpriseDashboard.Api.DTOs.Roles;
using EnterpriseDashboard.Api.DTOs.Users;
using EnterpriseDashboard.Api.Entities;
using EnterpriseDashboard.Tests.Helpers;

namespace EnterpriseDashboard.Tests.Misc;

/// <summary>
/// Covers DTO and entity property getters that are defined in production code
/// but aren't exercised by the service/controller tests (e.g. rarely-read
/// record properties, unused ApiResponse wrappers, entity navigation getters).
/// </summary>
public class DtoCoverageTests
{
    // ── UserDto — uncovered property getters ──────────────────────────────────

    [Fact]
    public void UserDto_LastName_FullName_CreatedAt_LastLoginAt_AllReadable()
    {
        var now = DateTime.UtcNow;
        var dto = new UserDto(1, "Jane", "Doe", "Jane Doe", "jane@x.com",
            "Active", "Engineering", ["Admin"], now, now.AddHours(-1));

        dto.LastName.Should().Be("Doe");
        dto.FullName.Should().Be("Jane Doe");
        dto.CreatedAt.Should().Be(now);
        dto.LastLoginAt.Should().Be(now.AddHours(-1));
    }

    // ── AuditLogDto — Id and CreatedAt ────────────────────────────────────────

    [Fact]
    public void AuditLogDto_Id_And_CreatedAt_Readable()
    {
        var now = DateTime.UtcNow;
        // Constructor: Id, UserEmail, Action, Resource, ResourceId, Description, IpAddress, Severity, CreatedAt
        var dto = new AuditLogDto(7, "user@x.com", "Create", "users",
            "42", "Created something", "127.0.0.1", "Info", now);

        dto.Id.Should().Be(7);
        dto.CreatedAt.Should().Be(now);
    }

    // ── RoleDto — Id, Description, Color, CreatedAt ──────────────────────────

    [Fact]
    public void RoleDto_Id_Description_Color_CreatedAt_AllReadable()
    {
        var now = DateTime.UtcNow;
        var dto = new RoleDto(3, "Admin", "Administrator role", "#FF5733",
            5, [new PermissionDto("users", "write")], now);

        dto.Id.Should().Be(3);
        dto.Description.Should().Be("Administrator role");
        dto.Color.Should().Be("#FF5733");
        dto.CreatedAt.Should().Be(now);
    }

    // ── PagedResult — PageSize ────────────────────────────────────────────────

    [Fact]
    public void PagedResult_PageSize_Readable()
    {
        var result = new PagedResult<string>(["a", "b", "c"], 20, 2, 10, 2);

        result.PageSize.Should().Be(10);
    }

    // ── ApiResponse / ApiResponse<T> — Success getter ────────────────────────

    [Fact]
    public void ApiResponse_Success_Readable()
    {
        var plain = new ApiResponse(true, "All good");
        var generic = new ApiResponse<string>(false, null, "Error occurred");

        plain.Success.Should().BeTrue();
        generic.Success.Should().BeFalse();
    }

    // ── UserInfo — Id getter ──────────────────────────────────────────────────

    [Fact]
    public void UserInfo_Id_Readable()
    {
        var info = new UserInfo(42, "John Doe", "john@x.com", "Engineering", ["Admin"]);

        info.Id.Should().Be(42);
    }

    // ── User.AvatarUrl entity property ───────────────────────────────────────

    [Fact]
    public void User_AvatarUrl_Readable()
    {
        using var db = DbContextFactory.Create();
        var user = SeedHelper.CreateUser(db, email: "avatar@x.com");
        user.AvatarUrl = "https://cdn.example.com/avatars/42.png";
        db.SaveChanges();

        var stored = db.Users.Find(user.Id)!;
        stored.AvatarUrl.Should().Be("https://cdn.example.com/avatars/42.png");
    }

    // ── AuditLog.User navigation property getter ─────────────────────────────

    [Fact]
    public void AuditLog_User_NavigationGetter_DoesNotThrow()
    {
        using var db = DbContextFactory.Create();
        var log = SeedHelper.CreateAuditLog(db, userEmail: "nav@x.com");

        // Accessing the getter without explicit load → returns null (lazy loading disabled)
        // The important thing is the getter itself is invoked and covered
        var _ = log.User;
    }

    // ── RolePermission.Id entity property ────────────────────────────────────

    [Fact]
    public void RolePermission_Id_Readable()
    {
        using var db = DbContextFactory.Create();
        var role = SeedHelper.CreateRole(db, "Viewer");
        db.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id,
            Resource = "dashboard",
            Action = "read"
        });
        db.SaveChanges();

        db.RolePermissions.First().Id.Should().BePositive();
    }
}
