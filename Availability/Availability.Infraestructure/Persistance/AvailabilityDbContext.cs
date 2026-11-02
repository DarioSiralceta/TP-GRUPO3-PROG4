using Availability.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Availability.Infraestructure.Persistence;

public class AvailabilityDbContext : DbContext
{
    public AvailabilityDbContext(
        DbContextOptions<AvailabilityDbContext> options)
        : base(options)
    {
    }

    public DbSet<ReservationRequest> ReservationRequests => Set<ReservationRequest>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Seat> Seats => Set<Seat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ReservationRequest>(entity =>
        {
            entity.HasKey(r => r.RequestId);

            entity.Property(r => r.UserId).IsRequired();
            entity.Property(r => r.Status).IsRequired();
            entity.Property(r => r.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).IsRequired();
            entity.HasMany(r => r.Seats)
                .WithOne(s => s.Room)
                .HasForeignKey(s => s.RoomId);

            entity.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.HasIndex(s => new { s.RoomId, s.Number })
                .IsUnique();
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();

            entity.HasOne(e => e.Room)
                .WithMany()
                .HasForeignKey(e => e.RoomId);
        });

        // Datos iniciales del inventario físico
        modelBuilder.Entity<Room>().HasData(
            new Room
            {
                Id = 1,
                Name = "Teatro Central",
                Capacity = 10
            }
        );

        modelBuilder.Entity<Seat>().HasData(
            new Seat { Id = 1, Number = 1, RoomId = 1 },
            new Seat { Id = 2, Number = 2, RoomId = 1 },
            new Seat { Id = 3, Number = 3, RoomId = 1 },
            new Seat { Id = 4, Number = 4, RoomId = 1 },
            new Seat { Id = 5, Number = 5, RoomId = 1 },
            new Seat { Id = 6, Number = 6, RoomId = 1 },
            new Seat { Id = 7, Number = 7, RoomId = 1 },
            new Seat { Id = 8, Number = 8, RoomId = 1 },
            new Seat { Id = 9, Number = 9, RoomId = 1 },
            new Seat { Id = 10, Number = 10, RoomId = 1 }
        );

        modelBuilder.Entity<Event>().HasData(
            new Event
            {
                Id = 1,
                Name = "Concierto de prueba",
                Location = "Teatro Central",
                DateUtc = new DateTime(2026, 12, 10, 20, 0, 0, DateTimeKind.Utc),
                RoomId = 1
            },
            new Event
            {
                Id = 2,
                Name = "Obra de teatro",
                Location = "Teatro Central",
                DateUtc = new DateTime(2026, 12, 15, 21, 0, 0, DateTimeKind.Utc),
                RoomId = 1
            }
        );
    }
}