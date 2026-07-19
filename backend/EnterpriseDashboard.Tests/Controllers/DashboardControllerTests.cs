using EnterpriseDashboard.Api.Controllers;
using EnterpriseDashboard.Api.Entities;
using EnterpriseDashboard.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDashboard.Tests.Controllers;

public class DashboardControllerTests
{
    private static DashboardController CreateController(Api.Data.AppDbContext db)
    {
        var ctrl = new DashboardController(db);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return ctrl;
    }

    // ── GetStats ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStats_ReturnsOkWithExpectedShape()
    {
        using var db = DbContextFactory.Create();
        var ctrl = CreateController(db);

        var result = await ctrl.GetStats();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var body = ok.Value!;
        ((bool)body.GetType().GetProperty("success")!.GetValue(body)!).Should().BeTrue();
        body.GetType().GetProperty("data")!.GetValue(body).Should().NotBeNull();
    }

    [Fact]
    public async Task GetStats_KpisTotalUsers_ReturnsCorrectCount()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "a@x.com", status: UserStatus.Active);
        SeedHelper.CreateUser(db, email: "b@x.com", status: UserStatus.Inactive);
        var ctrl = CreateController(db);

        var result = await ctrl.GetStats();
        var body = ((OkObjectResult)result).Value!;
        var data = body.GetType().GetProperty("data")!.GetValue(body)!;
        var kpis = data.GetType().GetProperty("kpis")!.GetValue(data)!;

        ((int)kpis.GetType().GetProperty("totalUsers")!.GetValue(kpis)!).Should().Be(2);
        ((int)kpis.GetType().GetProperty("activeUsers")!.GetValue(kpis)!).Should().Be(1);
    }

    [Fact]
    public async Task GetStats_KpisRoleCount_ReturnsCorrectCount()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateRole(db, "Admin");
        SeedHelper.CreateRole(db, "Manager");
        var ctrl = CreateController(db);

        var result = await ctrl.GetStats();
        var body = ((OkObjectResult)result).Value!;
        var data = body.GetType().GetProperty("data")!.GetValue(body)!;
        var kpis = data.GetType().GetProperty("kpis")!.GetValue(data)!;

        ((int)kpis.GetType().GetProperty("totalRoles")!.GetValue(kpis)!).Should().Be(2);
    }

    [Fact]
    public async Task GetStats_AuditLogsToday_CountsTodayOnly()
    {
        using var db = DbContextFactory.Create();
        // Today
        SeedHelper.CreateAuditLog(db, createdAt: DateTime.UtcNow);
        // Yesterday — should NOT count
        SeedHelper.CreateAuditLog(db, createdAt: DateTime.UtcNow.AddDays(-1));
        var ctrl = CreateController(db);

        var result = await ctrl.GetStats();
        var body = ((OkObjectResult)result).Value!;
        var data = body.GetType().GetProperty("data")!.GetValue(body)!;
        var kpis = data.GetType().GetProperty("kpis")!.GetValue(data)!;

        ((int)kpis.GetType().GetProperty("auditLogsToday")!.GetValue(kpis)!).Should().Be(1);
    }

    [Fact]
    public async Task GetStats_ActivityChart_HasSevenDays()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateAuditLog(db, createdAt: DateTime.UtcNow);
        var ctrl = CreateController(db);

        var result = await ctrl.GetStats();
        var body = ((OkObjectResult)result).Value!;
        var data = body.GetType().GetProperty("data")!.GetValue(body)!;
        var chart = data.GetType().GetProperty("activityChart")!.GetValue(data) as IEnumerable<object>;

        chart.Should().HaveCount(7);
    }

    [Fact]
    public async Task GetStats_RoleDistribution_ReflectsUserRoleAssignments()
    {
        using var db = DbContextFactory.Create();
        var role = SeedHelper.CreateRole(db, "Admin");
        SeedHelper.CreateUser(db, email: "a@x.com", roleIds: [role.Id]);
        SeedHelper.CreateUser(db, email: "b@x.com", roleIds: [role.Id]);
        var ctrl = CreateController(db);

        var result = await ctrl.GetStats();
        var body = ((OkObjectResult)result).Value!;
        var data = body.GetType().GetProperty("data")!.GetValue(body)!;
        var dist = data.GetType().GetProperty("roleDistribution")!.GetValue(data) as IEnumerable<object>;

        dist.Should().HaveCount(1);
        var entry = dist!.First();
        ((int)entry.GetType().GetProperty("count")!.GetValue(entry)!).Should().Be(2);
    }

    [Fact]
    public async Task GetStats_RecentActivity_ReturnsMaxFive()
    {
        using var db = DbContextFactory.Create();
        for (int i = 0; i < 8; i++)
            SeedHelper.CreateAuditLog(db, description: $"Event {i}");
        var ctrl = CreateController(db);

        var result = await ctrl.GetStats();
        var body = ((OkObjectResult)result).Value!;
        var data = body.GetType().GetProperty("data")!.GetValue(body)!;
        var activity = data.GetType().GetProperty("recentActivity")!.GetValue(data) as IEnumerable<object>;

        activity.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetStats_RecentActivity_IsOrderedByCreatedAtDesc()
    {
        using var db = DbContextFactory.Create();
        var now = DateTime.UtcNow;
        SeedHelper.CreateAuditLog(db, description: "oldest", createdAt: now.AddHours(-3));
        SeedHelper.CreateAuditLog(db, description: "newest", createdAt: now);
        SeedHelper.CreateAuditLog(db, description: "middle", createdAt: now.AddHours(-1));
        var ctrl = CreateController(db);

        var result = await ctrl.GetStats();
        var body = ((OkObjectResult)result).Value!;
        var data = body.GetType().GetProperty("data")!.GetValue(body)!;
        var activity = (data.GetType().GetProperty("recentActivity")!.GetValue(data) as IEnumerable<object>)!.ToList();

        ((string)activity[0].GetType().GetProperty("Description")!.GetValue(activity[0])!).Should().Be("newest");
    }

    [Fact]
    public async Task GetStats_KpisHasGrowthStrings()
    {
        using var db = DbContextFactory.Create();
        var ctrl = CreateController(db);

        var result = await ctrl.GetStats();
        var body = ((OkObjectResult)result).Value!;
        var data = body.GetType().GetProperty("data")!.GetValue(body)!;
        var kpis = data.GetType().GetProperty("kpis")!.GetValue(data)!;

        ((string)kpis.GetType().GetProperty("userGrowth")!.GetValue(kpis)!).Should().Be("+12%");
        ((string)kpis.GetType().GetProperty("activeSessionsGrowth")!.GetValue(kpis)!).Should().Be("+5%");
    }
}
