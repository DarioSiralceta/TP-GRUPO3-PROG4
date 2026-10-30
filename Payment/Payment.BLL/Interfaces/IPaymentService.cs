using Payment.Domain.Entities;

namespace Payment.BLL.Interfaces;

public interface IPaymentService
{
    Task<PaymentAttempt> ProcessPaymentAsync(string bookingId, string userId, decimal amount, string currency);
}
