using Strawberry.Data;
using Strawberry.Models;

namespace Strawberry.Services;

public class AuditInterceptor
{
    private readonly AppDbContext _db;

    public AuditInterceptor(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(string action, string? details = null, string? userId = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            Details = details,
            UserId = userId,
            TimestampUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
