using Microsoft.Extensions.Configuration;
using Payment.BLL.Interfaces;
using Payment.Domain.Enums;

namespace Payment.BLL.Services;

public class MockPaymentProvider : IPaymentProvider
{
    private readonly PaymentStatus _result;

    public MockPaymentProvider(IConfiguration configuration)
    {
        var configuredResult = configuration["MockPayment:Result"];

        if (!Enum.TryParse<PaymentStatus>(
            configuredResult,
            true,
            out _result))
        {
            throw new ArgumentException("Resultado de pago inválido");
        }
    }

    public PaymentStatus ProcessPayment()
    {
        return _result;
    }
}