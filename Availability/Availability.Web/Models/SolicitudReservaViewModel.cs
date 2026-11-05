namespace Availability.Web.Models;

public class SolicitudReservaViewModel
{
    public int EventoId { get; set; }
    public string NombreEvento { get; set; } = string.Empty;
    public int SeatId { get; set; }
    public int NumeroAsiento { get; set; }
    // Precio de referencia del catálogo para mostrar en el formulario.
    public decimal Precio { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public Guid? SolicitudId { get; set; }
}