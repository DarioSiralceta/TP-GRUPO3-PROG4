using Payment.BLL.Interfaces;
using Payment.DAL.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.BLL.Services;

// Clase que implementa la logica de negocio (BLL). Se encarga de decidir cuando
// reintentar, armar cada intento, cuando persistir. Orquesta entre
// el proveedor de pagos (IPaymentProvider) y la persistencia (IPaymentRepository) - (Federico Diaz)
public class PaymentService : IPaymentService
{
    private readonly IPaymentProvider _paymentProvider;
    private readonly IPaymentRepository _paymentRepository;

    public PaymentService(IPaymentProvider paymentProvider, IPaymentRepository paymentRepository)
    {
        _paymentProvider = paymentProvider;
        _paymentRepository = paymentRepository;
    }

    
    // Metodo que se encarga de procesar el pago, crear el PaymentAttempt y persistir en la base - (Federico Diaz)
    public async Task<PaymentAttempt> ProcessPaymentAsync(string bookingId, string userId, decimal amount, string currency)
    {
        // Se define un mismo PaymentOperationId para los intentos en caso de fallo.
        var operationId = Guid.NewGuid();

        // Se reintenta hasta un maximo fijo de veces (3) con una espera fija entre intentos de 1 segundo.
        // Solo se reintenta ante un status Unavailable (una simulacion de falla transitoria del proveedor
        // por el mock): Success y Declined son resultados definitivos del negocio

        for (int attemptNumber = 1; attemptNumber <= 3; attemptNumber++)
        {
            var status = _paymentProvider.ProcessPayment();

            var attempt = new PaymentAttempt
            {
                Id = Guid.NewGuid(),
                PaymentOperationId = operationId,
                BookingId = bookingId,
                UserId = userId,
                Amount = amount,
                Currency = currency,
                Status = status,
                AttemptNumber = attemptNumber,
                CreatedAt = DateTime.UtcNow
            };

            // Se persiste cada intento de pago porque la consigna pide
            // incluye tanto los reintentos fallidos por Unavailable
            // como el intento que finalmente cierra la operacion.
            await _paymentRepository.AddAsync(attempt);

            if (status != PaymentStatus.Unavailable)
            {
                return attempt;
            }

            if (attemptNumber == 3)
            {
                return attempt;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new InvalidOperationException("No se pudo determinar el resultado del pago.");
    }
}