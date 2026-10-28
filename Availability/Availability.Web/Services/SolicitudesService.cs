using Availability.Web.Models;
using System.Collections.Concurrent;

namespace Availability.Web.Services;

public class SolicitudesService
{
    private readonly ConcurrentDictionary<Guid, SeguimientoViewModel> _solicitudes
        = new();

    public SeguimientoViewModel CrearSolicitud(
        int eventoId,
        string nombreEvento,
        int numeroAsiento)
    {
        var solicitud = new SeguimientoViewModel
        {
            SolicitudId = Guid.NewGuid(),
            EventoId = eventoId,
            NombreEvento = nombreEvento,
            NumeroAsiento = numeroAsiento,
            Estado = "Pendiente"
        };

        _solicitudes[solicitud.SolicitudId] = solicitud;

        return solicitud;
    }

    public SeguimientoViewModel? ObtenerPorId(Guid solicitudId)
    {
        _solicitudes.TryGetValue(solicitudId, out var solicitud);
        return solicitud;
    }
}