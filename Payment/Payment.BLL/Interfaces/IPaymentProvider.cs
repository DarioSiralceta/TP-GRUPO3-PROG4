using Payment.Domain.Enums;

namespace Payment.BLL.Interfaces;

public interface IPaymentProvider
{
    PaymentStatus ProcessPayment();
}
