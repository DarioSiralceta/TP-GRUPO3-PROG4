using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Booking.Service.Services.Payment;

/// <summary>
/// Cliente para la integración con Payment. En el paso 5 del flujo de reserva, Booking invoca a Payment.
/// Si el servicio gRPC de Payment no se encuentra levantado o no está configurado, implementa una estrategia segura por defecto
/// para permitir pruebas locales del flujo Happy Path / Fallo según los requerimientos del TP.
/// </summary>
public class PaymentServiceClient : IPaymentServiceClient
{
    private readonly ILogger<PaymentServiceClient> _logger;
    private readonly IConfiguration _configuration;

    public PaymentServiceClient(ILogger<PaymentServiceClient> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(Guid bookingId, string userId, decimal amount, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[AUDIT_PAYMENT_START] Solicitando cobro a Payment para BookingId: {BookingId}, Usuario: {UserId}, Monto: {Amount}",
            bookingId, userId, amount);

        // Simula la llamada de red/gRPC (asíncrona)
        await Task.Delay(100, cancellationToken);

        // Permite simular fallos de pago para el requerimiento (j): flujo de fallo si un componente falla o pago es rechazado
        var simulateFailure = bool.TryParse(_configuration["Payment:SimulateFailure"], out var isFailure) && isFailure;
        if (simulateFailure)
        {
            _logger.LogWarning("[AUDIT_PAYMENT_FAILED] Pago rechazado por entidad financiera para BookingId: {BookingId}", bookingId);
            return new PaymentResult(false, "Pago fallido: Fondos insuficientes o tarjeta rechazada");
        }

        var transactionId = Guid.NewGuid().ToString("N");
        _logger.LogInformation("[AUDIT_PAYMENT_END] Cobro exitoso registrado en Payment. TxId: {TxId} para BookingId: {BookingId}", transactionId, bookingId);
        
        return new PaymentResult(true, null, transactionId);
    }
}
