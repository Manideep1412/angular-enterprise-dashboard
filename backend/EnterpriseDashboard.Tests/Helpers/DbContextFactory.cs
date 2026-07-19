using EnterpriseDashboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDashboard.Tests.Helpers;

/// <summary>Creates a fresh in-memory AppDbContext for each test.</summary>
public static class DbContextFactory
{
    public static AppDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }
}
