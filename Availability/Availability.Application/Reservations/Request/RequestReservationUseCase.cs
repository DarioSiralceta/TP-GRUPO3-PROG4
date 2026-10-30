using System;
using System.Linq;
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
        private readonly ISeatRepository _seatRepository;

        public RequestReservationUseCase(
            IReservationRequestPublisher publisher,
            IReservationRequestRepository repository,
            ISeatRepository seatRepository)
        {
            _publisher = publisher;
            _repository = repository;
            _seatRepository = seatRepository;
        }

        public async Task<RequestReservationResult> Execute(
            RequestReservationComand comand,
            string userId)
        {
            if (comand.EventId <= 0 || comand.SeatId <= 0)
            {
                throw new ArgumentException(
                    "EventId y SeatId deben ser mayores que cero.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("El usuario es obligatorio.");
            }

            bool eventExists =
                await _seatRepository.EventExistsAsync(comand.EventId);

            if (!eventExists)
            {
                throw new ArgumentException(
                    $"No existe el evento con ID {comand.EventId}.");
            }

            var seats =
                await _seatRepository.GetByEventIdAsync(comand.EventId);

            bool seatBelongsToEvent =
                seats.Any(s => s.SeatId == comand.SeatId);

            if (!seatBelongsToEvent)
            {
                throw new ArgumentException(
                    $"El asiento {comand.SeatId} no pertenece al evento {comand.EventId}.");
            }

            Guid requestId = Guid.NewGuid();

            var request = new ReservationRequest
            {
                RequestId = requestId,
                EventId = comand.EventId,
                SeatId = comand.SeatId,
                UserId = userId,
                Status = RequestStatus.Received.ToString(),
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repository.AddAsync(request);

            var message = new ReservationRequested
            {
                RequestId = requestId,
                EventId = comand.EventId,
                SeatId = comand.SeatId,
                UserId = userId
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