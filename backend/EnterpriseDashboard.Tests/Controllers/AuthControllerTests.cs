using EnterpriseDashboard.Api.Controllers;
using EnterpriseDashboard.Api.DTOs.Auth;
using EnterpriseDashboard.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace EnterpriseDashboard.Tests.Controllers;

public class AuthControllerTests
{
    private static AuthController CreateController(IAuthService authService)
    {
        var ctrl = new AuthController(authService);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return ctrl;
    }

    // ── Login ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var mockAuth = new Mock<IAuthService>();
        var loginResponse = new LoginResponse(
            "jwt-token", "Bearer", 3600,
            new UserInfo(1, "Test User", "test@x.com", "Eng", ["Admin"]));
        mockAuth.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<string?>()))
                .ReturnsAsync(loginResponse);

        var ctrl = CreateController(mockAuth.Object);

        var actionResult = await ctrl.Login(new LoginRequest("test@x.com", "pass"));

        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var body = ok.Value!;
        body.GetType().GetProperty("success")!.GetValue(body).Should().Be(true);
        body.GetType().GetProperty("data")!.GetValue(body).Should().Be(loginResponse);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        var mockAuth = new Mock<IAuthService>();
        mockAuth.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<string?>()))
                .ReturnsAsync((LoginResponse?)null);

        var ctrl = CreateController(mockAuth.Object);

        var actionResult = await ctrl.Login(new LoginRequest("bad@x.com", "wrong"));

        var unauth = actionResult.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauth.StatusCode.Should().Be(401);
        var body = unauth.Value!;
        body.GetType().GetProperty("success")!.GetValue(body).Should().Be(false);
    }

    [Fact]
    public async Task Login_WithRemoteIpAddress_PassesIpToAuthService()
    {
        // Covers the non-null branch of RemoteIpAddress?.ToString()
        var mockAuth = new Mock<IAuthService>();
        mockAuth.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), "192.168.1.1"))
                .ReturnsAsync((LoginResponse?)null);

        var ctrl = new AuthController(mockAuth.Object);
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
        ctrl.ControllerContext = new ControllerContext { HttpContext = ctx };

        await ctrl.Login(new LoginRequest("u@x.com", "p"));

        mockAuth.Verify(s => s.LoginAsync(It.IsAny<LoginRequest>(), "192.168.1.1"), Times.Once);
    }

    // ── Me ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Me_AuthenticatedUser_ReturnsClaims()
    {
        var mockAuth = new Mock<IAuthService>();
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, "user@x.com"),
            new Claim(ClaimTypes.Name, "Test User"),
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var ctrl = new AuthController(mockAuth.Object);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var result = ctrl.Me();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var body = ok.Value!;
        body.GetType().GetProperty("success")!.GetValue(body).Should().Be(true);
        var data = body.GetType().GetProperty("data")!.GetValue(body);
        data.Should().NotBeNull();
    }
}
