
using Availability.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Availability.Infraestructure.Persistence
{
    public class AvailabilityDbContext : DbContext
    {
        public AvailabilityDbContext(
            DbContextOptions<AvailabilityDbContext> options)
            : base(options)
        {
        }

        public DbSet<ReservationRequest> ReservationRequests
            => Set<ReservationRequest>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ReservationRequest>(entity =>
            {
                entity.HasKey(r => r.RequestId);

                entity.Property(r => r.UserId)
                    .IsRequired();

                entity.Property(r => r.Status)
                    .IsRequired();

                entity.Property(r => r.CreatedAtUtc)
                    .IsRequired();
            });
        }
    }
}


