namespace Availability.Application.Reservations.Tracking;

public class GetReservationStatusResult
{
    public Guid RequestId { get; set; }

    public int EventId { get; set; }

    public int SeatId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? RejectionReason { get; set; }
}