using System;
using Availability.Application.Enums;   
using Availability.Application.Reservations.Messages;
using Availability.Application.Interfaces;
using System.Threading.Tasks;


namespace Availability.Application.Reservations.Request
{
    public class RequestReservationUseCase 

    {
        private IReservationRequestPublisher _publisher;

        public RequestReservationUseCase(IReservationRequestPublisher publisher)
        {
            _publisher = publisher;
        }   
        public async Task <RequestReservationResult> Execute(RequestReservationComand comand)
        {
             Guid requestId = Guid.NewGuid();

            var  message = new ReservationRequested
            {
                RequestId = requestId,
                EventId = comand.EventId,
                SeatId = comand.SeatId
            };

            await _publisher.PublishAsync(message);

            return new RequestReservationResult
            {
                RequestId = requestId,
                Status = RequestStatus.Received
            };

           

        }
    }
}
