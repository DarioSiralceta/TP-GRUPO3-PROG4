using System;

namespace Booking.Infrastructure.Entities;

public class BookingEntity
{
    public Guid Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string SeatId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Status { get; set; } = "Retenida"; // Retenida, Confirmada, Rechazada, Vencida
    public string? RejectionReason { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
