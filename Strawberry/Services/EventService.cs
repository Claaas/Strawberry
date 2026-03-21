using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Strawberry.Data;
using Strawberry.Models;

namespace Strawberry.Services;

public class EventService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly UserManager<AppUser> _userManager;
    private readonly QrCodeService _qrCodeService;
    private readonly AuditService _audit;

    public EventService(
        IDbContextFactory<AppDbContext> dbFactory,
        UserManager<AppUser> userManager,
        QrCodeService qrCodeService,
        AuditService audit)
    {
        _dbFactory = dbFactory;
        _userManager = userManager;
        _qrCodeService = qrCodeService;
        _audit = audit;
    }

    public async Task<List<Event>> GetAllEventsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Events
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    public async Task<Event?> GetActiveEventAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Events
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.Date)
            .FirstOrDefaultAsync();
    }

    public async Task<Event> CreateEventAsync(string name, string description, decimal pricePerKg)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var evt = new Event
        {
            Name = name,
            Description = description,
            Date = DateTime.UtcNow,
            IsActive = true,
            PricePerKg = pricePerKg
        };

        db.Events.Add(evt);
        await db.SaveChangesAsync();
        await _audit.LogAsync("Event.Created", $"Event '{name}' (Id={evt.Id}, Price={pricePerKg}/kg)");
        return evt;
    }

    public async Task<Container> CreateContainerAsync(int eventId, bool isCustom = false)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        string code;
        do
        {
            code = _qrCodeService.GenerateRandomCode();
        } while (await db.Containers.AnyAsync(c => c.QrCode == code));

        var container = new Container
        {
            QrCode = code,
            EventId = eventId,
            Status = ContainerStatus.Available,
            CreatedAt = DateTime.UtcNow,
            IsCustomCode = isCustom
        };

        db.Containers.Add(container);
        await db.SaveChangesAsync();
        await _audit.LogAsync("Container.Created", $"Code={code}, EventId={eventId}, Custom={isCustom}");
        return container;
    }

    public async Task<Container?> RegisterCustomContainerAsync(int eventId, string code)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var exists = await db.Containers.AnyAsync(c => c.QrCode == code);
        if (exists) return null;

        var container = new Container
        {
            QrCode = code,
            EventId = eventId,
            Status = ContainerStatus.Available,
            CreatedAt = DateTime.UtcNow,
            IsCustomCode = true
        };

        db.Containers.Add(container);
        await db.SaveChangesAsync();
        await _audit.LogAsync("Container.RegisteredCustom", $"Code={code}, EventId={eventId}");
        return container;
    }

    public async Task SetEventActiveAsync(int eventId, bool active)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var evt = await db.Events.FindAsync(eventId);
        if (evt == null) return;

        evt.IsActive = active;
        await db.SaveChangesAsync();
        await _audit.LogAsync(active ? "Event.Activated" : "Event.Deactivated", $"Event '{evt.Name}' (Id={eventId})");
    }

    public async Task<bool> AssignContainerAsync(int containerId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var container = await db.Containers.FindAsync(containerId);
        if (container == null) return false;

        container.AssignedUserId = userId;
        container.Status = ContainerStatus.InUse;
        await db.SaveChangesAsync();
        await _audit.LogAsync("Container.Assigned", $"ContainerId={containerId}, Code={container.QrCode}, AssignedTo={userId}");
        return true;
    }

    public async Task<bool> AddUserToEventAsync(int eventId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var exists = await db.UserEvents
            .AnyAsync(ue => ue.EventId == eventId && ue.UserId == userId);

        if (exists) return false;

        db.UserEvents.Add(new UserEvent
        {
            EventId = eventId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        await _audit.LogAsync("User.AddedToEvent", $"UserId={userId}, EventId={eventId}");
        return true;
    }

    public async Task<List<AppUser>> GetEventParticipantsAsync(int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.UserEvents
            .Where(ue => ue.EventId == eventId)
            .Include(ue => ue.User)
            .Select(ue => ue.User)
            .ToListAsync();
    }

    public async Task<List<Event>> GetUserEventsAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.UserEvents
            .Where(ue => ue.UserId == userId)
            .Include(ue => ue.Event)
            .Select(ue => ue.Event)
            .ToListAsync();
    }

    public async Task<bool> RemoveUserFromEventAsync(int eventId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var entry = await db.UserEvents
            .FirstOrDefaultAsync(ue => ue.EventId == eventId && ue.UserId == userId);
        if (entry == null) return false;

        db.UserEvents.Remove(entry);
        await db.SaveChangesAsync();
        await _audit.LogAsync("User.RemovedFromEvent", $"UserId={userId}, EventId={eventId}");
        return true;
    }

    public async Task<List<Container>> GetContainersForEventAsync(int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Containers
            .Include(c => c.AssignedUser)
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.QrCode)
            .ToListAsync();
    }

    public async Task<List<Container>> GetContainersForUserAsync(string userId, int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Containers
            .Include(c => c.WeighingRecords)
            .Where(c => c.AssignedUserId == userId && c.EventId == eventId)
            .ToListAsync();
    }

    public async Task<List<AppUser>> GetAllHarvestersAsync()
    {
        return (await _userManager.GetUsersInRoleAsync("Harvester")).ToList();
    }

    public async Task<bool> UpdateEventAsync(int eventId, string name, string description, decimal pricePerKg)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var evt = await db.Events.FindAsync(eventId);
        if (evt == null) return false;

        evt.Name = name;
        evt.Description = description;
        evt.PricePerKg = pricePerKg;

        await db.SaveChangesAsync();
        await _audit.LogAsync("Event.Updated", $"Event '{name}' (Id={eventId}, Price={pricePerKg}/kg)");
        return true;
    }

    public async Task<bool> DeleteEventAsync(int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var evt = await db.Events.FindAsync(eventId);
        if (evt == null) return false;

        db.Events.Remove(evt);
        await db.SaveChangesAsync();
        await _audit.LogAsync("Event.Deleted", $"Event '{evt.Name}' (Id={eventId})");
        return true;
    }

    public async Task<bool> UnassignContainerAsync(int containerId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var container = await db.Containers.FindAsync(containerId);
        if (container == null) return false;

        container.AssignedUserId = null;
        container.Status = ContainerStatus.Available;

        await db.SaveChangesAsync();
        await _audit.LogAsync("Container.Unassigned", $"ContainerId={containerId}, Code={container.QrCode}");
        return true;
    }

    public async Task<bool> DeleteContainerAsync(int containerId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var container = await db.Containers.FindAsync(containerId);
        if (container == null) return false;

        db.Containers.Remove(container);
        await db.SaveChangesAsync();
        await _audit.LogAsync("Container.Deleted", $"Code={container.QrCode}, EventId={container.EventId}");
        return true;
    }
}
