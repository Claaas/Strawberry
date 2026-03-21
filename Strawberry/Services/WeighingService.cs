using Microsoft.EntityFrameworkCore;
using Strawberry.Data;
using Strawberry.Models;

namespace Strawberry.Services;

public class WeighingService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly BalanceService _balanceService;
    private readonly AuditService _audit;

    public WeighingService(
        IDbContextFactory<AppDbContext> dbFactory,
        BalanceService balanceService,
        AuditService audit)
    {
        _dbFactory = dbFactory;
        _balanceService = balanceService;
        _audit = audit;
    }

    public async Task<Container?> FindContainerByQrCodeAsync(string qrCode)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Containers
            .Include(c => c.AssignedUser)
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.QrCode == qrCode.ToUpperInvariant().Trim());
    }

    public async Task<WeighingRecord?> RecordWeightAsync(int containerId, decimal weightKg, string weighedByUserId)
    {
        if (weightKg <= 0 || weightKg > 10000) return null;

        await using var db = await _dbFactory.CreateDbContextAsync();

        var container = await db.Containers
            .Include(c => c.Event)
            .Include(c => c.AssignedUser)
            .FirstOrDefaultAsync(c => c.Id == containerId);

        if (container == null || container.AssignedUserId == null) return null;
        if (!container.Event.IsActive) return null;

        var price = container.Event.PricePerKg;
        var amount = Math.Round(weightKg * price, 2);

        var record = new WeighingRecord
        {
            ContainerId = containerId,
            WeightKg = weightKg,
            PricePerKg = price,
            AmountPaid = amount,
            WeighedByUserId = weighedByUserId,
            TimestampUtc = DateTime.UtcNow,
    
            AssignedUserIdAtTime = container.AssignedUserId,
            AssignedUserNameAtTime = container.AssignedUser != null 
                ? $"{container.AssignedUser.FirstName} {container.AssignedUser.LastName}" 
                : null
        };

        db.WeighingRecords.Add(record);
        container.Status = ContainerStatus.InUse;

        await db.SaveChangesAsync();

        await _balanceService.AddBalanceAsync(container.AssignedUserId, amount);

        await _audit.LogAsync(
            "DropOff.Weighed",
            $"Container={container.QrCode}, User={record.AssignedUserNameAtTime}, Weight={weightKg}kg, Amount=€{amount}, WeighedBy={weighedByUserId}",
            weighedByUserId);

        return record;
    }

    public async Task<List<WeighingRecord>> GetWeighingRecordsAsync(int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.WeighingRecords
            .Include(w => w.Container)
                .ThenInclude(c => c.AssignedUser)
            .Include(w => w.WeighedByUser)
            .Where(w => w.Container.EventId == eventId)
            .OrderByDescending(w => w.TimestampUtc)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalWeightAsync(int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.WeighingRecords
            .Where(w => w.Container.EventId == eventId)
            .SumAsync(w => w.WeightKg);
    }

    public async Task<int> GetWeighingCountAsync(int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.WeighingRecords
            .Where(w => w.Container.EventId == eventId)
            .CountAsync();
    }

    public async Task<decimal> GetTotalAmountAsync(int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.WeighingRecords
            .Where(w => w.Container.EventId == eventId)
            .SumAsync(w => w.AmountPaid);
    }
}
