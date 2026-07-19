using EnterpriseDashboard.Api.Controllers;
using EnterpriseDashboard.Api.DTOs.Common;
using EnterpriseDashboard.Api.DTOs.Users;
using EnterpriseDashboard.Api.Entities;
using EnterpriseDashboard.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace EnterpriseDashboard.Tests.Controllers;

public class UsersControllerTests
{
    private static readonly UserDto SampleUser = new(
        1, "Jane", "Doe", "Jane Doe", "jane@x.com",
        "Active", "Engineering", ["Admin"],
        DateTime.UtcNow, null);

    private static UsersController CreateController(
        IUserService? userSvc = null,
        IAuditService? auditSvc = null,
        int actorId = 1,
        string actorEmail = "actor@x.com")
    {
        userSvc ??= new Mock<IUserService>().Object;
        auditSvc ??= new Mock<IAuditService>().Object;

        var ctrl = new UsersController(userSvc, auditSvc);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, actorId.ToString()),
            new Claim(ClaimTypes.Email, actorEmail),
        };
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
        return ctrl;
    }

    // ── GetAll ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedResult()
    {
        var mockSvc = new Mock<IUserService>();
        var paged = new PagedResult<UserDto>([SampleUser], 1, 1, 10, 1);
        mockSvc.Setup(s => s.GetUsersAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
            It.IsAny<List<string>?>(), It.IsAny<List<string>?>(),
            It.IsAny<string?>(), It.IsAny<string?>()))
               .ReturnsAsync(paged);

        var ctrl = CreateController(userSvc: mockSvc.Object);

        var result = await ctrl.GetAll();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    // ── GetById ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingUser_Returns200()
    {
        var mockSvc = new Mock<IUserService>();
        mockSvc.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(SampleUser);
        var ctrl = CreateController(userSvc: mockSvc.Object);

        var result = await ctrl.GetById(1);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_MissingUser_Returns404()
    {
        var mockSvc = new Mock<IUserService>();
        mockSvc.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((UserDto?)null);
        var ctrl = CreateController(userSvc: mockSvc.Object);

        var result = await ctrl.GetById(999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201AndLogsAudit()
    {
        var mockSvc = new Mock<IUserService>();
        var mockAudit = new Mock<IAuditService>();
        mockSvc.Setup(s => s.CreateAsync(It.IsAny<CreateUserRequest>()))
               .ReturnsAsync(SampleUser);

        var ctrl = CreateController(userSvc: mockSvc.Object, auditSvc: mockAudit.Object);

        var result = await ctrl.Create(new CreateUserRequest(
            "Jane", "Doe", "jane@x.com", "Pass@123", "Eng", [1]));

        result.Should().BeOfType<CreatedAtActionResult>()
              .Which.StatusCode.Should().Be(201);

        mockAudit.Verify(a => a.LogAsync(
            It.IsAny<int?>(), It.IsAny<string>(), AuditAction.Create,
            "users", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            AuditSeverity.Info), Times.Once);
    }

    [Fact]
    public async Task Create_ActorIdNotParseable_UsesNullActorId()
    {
        // actor ID claim is missing — should still succeed
        var mockSvc = new Mock<IUserService>();
        var mockAudit = new Mock<IAuditService>();
        mockSvc.Setup(s => s.CreateAsync(It.IsAny<CreateUserRequest>()))
               .ReturnsAsync(SampleUser);

        var ctrl = new UsersController(mockSvc.Object, mockAudit.Object);
        // No NameIdentifier claim
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Email, "actor@x.com")], "TestAuth"))
            }
        };

        var result = await ctrl.Create(new CreateUserRequest("A", "B", "a@x.com", "P", null, []));

        result.Should().BeOfType<CreatedAtActionResult>();
        mockAudit.Verify(a => a.LogAsync(
            null, "actor@x.com", AuditAction.Create, "users",
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            AuditSeverity.Info), Times.Once);
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingUser_Returns200AndLogsAudit()
    {
        var mockSvc = new Mock<IUserService>();
        var mockAudit = new Mock<IAuditService>();
        mockSvc.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateUserRequest>()))
               .ReturnsAsync(SampleUser);

        var ctrl = CreateController(userSvc: mockSvc.Object, auditSvc: mockAudit.Object);

        var result = await ctrl.Update(1, new UpdateUserRequest("Jane", "Doe", "Eng", "active", []));

        result.Should().BeOfType<OkObjectResult>();
        mockAudit.Verify(a => a.LogAsync(
            It.IsAny<int?>(), It.IsAny<string>(), AuditAction.Update,
            "users", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            AuditSeverity.Info), Times.Once);
    }

    [Fact]
    public async Task Update_MissingUser_Returns404()
    {
        var mockSvc = new Mock<IUserService>();
        mockSvc.Setup(s => s.UpdateAsync(999, It.IsAny<UpdateUserRequest>()))
               .ReturnsAsync((UserDto?)null);
        var ctrl = CreateController(userSvc: mockSvc.Object);

        var result = await ctrl.Update(999, new UpdateUserRequest("A", "B", null, "active", []));

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingUser_Returns200AndLogsWarningAudit()
    {
        var mockSvc = new Mock<IUserService>();
        var mockAudit = new Mock<IAuditService>();
        mockSvc.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        var ctrl = CreateController(userSvc: mockSvc.Object, auditSvc: mockAudit.Object);

        var result = await ctrl.Delete(1);

        result.Should().BeOfType<OkObjectResult>();
        mockAudit.Verify(a => a.LogAsync(
            It.IsAny<int?>(), It.IsAny<string>(), AuditAction.Delete,
            "users", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            AuditSeverity.Warning), Times.Once);
    }

    [Fact]
    public async Task Delete_MissingUser_Returns404()
    {
        var mockSvc = new Mock<IUserService>();
        mockSvc.Setup(s => s.DeleteAsync(999)).ReturnsAsync(false);
        var ctrl = CreateController(userSvc: mockSvc.Object);

        var result = await ctrl.Delete(999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ActorIdNotParseable_UsesNullActorId()
    {
        var mockSvc = new Mock<IUserService>();
        var mockAudit = new Mock<IAuditService>();
        mockSvc.Setup(s => s.DeleteAsync(5)).ReturnsAsync(true);

        var ctrl = new UsersController(mockSvc.Object, mockAudit.Object);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Email, "actor@x.com")], "TestAuth"))
            }
        };

        var result = await ctrl.Delete(5);

        result.Should().BeOfType<OkObjectResult>();
        mockAudit.Verify(a => a.LogAsync(
            null, "actor@x.com", AuditAction.Delete, "users",
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            AuditSeverity.Warning), Times.Once);
    }

    // ── Actor email "system" fallback (no Email claim) ──────────────────────

    [Fact]
    public async Task Create_NoEmailClaim_UsesSystemFallback()
    {
        var mockSvc = new Mock<IUserService>();
        var mockAudit = new Mock<IAuditService>();
        mockSvc.Setup(s => s.CreateAsync(It.IsAny<CreateUserRequest>()))
               .ReturnsAsync(SampleUser);

        var ctrl = new UsersController(mockSvc.Object, mockAudit.Object);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "1")], "TestAuth"))
            }
        };

        await ctrl.Create(new CreateUserRequest("A", "B", "a@x.com", "P", null, []));

        mockAudit.Verify(a => a.LogAsync(
            1, "system", AuditAction.Create, "users",
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            AuditSeverity.Info), Times.Once);
    }

    [Fact]
    public async Task Update_NoEmailClaim_UsesSystemFallback()
    {
        var mockSvc = new Mock<IUserService>();
        var mockAudit = new Mock<IAuditService>();
        mockSvc.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateUserRequest>()))
               .ReturnsAsync(SampleUser);

        var ctrl = new UsersController(mockSvc.Object, mockAudit.Object);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "1")], "TestAuth"))
            }
        };

        await ctrl.Update(1, new UpdateUserRequest("A", "B", null, "active", []));

        mockAudit.Verify(a => a.LogAsync(
            1, "system", AuditAction.Update, "users",
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            AuditSeverity.Info), Times.Once);
    }

    [Fact]
    public async Task Delete_NoEmailClaim_UsesSystemFallback()
    {
        var mockSvc = new Mock<IUserService>();
        var mockAudit = new Mock<IAuditService>();
        mockSvc.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        var ctrl = new UsersController(mockSvc.Object, mockAudit.Object);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "1")], "TestAuth"))
            }
        };

        await ctrl.Delete(1);

        mockAudit.Verify(a => a.LogAsync(
            1, "system", AuditAction.Delete, "users",
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            AuditSeverity.Warning), Times.Once);
    }

    [Fact]
    public async Task Update_ActorIdNotParseable_UsesNullActorId()
    {
        var mockSvc = new Mock<IUserService>();
        var mockAudit = new Mock<IAuditService>();
        mockSvc.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateUserRequest>()))
               .ReturnsAsync(SampleUser);

        var ctrl = new UsersController(mockSvc.Object, mockAudit.Object);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Email, "actor@x.com")], "TestAuth"))
            }
        };

        var result = await ctrl.Update(1, new UpdateUserRequest("A", "B", null, "active", []));

        result.Should().BeOfType<OkObjectResult>();
        mockAudit.Verify(a => a.LogAsync(
            null, "actor@x.com", AuditAction.Update, "users",
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            AuditSeverity.Info), Times.Once);
    }
}
