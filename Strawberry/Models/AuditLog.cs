namespace Strawberry.Models;

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? UserId { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
