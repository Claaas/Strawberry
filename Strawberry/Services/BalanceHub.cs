using Microsoft.AspNetCore.SignalR;

namespace Strawberry.Services;

public class BalanceHub : Hub
{
    public async Task SendBalanceUpdate(string userId, decimal newBalance)
    {
        await Clients.Group(userId).SendAsync("BalanceUpdated", newBalance);
    }

    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
    }
}
