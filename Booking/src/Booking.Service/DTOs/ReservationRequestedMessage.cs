using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Booking.Service.DTOs;

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

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
}

// Accept numeric IDs as well as strings from Availability.
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
