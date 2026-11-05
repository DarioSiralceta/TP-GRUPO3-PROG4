using Booking.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence;

public class BookingDbContext : DbContext
{
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();

    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<BookingEntity>().ToTable("bookings", t =>
        {
            t.HasCheckConstraint(
                "CK_bookings_Status",
                $"\"Status\" IN ('{BookingStatus.Retenida}', '{BookingStatus.Confirmada}', '{BookingStatus.Rechazada}', '{BookingStatus.Vencida}')");
        });
        modelBuilder.Entity<BookingEntity>().HasKey(b => b.Id);

        modelBuilder.Entity<BookingEntity>()
            .Property(b => b.Status)
            .HasConversion<string>();

        // Enforce one active booking per event and seat.
        modelBuilder.Entity<BookingEntity>()
            .HasIndex(b => new { b.EventId, b.SeatId })
            .HasFilter($"\"Status\" IN ('{BookingStatus.Retenida}', '{BookingStatus.Confirmada}')")
            .IsUnique()
            .HasDatabaseName("IX_bookings_EventId_SeatId_Active");

        modelBuilder.Entity<BookingEntity>()
            .HasIndex(b => b.ExpiresAt)
            .HasFilter($"\"Status\" = '{BookingStatus.Retenida}'")
            .HasDatabaseName("IX_bookings_ExpiresAt_Retenida");
    }
}
