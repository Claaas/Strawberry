namespace Strawberry.Models;

public class PayoutRecord
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string PaidByUserId { get; set; } = string.Empty;
    public AppUser PaidByUser { get; set; } = null!;
}
