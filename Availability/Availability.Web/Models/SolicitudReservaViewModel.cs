namespace Availability.Web.Models;

public class SolicitudReservaViewModel
{
    public int EventoId { get; set; }
    public string NombreEvento { get; set; } = string.Empty;

    public int SeatId { get; set; }
    public int NumeroAsiento { get; set; }

    public Guid? SolicitudId { get; set; }
}