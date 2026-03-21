namespace Strawberry.Models;

public class UserEvent
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}