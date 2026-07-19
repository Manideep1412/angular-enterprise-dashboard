using EnterpriseDashboard.Api.Controllers;
using EnterpriseDashboard.Api.DTOs.AuditLogs;
using EnterpriseDashboard.Api.DTOs.Common;
using EnterpriseDashboard.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EnterpriseDashboard.Tests.Controllers;

public class AuditLogsControllerTests
{
    private static AuditLogsController CreateController(IAuditService auditSvc)
    {
        var ctrl = new AuditLogsController(auditSvc);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return ctrl;
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedLogs()
    {
        var mockSvc = new Mock<IAuditService>();
        var pagedResult = new PagedResult<AuditLogDto>(
            [new AuditLogDto(1, "u@x.com", "Create", "users", null, "desc", null, "Info", DateTime.UtcNow)],
            1, 1, 10, 1);
        mockSvc.Setup(s => s.GetLogsAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>?>(),
            It.IsAny<List<string>?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>()))
               .ReturnsAsync(pagedResult);

        var ctrl = CreateController(mockSvc.Object);

        var result = await ctrl.GetAll(1, 10, null, null, null, null, "desc");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var body = ok.Value!;
        ((bool)body.GetType().GetProperty("success")!.GetValue(body)!).Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_PassesAllParametersToService()
    {
        var mockSvc = new Mock<IAuditService>();
        mockSvc.Setup(s => s.GetLogsAsync(
            2, 5, It.Is<List<string>?>(l => l != null && l.Contains("Create")),
            It.Is<List<string>?>(l => l != null && l.Contains("Warning")),
            "test", "createdAt", "desc"))
               .ReturnsAsync(new PagedResult<AuditLogDto>([], 0, 2, 5, 0));

        var ctrl = CreateController(mockSvc.Object);

        await ctrl.GetAll(2, 5, ["Create"], ["Warning"], "test", "createdAt", "desc");

        mockSvc.Verify(s => s.GetLogsAsync(2, 5,
            It.IsAny<List<string>?>(), It.IsAny<List<string>?>(),
            "test", "createdAt", "desc"), Times.Once);
    }
}
