using System;
using Availability.Application.Enums;

namespace Availability.Application.Reservations.Request
{
    public class RequestReservationResult
    {
        public Guid RequestId { get; set; }

        public RequestStatus Status { get; set; }


    }
}
