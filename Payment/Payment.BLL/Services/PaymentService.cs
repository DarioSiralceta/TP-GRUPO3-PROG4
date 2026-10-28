using Payment.BLL.Interfaces;
using Payment.Domain.Enums;

namespace Payment.BLL.Services;

public class PaymentService : IPaymentService
{
    private readonly MockPaymentProvider _mockPaymentProvider;

    public PaymentService(MockPaymentProvider mockPaymentProvider)
    {
        _mockPaymentProvider = mockPaymentProvider;
    }

    public async Task<PaymentStatus> ProcessPaymentAsync()
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            var result = _mockPaymentProvider.ProcessPayment();

            if (result != PaymentStatus.Unavailable)
            {
                return result;
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        return PaymentStatus.Unavailable;
    }
}