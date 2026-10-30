
using System;

namespace Availability.Domain.Entities
{
    public class ReservationRequest
    {
        public Guid RequestId { get; set; }

        public int EventId { get; set; }

        public int SeatId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string Status { get; set; } = "Received";

        public DateTime CreatedAtUtc { get; set; }
    }
}
