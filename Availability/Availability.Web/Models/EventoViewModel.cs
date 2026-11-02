namespace Availability.Web.Models;

public class EventoViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Lugar { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}