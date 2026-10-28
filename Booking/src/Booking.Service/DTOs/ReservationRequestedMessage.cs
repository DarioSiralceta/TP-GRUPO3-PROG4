using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Booking.Service.DTOs;

/// <summary>
/// Mensaje publicado por el microservicio Availability a través de Azure Service Bus (Topic: grupo3-booking-topic).
/// Cumple con el contrato de integración asíncrona entre Availability y Booking.
/// </summary>
public class ReservationRequestedMessage
{
    [JsonPropertyName("requestId")]
    public Guid RequestId { get; set; }

    [JsonPropertyName("eventId")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("seatId")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string SeatId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
}

/// <summary>
/// Permite deserializar números o strings de manera transparente evitando incompatibilidades
/// si Availability envía EventId o SeatId como entero o como string.
/// </summary>
public class FlexibleStringJsonConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out long l) ? l.ToString() : reader.GetDouble().ToString(),
            JsonTokenType.Null => null,
            _ => reader.GetString()
        };
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
