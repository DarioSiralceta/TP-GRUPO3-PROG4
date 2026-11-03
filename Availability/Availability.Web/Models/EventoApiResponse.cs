namespace Availability.Web.Models;

// Representa el contrato HTTP de Availability.API.
// Lo mantenemos separado del modelo que utiliza la vista.
public class EventoApiResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTimeOffset DateUtc { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
}