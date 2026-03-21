using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Strawberry.Models;

namespace Strawberry.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Event> Events { get; set; }
    public DbSet<UserEvent> UserEvents { get; set; }
    public DbSet<Container> Containers { get; set; }
    public DbSet<WeighingRecord> WeighingRecords { get; set; }
    public DbSet<PayoutRecord> PayoutRecords { get; set; }
    public DbSet<PayoutRequest> PayoutRequests { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserEvent>()
            .HasOne(ue => ue.User)
            .WithMany(u => u.UserEvents)
            .HasForeignKey(ue => ue.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserEvent>()
            .HasOne(ue => ue.Event)
            .WithMany(e => e.UserEvents)
            .HasForeignKey(ue => ue.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Container>()
            .HasOne(c => c.AssignedUser)
            .WithMany(u => u.Containers)
            .HasForeignKey(c => c.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Container>()
            .HasOne(c => c.Event)
            .WithMany(e => e.Containers)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WeighingRecord>()
            .HasOne(w => w.Container)
            .WithMany(c => c.WeighingRecords)
            .HasForeignKey(w => w.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WeighingRecord>()
            .HasOne(w => w.WeighedByUser)
            .WithMany()
            .HasForeignKey(w => w.WeighedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PayoutRecord>()
            .HasOne(p => p.User)
            .WithMany(u => u.Payouts)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PayoutRecord>()
            .HasOne(p => p.PaidByUser)
            .WithMany()
            .HasForeignKey(p => p.PaidByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Container>()
            .HasIndex(c => c.QrCode)
            .IsUnique();

        builder.Entity<PayoutRequest>()
            .HasOne(pr => pr.User)
            .WithMany()
            .HasForeignKey(pr => pr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PayoutRequest>()
            .HasOne(pr => pr.HandledByUser)
            .WithMany()
            .HasForeignKey(pr => pr.HandledByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
