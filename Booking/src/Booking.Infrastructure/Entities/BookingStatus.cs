using System.Text.Json.Serialization;

namespace Booking.Infrastructure.Entities;

/// <summary>
/// Representa el ciclo de vida de un asiento / reserva en el microservicio Booking:
/// - Retenida: Reserva temporal (bloqueo preventivo de 2 minutos mientras se procesa el cobro).
/// - Confirmada: Reserva pagada y confirmada exitosamente.
/// - Rechazada: Reserva rechazada (por asiento ocupado o pago fallido).
/// - Vencida: Retención expirada por tiempo límite sin confirmación de pago.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BookingStatus
{
    Retenida,
    Confirmada,
    Rechazada,
    Vencida
}
