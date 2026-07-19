using EnterpriseDashboard.Api.Entities;
using EnterpriseDashboard.Api.Services;
using EnterpriseDashboard.Tests.Helpers;
using FluentAssertions;

namespace EnterpriseDashboard.Tests.Services;

public class AuditServiceTests
{
    private static AuditService CreateService(Api.Data.AppDbContext db) => new(db);

    // ── LogAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LogAsync_AddsAuditLogToDatabase()
    {
        using var db = DbContextFactory.Create();
        var svc = CreateService(db);

        await svc.LogAsync(1, "actor@example.com", AuditAction.Create,
            "users", "Created user X", "42", "127.0.0.1", AuditSeverity.Warning);

        var log = db.AuditLogs.Single();
        log.UserId.Should().Be(1);
        log.UserEmail.Should().Be("actor@example.com");
        log.Action.Should().Be(AuditAction.Create);
        log.Resource.Should().Be("users");
        log.Description.Should().Be("Created user X");
        log.ResourceId.Should().Be("42");
        log.IpAddress.Should().Be("127.0.0.1");
        log.Severity.Should().Be(AuditSeverity.Warning);
    }

    [Fact]
    public async Task LogAsync_DefaultSeverityIsInfo()
    {
        using var db = DbContextFactory.Create();
        var svc = CreateService(db);

        await svc.LogAsync(null, "a@b.com", AuditAction.Login, "auth", "Login");

        db.AuditLogs.Single().Severity.Should().Be(AuditSeverity.Info);
    }

    // ── GetLogsAsync — no filters ────────────────────────────────────────────

    [Fact]
    public async Task GetLogsAsync_NoFilters_ReturnsAllLogs()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, action: AuditAction.Create);
        SeedHelper.CreateAuditLog(db, action: AuditAction.Delete);
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, null, null, null);

        result.TotalCount.Should().Be(2);
    }

    // ── GetLogsAsync — action filter ─────────────────────────────────────────

    [Fact]
    public async Task GetLogsAsync_ActionFilter_ReturnsMatchingLogs()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, action: AuditAction.Create);
        SeedHelper.CreateAuditLog(db, action: AuditAction.Login);
        SeedHelper.CreateAuditLog(db, action: AuditAction.Delete);
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, ["Create", "Login"], null, null, null, null);

        result.TotalCount.Should().Be(2);
        result.Items.Select(l => l.Action).Should().BeEquivalentTo(["Create", "Login"]);
    }

    [Fact]
    public async Task GetLogsAsync_ActionFilter_InvalidValue_Ignored()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, action: AuditAction.Login);
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, ["NotAnAction"], null, null, null, null);

        // invalid enum → empty parsed list → no WHERE → all returned
        result.TotalCount.Should().Be(1);
    }

    // ── GetLogsAsync — severity filter ───────────────────────────────────────

    [Fact]
    public async Task GetLogsAsync_SeverityFilter_ReturnsMatchingLogs()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, severity: AuditSeverity.Info);
        SeedHelper.CreateAuditLog(db, severity: AuditSeverity.Warning);
        SeedHelper.CreateAuditLog(db, severity: AuditSeverity.Critical);
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, ["Warning"], null, null, null);

        result.TotalCount.Should().Be(1);
        result.Items.First().Severity.Should().Be("Warning");
    }

    [Fact]
    public async Task GetLogsAsync_SeverityFilter_InvalidValue_Ignored()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, severity: AuditSeverity.Info);
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, ["UnknownSeverity"], null, null, null);

        result.TotalCount.Should().Be(1);
    }

    // ── GetLogsAsync — search filter ─────────────────────────────────────────

    [Fact]
    public async Task GetLogsAsync_SearchByUserEmail_ReturnsMatch()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, userEmail: "admin@enterprise.dev");
        SeedHelper.CreateAuditLog(db, userEmail: "viewer@enterprise.dev");
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, "admin", null, null);

        result.TotalCount.Should().Be(1);
        result.Items.First().UserEmail.Should().Be("admin@enterprise.dev");
    }

    [Fact]
    public async Task GetLogsAsync_SearchByDescription_ReturnsMatch()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, description: "Created user Alice");
        SeedHelper.CreateAuditLog(db, description: "Deleted user Bob");
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, "alice", null, null);

        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLogsAsync_SearchByResource_ReturnsMatch()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, resource: "roles");
        SeedHelper.CreateAuditLog(db, resource: "users");
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, "roles", null, null);

        result.TotalCount.Should().Be(1);
    }

    // ── GetLogsAsync — sort variants ─────────────────────────────────────────

    [Fact]
    public async Task GetLogsAsync_DefaultSort_IsDescByCreatedAt()
    {
        using var db = DbContextFactory.Create();
        var now = DateTime.UtcNow;
        SeedHelper.CreateAuditLog(db, userEmail: "old@x.com", createdAt: now.AddHours(-2));
        SeedHelper.CreateAuditLog(db, userEmail: "new@x.com", createdAt: now);
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, null, null, null);

        result.Items.First().UserEmail.Should().Be("new@x.com");
    }

    [Fact]
    public async Task GetLogsAsync_SortByCreatedAt_Asc()
    {
        using var db = DbContextFactory.Create();
        var now = DateTime.UtcNow;
        SeedHelper.CreateAuditLog(db, userEmail: "old@x.com", createdAt: now.AddHours(-2));
        SeedHelper.CreateAuditLog(db, userEmail: "new@x.com", createdAt: now);
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, null, "createdat", "asc");

        result.Items.First().UserEmail.Should().Be("old@x.com");
    }

    [Fact]
    public async Task GetLogsAsync_SortByCreatedAt_UsingTimeAlias_Asc()
    {
        using var db = DbContextFactory.Create();
        var now = DateTime.UtcNow;
        SeedHelper.CreateAuditLog(db, userEmail: "a@x.com", createdAt: now.AddHours(-1));
        SeedHelper.CreateAuditLog(db, userEmail: "b@x.com", createdAt: now);
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, null, "time", "asc");

        result.Items.First().UserEmail.Should().Be("a@x.com");
    }

    [Fact]
    public async Task GetLogsAsync_SortByUserEmail_Asc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, userEmail: "zzz@x.com");
        SeedHelper.CreateAuditLog(db, userEmail: "aaa@x.com");
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, null, "useremail", "asc");

        result.Items.First().UserEmail.Should().Be("aaa@x.com");
    }

    [Fact]
    public async Task GetLogsAsync_SortByUserEmail_UsingUserAlias_Desc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, userEmail: "aaa@x.com");
        SeedHelper.CreateAuditLog(db, userEmail: "zzz@x.com");
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, null, "user", null); // null → defaults to desc

        result.Items.First().UserEmail.Should().Be("zzz@x.com");
    }

    [Fact]
    public async Task GetLogsAsync_SortByAction()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, action: AuditAction.Login);
        SeedHelper.CreateAuditLog(db, action: AuditAction.Create);
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, null, "action", "asc");

        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetLogsAsync_SortByResource()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, resource: "users");
        SeedHelper.CreateAuditLog(db, resource: "roles");
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, null, "resource", "asc");

        result.Items.First().Resource.Should().Be("roles");
    }

    [Fact]
    public async Task GetLogsAsync_SortBySeverity_Asc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, severity: AuditSeverity.Warning);
        SeedHelper.CreateAuditLog(db, severity: AuditSeverity.Critical);
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, null, "severity", "asc");

        result.TotalCount.Should().Be(2);
    }

    // ── GetLogsAsync — sort desc direction (second branch of each ternary) ──

    [Fact]
    public async Task GetLogsAsync_SortByAction_Desc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, action: AuditAction.Create);
        SeedHelper.CreateAuditLog(db, action: AuditAction.Login);
        var svc = CreateService(db);

        // null sortDir → desc=true → OrderByDescending(l => l.Action)
        // AuditAction enum: Login=0, Create=2 — desc by int value puts Create first
        var result = await svc.GetLogsAsync(1, 10, null, null, null, "action", null);

        result.TotalCount.Should().Be(2);
        result.Items.First().Action.Should().Be("Create"); // Create(2) > Login(0)
    }

    [Fact]
    public async Task GetLogsAsync_SortByResource_Desc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, resource: "roles");
        SeedHelper.CreateAuditLog(db, resource: "users");
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, null, "resource", "desc");

        result.Items.First().Resource.Should().Be("users"); // u > r
    }

    [Fact]
    public async Task GetLogsAsync_SortBySeverity_Desc()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, severity: AuditSeverity.Info);
        SeedHelper.CreateAuditLog(db, severity: AuditSeverity.Warning);
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, null, "severity", "desc");

        result.TotalCount.Should().Be(2);
        // "Warning" > "Info" — Warning comes first descending
        result.Items.First().Severity.Should().Be("Warning");
    }

    [Fact]
    public async Task GetLogsAsync_SortByCreatedAt_ExplicitDesc()
    {
        using var db = DbContextFactory.Create();
        var now = DateTime.UtcNow;
        SeedHelper.CreateAuditLog(db, userEmail: "old@x.com", createdAt: now.AddHours(-2));
        SeedHelper.CreateAuditLog(db, userEmail: "new@x.com", createdAt: now);
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, null, "createdat", "desc");

        result.Items.First().UserEmail.Should().Be("new@x.com");
    }

    // ── GetLogsAsync — pagination ────────────────────────────────────────────

    [Fact]
    public async Task GetLogsAsync_Pagination_ReturnsCorrectPage()
    {
        using var db = DbContextFactory.Create();
        for (int i = 0; i < 6; i++)
            SeedHelper.CreateAuditLog(db, userEmail: $"u{i}@x.com");
        var svc = CreateService(db);

        var page1 = await svc.GetLogsAsync(1, 2, null, null, null, null, null);
        var page2 = await svc.GetLogsAsync(2, 2, null, null, null, null, null);

        page1.Items.Should().HaveCount(2);
        page1.TotalCount.Should().Be(6);
        page1.TotalPages.Should().Be(3);
        page2.Page.Should().Be(2);
    }

    // ── DTO mapping ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLogsAsync_MapsDtoFieldsCorrectly()
    {
        using var db = DbContextFactory.Create();
        var log = SeedHelper.CreateAuditLog(db,
            userEmail: "map@x.com",
            action: AuditAction.Update,
            resource: "roles",
            description: "Updated role",
            severity: AuditSeverity.Critical);
        db.AuditLogs.First().ResourceId = "7";
        db.AuditLogs.First().IpAddress = "192.168.1.1";
        db.SaveChanges();
        var svc = CreateService(db);

        var result = await svc.GetLogsAsync(1, 10, null, null, null, null, null);
        var dto = result.Items.First();

        dto.UserEmail.Should().Be("map@x.com");
        dto.Action.Should().Be("Update");
        dto.Resource.Should().Be("roles");
        dto.Description.Should().Be("Updated role");
        dto.Severity.Should().Be("Critical");
        dto.ResourceId.Should().Be("7");
        dto.IpAddress.Should().Be("192.168.1.1");
        dto.Id.Should().BePositive();
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
