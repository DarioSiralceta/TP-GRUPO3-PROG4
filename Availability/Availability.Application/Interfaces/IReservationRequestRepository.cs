
using System.Threading.Tasks;
using Availability.Domain.Entities;

namespace Availability.Application.Interfaces
{
    public interface IReservationRequestRepository
    {
        Task AddAsync(ReservationRequest request);
    }
}
