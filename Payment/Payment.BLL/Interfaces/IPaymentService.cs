using Payment.Domain.Enums;

namespace Payment.BLL.Interfaces;

public interface IPaymentService
{
    Task<PaymentStatus> ProcessPaymentAsync();
}
