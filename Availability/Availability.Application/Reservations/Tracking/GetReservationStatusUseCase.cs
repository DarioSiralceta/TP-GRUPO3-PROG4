using Availability.Application.Interfaces;

namespace Availability.Application.Reservations.Tracking;

public class GetReservationStatusUseCase
{
    private readonly IReservationRequestRepository _repository;
    private readonly IBookingReservationClient _bookingClient;

    public GetReservationStatusUseCase(
        IReservationRequestRepository repository,
        IBookingReservationClient bookingClient)
    {
        _repository = repository;
        _bookingClient = bookingClient;
    }

    public async Task<GetReservationStatusResult> ExecuteAsync(
        GetReservationStatusQuery query)
    {
        // 1. Buscar la solicitud en Availability.
        var request = await _repository.GetByIdAsync(query.RequestId);

        if (request == null)
        {
            throw new KeyNotFoundException(
                $"No existe la solicitud {query.RequestId}.");
        }

        // 2. Verificar que pertenezca al usuario autenticado.
        if (!string.Equals(
            request.UserId,
            query.UserId,
            StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException(
                "No tenés permiso para consultar esta solicitud.");
        }

        // 3. Consultar el estado real en Booking.
        var booking = await _bookingClient.GetReservationByIdAsync(
            query.RequestId);

        // 4. Si Booking todavía no procesó el mensaje,
        // la solicitud continúa pendiente.
        return new GetReservationStatusResult
        {
            RequestId = request.RequestId,
            EventId = request.EventId,
            SeatId = request.SeatId,
            Status = booking?.Status ?? "Pendiente",
            RejectionReason = booking?.RejectionReason
        };
    }
}