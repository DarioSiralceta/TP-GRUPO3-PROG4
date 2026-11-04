using Payment.Domain.Entities;

namespace Payment.BLL.Interfaces;

/// Define las operaciones para procesar pagos asociados a una reserva. Gabriel Rossi.
public interface IPaymentService
{
    Task<PaymentAttempt> ProcessPaymentAsync(string bookingId, string userId, decimal amount, string currency);
}
