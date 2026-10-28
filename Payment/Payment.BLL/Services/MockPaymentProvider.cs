using Microsoft.Extensions.Configuration;   
using Payment.Domain.Enums;

namespace Payment.BLL.Services;

public class MockPaymentProvider
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