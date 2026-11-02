using System;
using System.Threading;
using System.Threading.Tasks;

namespace Booking.Service.Services.Payment;

public record PaymentResult(bool IsSuccess, string? ErrorMessage = null, string? TransactionId = null);

/// <summary>
/// Contrato para la comunicación con el microservicio Payment (vía gRPC según requerimiento 5 del flujo).
/// </summary>
public interface IPaymentServiceClient
{
    /// <summary>
    /// Invoca al microservicio de Payment para cobrar una reserva retenida.
    /// </summary>
    /// <param name="bookingId">Identificador único de la reserva.</param>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="amount">Monto del cobro.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado del cobro (exitoso o fallido).</returns>
    Task<PaymentResult> ProcessPaymentAsync(Guid bookingId, string userId, decimal amount, CancellationToken cancellationToken = default);
}
