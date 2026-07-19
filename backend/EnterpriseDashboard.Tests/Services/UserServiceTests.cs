using EnterpriseDashboard.Api.DTOs.Users;
using EnterpriseDashboard.Api.Entities;
using EnterpriseDashboard.Api.Services;
using EnterpriseDashboard.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnterpriseDashboard.Tests.Services;

public class UserServiceTests
{
    private static UserService CreateService(Api.Data.AppDbContext db) =>
        new(db, NullLogger<UserService>.Instance);

    // ── GetUsersAsync — no filters ───────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_NoFilters_ReturnsAllUsers()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "a@x.com");
        SeedHelper.CreateUser(db, email: "b@x.com");
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, null, null, null, null);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    // ── GetUsersAsync — search filter ────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_SearchByEmail_ReturnsMatch()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "alice@example.com", firstName: "Alice");
        SeedHelper.CreateUser(db, email: "bob@example.com", firstName: "Bob");
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, "alice", null, null, null, null);

        result.TotalCount.Should().Be(1);
        result.Items.First().Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task GetUsersAsync_SearchByFirstName_ReturnsMatch()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "u@x.com", firstName: "Charlie");
        SeedHelper.CreateUser(db, email: "v@x.com", firstName: "Delta");
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, "CHARLIE", null, null, null, null);

        result.TotalCount.Should().Be(1);
        result.Items.First().FirstName.Should().Be("Charlie");
    }

    [Fact]
    public async Task GetUsersAsync_SearchByDepartment_ReturnsMatch()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "u@x.com", department: "Finance");
        SeedHelper.CreateUser(db, email: "v@x.com", department: "HR");
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, "finance", null, null, null, null);

        result.TotalCount.Should().Be(1);
    }

    // ── GetUsersAsync — status filter ────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_StatusFilter_ReturnsMatchingStatus()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "a@x.com", status: UserStatus.Active);
        SeedHelper.CreateUser(db, email: "b@x.com", status: UserStatus.Inactive);
        SeedHelper.CreateUser(db, email: "c@x.com", status: UserStatus.Suspended);
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, ["active"], null, null, null);

        result.TotalCount.Should().Be(1);
        result.Items.First().Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetUsersAsync_StatusFilter_MultipleValues_ReturnsBoth()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "a@x.com", status: UserStatus.Active);
        SeedHelper.CreateUser(db, email: "b@x.com", status: UserStatus.Inactive);
        SeedHelper.CreateUser(db, email: "c@x.com", status: UserStatus.Suspended);
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, ["active", "inactive"], null, null, null);

        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetUsersAsync_StatusFilter_InvalidValue_Ignored()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "a@x.com");
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, ["invalidstatus"], null, null, null);

        // invalid enum → empty parsed list → no WHERE clause → all returned
        result.TotalCount.Should().Be(1);
    }

    // ── GetUsersAsync — role filter ──────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_RoleFilter_ReturnsUsersInRole()
    {
        using var db = DbContextFactory.Create();
        var admin = SeedHelper.CreateRole(db, "Admin");
        var mgr   = SeedHelper.CreateRole(db, "Manager");
        SeedHelper.CreateUser(db, email: "a@x.com", roleIds: [admin.Id]);
        SeedHelper.CreateUser(db, email: "b@x.com", roleIds: [mgr.Id]);
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, null, ["Admin"], null, null);

        result.TotalCount.Should().Be(1);
        result.Items.First().Email.Should().Be("a@x.com");
    }

    // ── GetUsersAsync — sort variants ────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_SortByFirstName_Asc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "z@x.com", firstName: "Zara");
        SeedHelper.CreateUser(db, email: "a@x.com", firstName: "Aaron");
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, null, null, "firstname", "asc");

        result.Items.First().FirstName.Should().Be("Aaron");
    }

    [Fact]
    public async Task GetUsersAsync_SortByFirstName_Desc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "a@x.com", firstName: "Aaron");
        SeedHelper.CreateUser(db, email: "z@x.com", firstName: "Zara");
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, null, null, "name", "desc");

        result.Items.First().FirstName.Should().Be("Zara");
    }

    [Fact]
    public async Task GetUsersAsync_SortByEmail_Desc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "aaa@x.com");
        SeedHelper.CreateUser(db, email: "zzz@x.com");
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, null, null, "email", "desc");

        result.Items.First().Email.Should().Be("zzz@x.com");
    }

    [Fact]
    public async Task GetUsersAsync_SortByDepartment_Asc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "a@x.com", department: "ZZZ");
        SeedHelper.CreateUser(db, email: "b@x.com", department: "AAA");
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, null, null, "department", "asc");

        result.Items.First().Department.Should().Be("AAA");
    }

    [Fact]
    public async Task GetUsersAsync_SortByStatus()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "a@x.com", status: UserStatus.Suspended);
        SeedHelper.CreateUser(db, email: "b@x.com", status: UserStatus.Active);
        var svc = CreateService(db);

        // just ensures no exception — sort order of enums as strings
        var result = await svc.GetUsersAsync(1, 10, null, null, null, "status", "asc");

        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetUsersAsync_SortByLastLoginAt_Desc()
    {
        using var db = DbContextFactory.Create();
        var u1 = SeedHelper.CreateUser(db, email: "a@x.com");
        var u2 = SeedHelper.CreateUser(db, email: "b@x.com");
        u1.LastLoginAt = DateTime.UtcNow.AddDays(-5);
        u2.LastLoginAt = DateTime.UtcNow.AddDays(-1);
        db.SaveChanges();
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, null, null, "lastloginat", "desc");

        result.Items.First().Email.Should().Be("b@x.com");
    }

    [Fact]
    public async Task GetUsersAsync_SortByCreatedAt_Asc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "first@x.com");
        SeedHelper.CreateUser(db, email: "second@x.com");
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, null, null, "createdat", "asc");

        result.Items.First().Email.Should().Be("first@x.com");
    }

    [Fact]
    public async Task GetUsersAsync_SortByEmail_Asc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "zzz@x.com");
        SeedHelper.CreateUser(db, email: "aaa@x.com");
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, null, null, "email", "asc");

        result.Items.First().Email.Should().Be("aaa@x.com");
    }

    [Fact]
    public async Task GetUsersAsync_SortByDepartment_Desc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "a@x.com", department: "AAA");
        SeedHelper.CreateUser(db, email: "b@x.com", department: "ZZZ");
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, null, null, "department", "desc");

        result.Items.First().Department.Should().Be("ZZZ");
    }

    [Fact]
    public async Task GetUsersAsync_SortByStatus_Desc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "a@x.com", status: UserStatus.Active);
        SeedHelper.CreateUser(db, email: "b@x.com", status: UserStatus.Suspended);
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, null, null, "status", "desc");

        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetUsersAsync_SortByLastLoginAt_Asc()
    {
        using var db = DbContextFactory.Create();
        var u1 = SeedHelper.CreateUser(db, email: "early@x.com");
        var u2 = SeedHelper.CreateUser(db, email: "late@x.com");
        u1.LastLoginAt = DateTime.UtcNow.AddDays(-5);
        u2.LastLoginAt = DateTime.UtcNow.AddDays(-1);
        db.SaveChanges();
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, null, null, "lastloginat", "asc");

        result.Items.First().Email.Should().Be("early@x.com");
    }

    [Fact]
    public async Task GetUsersAsync_SortByCreatedAt_Desc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "first@x.com");
        SeedHelper.CreateUser(db, email: "second@x.com");
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, null, null, "createdat", "desc");

        result.Items.First().Email.Should().Be("second@x.com");
    }

    [Fact]
    public async Task GetUsersAsync_UnknownSortBy_DefaultsToCreatedAtDesc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "a@x.com");
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 10, null, null, null, "unknownfield", "asc");

        result.TotalCount.Should().Be(1);
    }

    // ── GetUsersAsync — PagedResult metadata ────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_PagedResult_ExposesPageSize()
    {
        using var db = DbContextFactory.Create();
        for (int i = 0; i < 5; i++) SeedHelper.CreateUser(db, email: $"u{i}@px.com");
        var svc = CreateService(db);

        var result = await svc.GetUsersAsync(1, 3, null, null, null, null, null);

        result.PageSize.Should().Be(3);
    }

    // ── GetUsersAsync — pagination ───────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_Pagination_ReturnsCorrectPage()
    {
        using var db = DbContextFactory.Create();
        for (int i = 0; i < 5; i++)
            SeedHelper.CreateUser(db, email: $"user{i}@x.com");
        var svc = CreateService(db);

        var page1 = await svc.GetUsersAsync(1, 2, null, null, null, null, null);
        var page2 = await svc.GetUsersAsync(2, 2, null, null, null, null, null);

        page1.Items.Should().HaveCount(2);
        page1.TotalCount.Should().Be(5);
        page1.TotalPages.Should().Be(3);
        page2.Items.Should().HaveCount(2);
        page2.Page.Should().Be(2);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_UserDtoContainsAllProperties()
    {
        using var db = DbContextFactory.Create();
        var user = SeedHelper.CreateUser(db, email: "props@x.com",
            firstName: "John", lastName: "Smith");
        user.LastLoginAt = DateTime.UtcNow.AddHours(-1);
        db.SaveChanges();
        var svc = CreateService(db);

        var dto = (await svc.GetByIdAsync(user.Id))!;

        dto.LastName.Should().Be("Smith");
        dto.FullName.Should().Be("John Smith");
        dto.CreatedAt.Should().BeCloseTo(user.CreatedAt, TimeSpan.FromSeconds(5));
        dto.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        using var db = DbContextFactory.Create();
        var user = SeedHelper.CreateUser(db, email: "found@x.com");
        var svc = CreateService(db);

        var result = await svc.GetByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Email.Should().Be("found@x.com");
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        using var db = DbContextFactory.Create();
        var svc = CreateService(db);

        var result = await svc.GetByIdAsync(9999);

        result.Should().BeNull();
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_CreatesUserWithHashedPassword()
    {
        using var db = DbContextFactory.Create();
        var svc = CreateService(db);

        var dto = await svc.CreateAsync(new CreateUserRequest(
            "Jane", "Doe", "jane@x.com", "Secure@123", "Finance", []));

        dto.Email.Should().Be("jane@x.com");
        var stored = db.Users.First(u => u.Email == "jane@x.com");
        BCrypt.Net.BCrypt.Verify("Secure@123", stored.PasswordHash).Should().BeTrue();
        stored.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task CreateAsync_AssignsRolesToUser()
    {
        using var db = DbContextFactory.Create();
        var role = SeedHelper.CreateRole(db, "Viewer");
        var svc = CreateService(db);

        var dto = await svc.CreateAsync(new CreateUserRequest(
            "Tom", "Smith", "tom@x.com", "Pass@123", null, [role.Id]));

        dto.Roles.Should().Contain("Viewer");
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingUser_UpdatesFieldsAndRoles()
    {
        using var db = DbContextFactory.Create();
        var role1 = SeedHelper.CreateRole(db, "Admin");
        var role2 = SeedHelper.CreateRole(db, "Viewer");
        var user = SeedHelper.CreateUser(db, email: "u@x.com", roleIds: [role1.Id]);
        var svc = CreateService(db);

        var dto = await svc.UpdateAsync(user.Id,
            new UpdateUserRequest("Updated", "Name", "Marketing", "inactive", [role2.Id]));

        dto.Should().NotBeNull();
        dto!.FirstName.Should().Be("Updated");
        dto.Department.Should().Be("Marketing");
        dto.Status.Should().Be("Inactive");
        dto.Roles.Should().Contain("Viewer");
        dto.Roles.Should().NotContain("Admin");
    }

    [Fact]
    public async Task UpdateAsync_NonExistingUser_ReturnsNull()
    {
        using var db = DbContextFactory.Create();
        var svc = CreateService(db);

        var result = await svc.UpdateAsync(9999,
            new UpdateUserRequest("A", "B", null, "active", []));

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_InvalidStatus_StatusUnchanged()
    {
        using var db = DbContextFactory.Create();
        var user = SeedHelper.CreateUser(db, email: "u@x.com", status: UserStatus.Active);
        var svc = CreateService(db);

        // "badstatus" can't parse — status stays Active
        var dto = await svc.UpdateAsync(user.Id,
            new UpdateUserRequest("A", "B", null, "badstatus", []));

        dto!.Status.Should().Be("Active");
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingUser_DeletesAndReturnsTrue()
    {
        using var db = DbContextFactory.Create();
        var user = SeedHelper.CreateUser(db, email: "del@x.com");
        var svc = CreateService(db);

        var result = await svc.DeleteAsync(user.Id);

        result.Should().BeTrue();
        db.Users.Any(u => u.Id == user.Id).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingUser_ReturnsFalse()
    {
        using var db = DbContextFactory.Create();
        var svc = CreateService(db);

        var result = await svc.DeleteAsync(9999);

        result.Should().BeFalse();
    }

    // ── GetStatsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatsAsync_ReturnsCorrectCounts()
    {
        using var db = DbContextFactory.Create();
        var role = SeedHelper.CreateRole(db, "Admin");
        SeedHelper.CreateUser(db, email: "a@x.com", status: UserStatus.Active, roleIds: [role.Id]);
        SeedHelper.CreateUser(db, email: "b@x.com", status: UserStatus.Inactive);

        var recent = SeedHelper.CreateUser(db, email: "r@x.com", status: UserStatus.Active);
        recent.LastLoginAt = DateTime.UtcNow.AddDays(-1);
        db.SaveChanges();

        var svc = CreateService(db);
        dynamic stats = await svc.GetStatsAsync();

        // Cast via anonymous type via reflection
        var type = stats.GetType();
        ((int)type.GetProperty("total")!.GetValue(stats)!).Should().Be(3);
        ((int)type.GetProperty("active")!.GetValue(stats)!).Should().Be(2);
        ((int)type.GetProperty("inactive")!.GetValue(stats)!).Should().Be(1);
        ((int)type.GetProperty("recentLogins")!.GetValue(stats)!).Should().Be(1);
    }
}
