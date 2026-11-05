namespace Availability.Application.Reservations.Tracking;

public class BookingReservationResponse
{
    public Guid Id { get; set; }

    public string EventId { get; set; } = string.Empty;

    public string SeatId { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? RejectionReason { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}