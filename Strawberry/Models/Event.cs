namespace Strawberry.Models;

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public decimal PricePerKg { get; set; } = 1.00m;
    public ICollection<UserEvent> UserEvents { get; set; } = new List<UserEvent>();
    public ICollection<Container> Containers { get; set; } = new List<Container>();
}
