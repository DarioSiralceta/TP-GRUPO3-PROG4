namespace Payment.Domain.Enums;

/// Indica el resultado del procesamiento de un pago. Gabriel Ross.
public enum PaymentStatus
{
    Unspecified = 0,
    Success = 1,
    Declined = 2,
    Unavailable = 3
}
