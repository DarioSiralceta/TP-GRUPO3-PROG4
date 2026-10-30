
using System;
using System.Threading.Tasks;
using Availability.Application.Enums;
using Availability.Application.Interfaces;
using Availability.Application.Reservations.Messages;
using Availability.Domain.Entities;

namespace Availability.Application.Reservations.Request
{
    public class RequestReservationUseCase
    {
        private readonly IReservationRequestPublisher _publisher;
        private readonly IReservationRequestRepository _repository;

        public RequestReservationUseCase(
            IReservationRequestPublisher publisher,
            IReservationRequestRepository repository)
        {
            _publisher = publisher;
            _repository = repository;
        }

        public async Task<RequestReservationResult> Execute(
            RequestReservationComand comand,string userId)
        {
            if (comand.EventId <= 0 || comand.SeatId <= 0)
            {
                throw new ArgumentException(
                    "EventId y SeatId deben ser mayores que cero.");
            }

            Guid requestId = Guid.NewGuid();

            var request = new ReservationRequest
            {
                RequestId = requestId,
                EventId = comand.EventId,
                SeatId = comand.SeatId,
                UserId = string.Empty,
                Status = RequestStatus.Received.ToString(),
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repository.AddAsync(request);

            var message = new ReservationRequested
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
