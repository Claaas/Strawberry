namespace Strawberry.Models;

public class Container
{
    public int Id { get; set; }
    public string QrCode { get; set; } = string.Empty;
    public ContainerStatus Status { get; set; } = ContainerStatus.Available;
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string? AssignedUserId { get; set; }
    public AppUser? AssignedUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsCustomCode { get; set; } = false;
    public ICollection<WeighingRecord> WeighingRecords { get; set; } = new List<WeighingRecord>();
}
