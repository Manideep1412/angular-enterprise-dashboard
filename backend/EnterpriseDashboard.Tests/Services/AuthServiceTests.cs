using EnterpriseDashboard.Api.DTOs.Auth;
using EnterpriseDashboard.Api.Entities;
using EnterpriseDashboard.Api.Services;
using EnterpriseDashboard.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.IdentityModel.Tokens.Jwt;

namespace EnterpriseDashboard.Tests.Services;

public class AuthServiceTests
{
    private AuthService CreateService(Api.Data.AppDbContext db) =>
        new(db, JwtConfigHelper.Build(), NullLogger<AuthService>.Instance);

    // ── LoginAsync — user not found ──────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_EmailNotFound_ReturnsNull()
    {
        using var db = DbContextFactory.Create();
        var svc = CreateService(db);

        var result = await svc.LoginAsync(new LoginRequest("nobody@example.com", "pass"), null);

        result.Should().BeNull();
    }

    // ── LoginAsync — wrong password ──────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "user@example.com", password: "CorrectPass!");
        var svc = CreateService(db);

        var result = await svc.LoginAsync(new LoginRequest("user@example.com", "WrongPass!"), null);

        result.Should().BeNull();
    }

    // ── LoginAsync — inactive user ───────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsNull()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "inactive@example.com", password: "Pass!", status: UserStatus.Inactive);
        var svc = CreateService(db);

        var result = await svc.LoginAsync(new LoginRequest("inactive@example.com", "Pass!"), null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_SuspendedUser_ReturnsNull()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "suspended@example.com", password: "Pass!", status: UserStatus.Suspended);
        var svc = CreateService(db);

        var result = await svc.LoginAsync(new LoginRequest("suspended@example.com", "Pass!"), null);

        result.Should().BeNull();
    }

    // ── LoginAsync — successful login ────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
    {
        using var db = DbContextFactory.Create();
        var role = SeedHelper.CreateRole(db, "Admin");
        SeedHelper.CreateUser(db, email: "admin@example.com", password: "Admin@123", roleIds: [role.Id]);
        var svc = CreateService(db);

        var result = await svc.LoginAsync(new LoginRequest("admin@example.com", "Admin@123"), "127.0.0.1");

        result.Should().NotBeNull();
        result!.TokenType.Should().Be("Bearer");
        result.ExpiresIn.Should().Be(3600);
        result.User.Email.Should().Be("admin@example.com");
        result.User.Roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_EmailIsCaseInsensitive()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "User@Example.COM", password: "Pass123!");
        var svc = CreateService(db);

        var result = await svc.LoginAsync(new LoginRequest("user@example.com", "Pass123!"), null);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_Success_UpdatesLastLoginAt()
    {
        using var db = DbContextFactory.Create();
        var user = SeedHelper.CreateUser(db, email: "u@example.com", password: "Pass!");
        user.LastLoginAt.Should().BeNull();
        var svc = CreateService(db);

        await svc.LoginAsync(new LoginRequest("u@example.com", "Pass!"), null);

        db.Users.First(u => u.Email == "u@example.com").LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_Success_AddsAuditLog()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "u@example.com", password: "Pass!");
        var svc = CreateService(db);

        await svc.LoginAsync(new LoginRequest("u@example.com", "Pass!"), "10.0.0.1");

        db.AuditLogs.Should().ContainSingle(l =>
            l.Action == AuditAction.Login &&
            l.UserEmail == "u@example.com" &&
            l.IpAddress == "10.0.0.1");
    }

    [Fact]
    public async Task LoginAsync_Success_AuditLogIpIsNull_WhenNotProvided()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "u@example.com", password: "Pass!");
        var svc = CreateService(db);

        await svc.LoginAsync(new LoginRequest("u@example.com", "Pass!"), null);

        db.AuditLogs.First().IpAddress.Should().BeNull();
    }

    // ── JWT token contents ───────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_Success_TokenIsValidJwt()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "u@example.com", password: "Pass!");
        var svc = CreateService(db);

        var result = await svc.LoginAsync(new LoginRequest("u@example.com", "Pass!"), null);

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(result!.AccessToken).Should().BeTrue();
        var token = handler.ReadJwtToken(result.AccessToken);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "u@example.com");
    }

    [Fact]
    public async Task LoginAsync_Success_TokenContainsRoleClaims()
    {
        using var db = DbContextFactory.Create();
        var role = SeedHelper.CreateRole(db, "Manager");
        SeedHelper.CreateUser(db, email: "mgr@example.com", password: "Pass!", roleIds: [role.Id]);
        var svc = CreateService(db);

        var result = await svc.LoginAsync(new LoginRequest("mgr@example.com", "Pass!"), null);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result!.AccessToken);
        token.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Manager");
    }

    [Fact]
    public async Task LoginAsync_Success_UserInfoContainsFullName()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "fn@example.com", password: "Pass!", firstName: "Jane", lastName: "Doe");
        var svc = CreateService(db);

        var result = await svc.LoginAsync(new LoginRequest("fn@example.com", "Pass!"), null);

        result!.User.FullName.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task LoginAsync_Success_UserInfoDepartmentEmptyWhenNull()
    {
        using var db = DbContextFactory.Create();
        SeedHelper.CreateUser(db, email: "nodept@example.com", password: "Pass!", department: null);
        var svc = CreateService(db);

        var result = await svc.LoginAsync(new LoginRequest("nodept@example.com", "Pass!"), null);

        result!.User.Department.Should().Be(string.Empty);
    }
}
