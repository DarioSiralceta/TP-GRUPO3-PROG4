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
        
        // Optional: Configure table name or constraints if needed
        modelBuilder.Entity<BookingEntity>().ToTable("bookings", t =>
        {
            // Database-level CHECK constraint enforcing the 4 valid statuses
            t.HasCheckConstraint(
                "CK_bookings_Status",
                $"\"Status\" IN ('{BookingStatus.Retenida}', '{BookingStatus.Confirmada}', '{BookingStatus.Rechazada}', '{BookingStatus.Vencida}')");
        });
        modelBuilder.Entity<BookingEntity>().HasKey(b => b.Id);

        // Map enum to string in PostgreSQL column ('text')
        modelBuilder.Entity<BookingEntity>()
            .Property(b => b.Status)
            .HasConversion<string>();

        // Partial Unique Index: Ensures that for a given (EventId, SeatId), there can only be ONE active booking (Retenida or Confirmada).
        // Bookings in status 'Vencida' or 'Rechazada' are excluded, allowing the seat to be re-booked once released.
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
