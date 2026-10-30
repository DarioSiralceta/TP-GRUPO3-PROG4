using Payment.Domain.Entities;

namespace Payment.DAL.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(PaymentAttempt attempt);
}
