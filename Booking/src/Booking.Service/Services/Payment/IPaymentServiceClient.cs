using System;
using System.Threading;
using System.Threading.Tasks;

namespace Booking.Service.Services.Payment;

public record PaymentResult(bool IsSuccess, string? ErrorMessage = null, string? TransactionId = null);

public interface IPaymentServiceClient
{
    Task<PaymentResult> ProcessPaymentAsync(
        Guid bookingId,
        string userId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default);
}
