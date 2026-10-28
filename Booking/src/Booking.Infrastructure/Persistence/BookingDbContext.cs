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
    }
}
