using Availability.Web.Models;

namespace Availability.Web.Services;

public class EventosService
{
    public List<EventoViewModel> ObtenerEventos()
    {
        // Datos simulados hasta conectar con la API.
        return new List<EventoViewModel>
        {
            new()
            {
                Id = 1,
                Nombre = "Concierto de música",
                Lugar = "Teatro Central",
                Fecha = new DateTime(2026, 12, 10, 20, 0, 0)
            },
            new()
            {
                Id = 2,
                Nombre = "Obra de teatro",
                Lugar = "Sala Principal",
                Fecha = new DateTime(2026, 12, 15, 21, 0, 0)
            }
        };
    }

    public EventoViewModel? ObtenerPorId(int id)
    {
        return ObtenerEventos().FirstOrDefault(evento => evento.Id == id);
    }
}