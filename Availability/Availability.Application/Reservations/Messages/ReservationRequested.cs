
using System;

namespace Availability.Application.Reservations.Messages
{
    public class ReservationRequested
    {
        public int EventId { get; set; }
        public int SeatId { get; set; }
        public Guid RequestId { get; set; }
        public string UserId { get; set; } = string.Empty;
    }
}
