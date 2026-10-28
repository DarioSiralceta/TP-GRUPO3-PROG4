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
        modelBuilder.Entity<BookingEntity>().ToTable("bookings");
        modelBuilder.Entity<BookingEntity>().HasKey(b => b.Id);

        // Partial Unique Index: Ensures that for a given (EventId, SeatId), there can only be ONE active booking (Retenida or Confirmada).
        // Bookings in status 'Vencida' or 'Rechazada' are excluded, allowing the seat to be re-booked once released.
        modelBuilder.Entity<BookingEntity>()
            .HasIndex(b => new { b.EventId, b.SeatId })
            .HasFilter("\"Status\" IN ('Retenida', 'Confirmada')")
            .IsUnique()
            .HasDatabaseName("IX_bookings_EventId_SeatId_Active");
    }
}
