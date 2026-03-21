namespace Strawberry.Models;

public enum PayoutRequestStatus
{
    Pending,
    Approved,
    Rejected
}

public class PayoutRequest
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
    public decimal Amount { get; set; }
    public PayoutRequestStatus Status { get; set; } = PayoutRequestStatus.Pending;
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? HandledAtUtc { get; set; }
    public string? HandledByUserId { get; set; }
    public AppUser? HandledByUser { get; set; }
}
