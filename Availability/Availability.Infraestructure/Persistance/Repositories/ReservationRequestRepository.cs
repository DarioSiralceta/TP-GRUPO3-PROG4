
using System.Threading.Tasks;
using Availability.Application.Interfaces;
using Availability.Domain.Entities;
using Availability.Infraestructure.Persistence;

namespace Availability.Infraestructure.Persistence.Repositories
{
    public class ReservationRequestRepository : IReservationRequestRepository
    {
        private readonly AvailabilityDbContext _context;

        public ReservationRequestRepository(AvailabilityDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ReservationRequest request)
        {
            await _context.ReservationRequests.AddAsync(request);
            await _context.SaveChangesAsync();
        }
    }
}
