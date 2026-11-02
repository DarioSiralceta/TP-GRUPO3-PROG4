using Availability.Application.Reservations.Tracking;

namespace Availability.Application.Interfaces;

public interface IBookingReservationClient
{
    Task<BookingReservationResponse?> GetReservationByIdAsync(Guid requestId);
}