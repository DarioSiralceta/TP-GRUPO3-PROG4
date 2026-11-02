using Payment.Domain.Enums;

namespace Payment.Domain.Entities;

public class PaymentAttempt
{
    public Guid Id { get; set; }
    public Guid PaymentOperationId { get; set; }
    public string BookingId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}
