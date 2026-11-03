namespace Availability.Web.Models;

public class SeguimientoViewModel
{
    public Guid SolicitudId { get; set; }
    public int EventoId { get; set; }
    public string NombreEvento { get; set; } = string.Empty;
    public int NumeroAsiento { get; set; }
    public int SeatId { get; set; }
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? MotivoRechazo { get; set; }
}