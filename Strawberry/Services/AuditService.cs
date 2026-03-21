using Microsoft.EntityFrameworkCore;
using Strawberry.Data;
using Strawberry.Models;

namespace Strawberry.Services;

public class AuditService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AuditService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task LogAsync(string action, string? details = null, string? userId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            Details = details,
            UserId = userId,
            TimestampUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetRecentAsync(int count = 200)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AuditLogs
            .OrderByDescending(a => a.TimestampUtc)
            .Take(count)
            .ToListAsync();
    }
}
