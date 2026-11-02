namespace Availability.Web.Models;

public class AsientoViewModel
{
    // Identificador real del asiento; puede ser distinto del número visible.
    public int SeatId { get; set; }

    public int Numero { get; set; }
    public bool Disponible { get; set; }
}