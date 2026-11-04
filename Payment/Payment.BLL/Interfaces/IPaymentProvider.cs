using Payment.Domain.Enums;

namespace Payment.BLL.Interfaces;

/// Define el contrato del proveedor que informa el resultado de un pago. Gabriel Rossi.
public interface IPaymentProvider
{
    PaymentStatus ProcessPayment();
}
