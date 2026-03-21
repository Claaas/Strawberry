using Microsoft.AspNetCore.Identity;

namespace Strawberry.Models;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public decimal Balance { get; set; } = 0;
    public ICollection<UserEvent> UserEvents { get; set; } = new List<UserEvent>();
    public ICollection<Container> Containers { get; set; } = new List<Container>();
    public ICollection<PayoutRecord> Payouts { get; set; } = new List<PayoutRecord>();
}
