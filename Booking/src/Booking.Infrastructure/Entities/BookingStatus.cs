using System.Text.Json.Serialization;

namespace Booking.Infrastructure.Entities;

// AI contribution: Gemini Flash 3.8 — reservation lifecycle enum.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BookingStatus
{
    Retenida,
    Confirmada,
    Rechazada,
    Vencida
}
