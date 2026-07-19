using EnterpriseDashboard.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace EnterpriseDashboard.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static ExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

    private static async Task<(int StatusCode, string Body)> InvokeAsync(Exception? ex)
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        RequestDelegate next = ex is null
            ? _ => Task.CompletedTask
            : _ => Task.FromException(ex);

        var mw = CreateMiddleware(next);
        await mw.InvokeAsync(ctx);

        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
        return (ctx.Response.StatusCode, body);
    }

    // ── No exception ─────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        var (code, _) = await InvokeAsync(null);

        // DefaultHttpContext starts at 200
        code.Should().Be(200);
    }

    // ── KeyNotFoundException → 404 ───────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404()
    {
        var (code, body) = await InvokeAsync(new KeyNotFoundException("Not found"));

        code.Should().Be(404);
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("message").GetString().Should().Be("Not found");
    }

    // ── UnauthorizedAccessException → 401 ────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns401()
    {
        var (code, body) = await InvokeAsync(new UnauthorizedAccessException());

        code.Should().Be(401);
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("message").GetString().Should().Be("Unauthorized");
    }

    // ── InvalidOperationException → 400 ──────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_Returns400()
    {
        var (code, body) = await InvokeAsync(new InvalidOperationException("Bad request"));

        code.Should().Be(400);
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("message").GetString().Should().Be("Bad request");
    }

    // ── Generic exception → 500 ───────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_GenericException_Returns500()
    {
        var (code, body) = await InvokeAsync(new Exception("Something went wrong"));

        code.Should().Be(500);
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("message").GetString()
           .Should().Be("An unexpected error occurred");
    }

    // ── Response content type ─────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_ExceptionThrown_SetsJsonContentType()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        var mw = CreateMiddleware(_ => Task.FromException(new Exception("err")));
        await mw.InvokeAsync(ctx);

        ctx.Response.ContentType.Should().Be("application/json");
    }

    // ── camelCase JSON ────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_ExceptionThrown_ResponseIsCamelCase()
    {
        var (_, body) = await InvokeAsync(new Exception("oops"));

        // "success" and "message" keys must be lowercase
        body.Should().Contain("\"success\"");
        body.Should().Contain("\"message\"");
    }
}
