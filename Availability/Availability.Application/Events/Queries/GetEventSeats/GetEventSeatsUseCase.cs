using Availability.Application.Interfaces;

namespace Availability.Application.Events.Queries.GetEventSeats;

public class GetEventSeatsUseCase
{
    private readonly ISeatRepository _seatRepository;
    private readonly IBookingAvailabilityClient _bookingClient;

    public GetEventSeatsUseCase(
        ISeatRepository seatRepository,
        IBookingAvailabilityClient bookingClient)
    {
        _seatRepository = seatRepository;
        _bookingClient = bookingClient;
    }

    public async Task<List<GetEventSeatsResult>> ExecuteAsync(
        GetEventSeatsQuery query)
    {
        var eventExists = await _seatRepository.EventExistsAsync(query.EventId);

        if (!eventExists)
        {
            throw new KeyNotFoundException(
                $"No existe el evento con ID {query.EventId}.");
        }

        var seats = await _seatRepository.GetByEventIdAsync(query.EventId);

        var occupiedSeatIds = await _bookingClient.GetOccupiedSeatsAsync(
            query.EventId);

        var occupiedSeats = new HashSet<string>(occupiedSeatIds);

        foreach (var seat in seats)
        {
            seat.IsAvailable = !occupiedSeats.Contains(
                seat.SeatId.ToString());
        }

        return seats;
    }
}