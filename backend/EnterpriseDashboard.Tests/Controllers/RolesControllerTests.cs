using EnterpriseDashboard.Api.Controllers;
using EnterpriseDashboard.Api.Entities;
using EnterpriseDashboard.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDashboard.Tests.Controllers;

public class RolesControllerTests
{
    private static RolesController CreateController(Api.Data.AppDbContext db)
    {
        var ctrl = new RolesController(db);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return ctrl;
    }

    // ── GetAll ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithRoles()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateRole(db, "Admin");
        SeedHelper.CreateRole(db, "Manager");
        var ctrl = CreateController(db);

        var result = await ctrl.GetAll();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var body = ok.Value!;
        var success = (bool)body.GetType().GetProperty("success")!.GetValue(body)!;
        success.Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_OrdersRolesAlphabetically()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateRole(db, "Zeta");
        SeedHelper.CreateRole(db, "Alpha");
        var ctrl = CreateController(db);

        var result = await ctrl.GetAll();

        var ok = (OkObjectResult)result;
        var data = ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value) as IEnumerable<object>;
        data.Should().NotBeNull();
        var names = data!.Select(d => (string)d.GetType().GetProperty("Name")!.GetValue(d)!).ToList();
        names.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetAll_IncludesPermissionsAndMemberCounts()
    {
        using var db = DbContextFactory.Create();
        var role = SeedHelper.CreateRole(db, "Admin");
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, Resource = "users", Action = "read" });
        SeedHelper.CreateUser(db, email: "u@x.com", roleIds: [role.Id]);
        db.SaveChanges();
        var ctrl = CreateController(db);

        var result = await ctrl.GetAll();

        var ok = (OkObjectResult)result;
        var data = ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value) as IEnumerable<object>;
        var roleDto = data!.First();
        ((int)roleDto.GetType().GetProperty("UserCount")!.GetValue(roleDto)!).Should().Be(1);
        var perms = roleDto.GetType().GetProperty("Permissions")!.GetValue(roleDto) as IEnumerable<object>;
        perms.Should().HaveCount(1);
    }

    // ── GetById ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingRole_Returns200()
    {
        using var db = DbContextFactory.Create();
        var role = SeedHelper.CreateRole(db, "Admin");
        var ctrl = CreateController(db);

        var result = await ctrl.GetById(role.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var success = (bool)ok.Value!.GetType().GetProperty("success")!.GetValue(ok.Value)!;
        success.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_ExistingRole_IncludesUserList()
    {
        using var db = DbContextFactory.Create();
        var role = SeedHelper.CreateRole(db, "Viewer");
        SeedHelper.CreateUser(db, email: "u@x.com", roleIds: [role.Id]);
        var ctrl = CreateController(db);

        var result = await ctrl.GetById(role.Id);

        var ok = (OkObjectResult)result;
        var data = ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value)!;
        ((int)data.GetType().GetProperty("UserCount")!.GetValue(data)!).Should().Be(1);
    }

    [Fact]
    public async Task GetById_RoleDto_AllPropertiesReadable()
    {
        // Covers RoleDto.Id, .Description, .Color, .CreatedAt getters
        using var db = DbContextFactory.Create();
        var role = SeedHelper.CreateRole(db, "Manager", "#00B4D8");
        var ctrl = CreateController(db);

        var result = await ctrl.GetById(role.Id);

        var ok = (OkObjectResult)result;
        var data = ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value)!;
        var type = data.GetType();
        ((int)type.GetProperty("Id")!.GetValue(data)!).Should().Be(role.Id);
        ((string)type.GetProperty("Description")!.GetValue(data)!).Should().Be("Manager role");
        ((string)type.GetProperty("Color")!.GetValue(data)!).Should().Be("#00B4D8");
        ((DateTime)type.GetProperty("CreatedAt")!.GetValue(data)!).Should().BeCloseTo(role.CreatedAt, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetById_RoleWithPermissions_IncludesPermissionDtos()
    {
        // Covers the p => new PermissionDto(p.Resource, p.Action) lambda on line 74
        using var db = DbContextFactory.Create();
        var role = SeedHelper.CreateRole(db, "Admin");
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, Resource = "users", Action = "write" });
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, Resource = "roles", Action = "read" });
        db.SaveChanges();
        var ctrl = CreateController(db);

        var result = await ctrl.GetById(role.Id);

        var ok = (OkObjectResult)result;
        var data = ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value)!;
        var perms = data.GetType().GetProperty("Permissions")!.GetValue(data) as IEnumerable<object>;
        perms.Should().HaveCount(2);
        var first = perms!.First();
        first.GetType().GetProperty("Resource")!.GetValue(first).Should().NotBeNull();
        first.GetType().GetProperty("Action")!.GetValue(first).Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_MissingRole_Returns404()
    {
        using var db = DbContextFactory.Create();
        var ctrl = CreateController(db);

        var result = await ctrl.GetById(9999);

        result.Should().BeOfType<NotFoundObjectResult>()
              .Which.StatusCode.Should().Be(404);
    }
}
