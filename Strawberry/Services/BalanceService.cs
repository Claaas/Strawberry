using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Strawberry.Data;
using Strawberry.Models;

namespace Strawberry.Services;

public class BalanceService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly UserManager<AppUser> _userManager;
    private readonly IHubContext<BalanceHub> _hubContext;
    private readonly AuditService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BalanceService(
        IDbContextFactory<AppDbContext> dbFactory,
        UserManager<AppUser> userManager,
        IHubContext<BalanceHub> hubContext,
        AuditService audit,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbFactory = dbFactory;
        _userManager = userManager;
        _hubContext = hubContext;
        _audit = audit;
        _httpContextAccessor = httpContextAccessor;
    }

    private bool CallerIsAdmin()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.IsInRole("Admin") == true;
    }

    private async Task UnassignUserContainersAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var containers = await db.Containers
            .Where(c => c.AssignedUserId == userId)
            .ToListAsync();

        foreach (var c in containers)
        {
            c.AssignedUserId = null;
            c.Status = ContainerStatus.Available;
        }

        if (containers.Count > 0)
            await db.SaveChangesAsync();
    }

    public async Task<decimal> GetBalanceAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.Balance ?? 0;
    }

    public async Task AddBalanceAsync(string userId, decimal amount)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        user.Balance += amount;
        await _userManager.UpdateAsync(user);

        await _hubContext.Clients.Group(userId).SendAsync("BalanceUpdated", user.Balance);
    }

    public async Task<bool> ProcessPayoutAsync(string userId, string paidByUserId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.Balance <= 0) return false;

        var amount = user.Balance;
        user.Balance = 0;
        await _userManager.UpdateAsync(user);

        await using var db = await _dbFactory.CreateDbContextAsync();

        db.PayoutRecords.Add(new PayoutRecord
        {
            UserId = userId,
            Amount = amount,
            PaidByUserId = paidByUserId,
            TimestampUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        await UnassignUserContainersAsync(userId);

        await _audit.LogAsync("Payout.Processed", $"UserId={userId}, Amount=€{Math.Round(amount, 2):F2}, PaidBy={paidByUserId}", paidByUserId);

        await _hubContext.Clients.Group(userId).SendAsync("BalanceUpdated", 0m);

        return true;
    }

    public async Task<List<(AppUser User, decimal Balance)>> GetAllBalancesAsync(int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var usersWithBalance = await db.WeighingRecords
            .Where(w => w.Container.EventId == eventId)
            .Select(w => w.Container.AssignedUserId)
            .Distinct()
            .ToListAsync();

        var users = await _userManager.Users
            .Where(u => usersWithBalance.Contains(u.Id))
            .ToListAsync();

        return users.Select(u => (u, u.Balance)).OrderByDescending(x => x.Balance).ToList();
    }

    public async Task<List<PayoutRecord>> GetPayoutHistoryAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.PayoutRecords
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.TimestampUtc)
            .ToListAsync();
    }

    public async Task<PayoutRequest?> CreatePayoutRequestAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.Balance <= 0) return null;

        await using var db = await _dbFactory.CreateDbContextAsync();

        var pending = await db.PayoutRequests
            .AnyAsync(r => r.UserId == userId && r.Status == PayoutRequestStatus.Pending);

        if (pending) return null;

        var request = new PayoutRequest
        {
            UserId = userId,
            Amount = user.Balance,
            Status = PayoutRequestStatus.Pending,
            RequestedAtUtc = DateTime.UtcNow
        };

        db.PayoutRequests.Add(request);
        await db.SaveChangesAsync();

        return request;
    }

    public async Task<List<PayoutRequest>> GetPendingRequestsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.PayoutRequests
            .Include(r => r.User)
            .Where(r => r.Status == PayoutRequestStatus.Pending)
            .OrderBy(r => r.RequestedAtUtc)
            .ToListAsync();
    }

    public async Task<List<PayoutRequest>> GetMyRequestsAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.PayoutRequests
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RequestedAtUtc)
            .ToListAsync();
    }

    public async Task<bool> ApprovePayoutRequestAsync(int requestId, string handledByUserId)
    {
        if (!CallerIsAdmin()) return false;
        await using var db = await _dbFactory.CreateDbContextAsync();

        var request = await db.PayoutRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null || request.Status != PayoutRequestStatus.Pending) return false;

        var ok = await ProcessPayoutAsync(request.UserId, handledByUserId);
        if (!ok) return false;

        request.Status = PayoutRequestStatus.Approved;
        request.HandledAtUtc = DateTime.UtcNow;
        request.HandledByUserId = handledByUserId;

        await db.SaveChangesAsync();
        await _audit.LogAsync("PayoutRequest.Approved", $"RequestId={requestId}, UserId={request.UserId}, ApprovedBy={handledByUserId}", handledByUserId);
        return true;
    }

    public async Task<bool> RejectPayoutRequestAsync(int requestId, string handledByUserId)
    {
        if (!CallerIsAdmin()) return false;
        await using var db = await _dbFactory.CreateDbContextAsync();

        var request = await db.PayoutRequests
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null || request.Status != PayoutRequestStatus.Pending) return false;

        request.Status = PayoutRequestStatus.Rejected;
        request.HandledAtUtc = DateTime.UtcNow;
        request.HandledByUserId = handledByUserId;

        await db.SaveChangesAsync();
        await _audit.LogAsync("PayoutRequest.Rejected", $"RequestId={requestId}, UserId={request.UserId}, RejectedBy={handledByUserId}", handledByUserId);
        return true;
    }
}
