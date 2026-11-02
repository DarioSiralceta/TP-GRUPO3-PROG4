namespace Booking.Service.DTOs;

// Hecho por Franco Brienza usando Copilot
public record SeatStatusDto
{
    public string EventId { get; init; } = string.Empty;
    public string SeatId { get; init; } = string.Empty;
    public bool IsOccupied { get; init; }
    public string Status { get; init; } = string.Empty;
}