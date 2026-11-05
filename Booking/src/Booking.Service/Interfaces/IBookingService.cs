using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Booking.Infrastructure.Entities;
using Booking.Service.DTOs;

namespace Booking.Service.Interfaces
{
    public interface IBookingService
    {
        Task<BookingEntity?> GetBookingByIdAsync(Guid id);

        Task<List<string>> GetOccupiedSeatsAsync(string eventId);

        Task<BookingEntity> ProcessReservationRequestAsync(
            ReservationRequestedMessage request,
            CancellationToken cancellationToken = default);

        Task<int> ExpireRetentionsAsync(
            CancellationToken cancellationToken = default);
    }
}