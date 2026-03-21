namespace Strawberry.Models;

public class WeighingRecord
{
    public int Id { get; set; }
    public int ContainerId { get; set; }
    public Container Container { get; set; } = null!;
    public decimal WeightKg { get; set; }
    public decimal PricePerKg { get; set; }
    public decimal AmountPaid { get; set; }
    public string WeighedByUserId { get; set; } = string.Empty;
    public AppUser WeighedByUser { get; set; } = null!;
    public string? AssignedUserIdAtTime { get; set; }
    public string? AssignedUserNameAtTime { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
