using Availability.Web.Models;
using Availability.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Availability.Web.Controllers;

public class ReservasController : Controller
{
    private readonly EventosService _eventosService;
    private readonly AsientosService _asientosService;

    public ReservasController(
        EventosService eventosService,
        AsientosService asientosService)
    {
        _eventosService = eventosService;
        _asientosService = asientosService;
    }

    [HttpGet]
    public IActionResult Solicitar(int eventoId, int numeroAsiento)
    {
        var evento = _eventosService.ObtenerPorId(eventoId);

        if (evento == null)
        {
            return NotFound("El evento no existe.");
        }

        var asiento = _asientosService.ObtenerAsientos()
            .FirstOrDefault(a => a.Numero == numeroAsiento);

        if (asiento == null || !asiento.Disponible)
        {
            return BadRequest("El asiento no existe o no está disponible.");
        }

        var modelo = new SolicitudReservaViewModel
        {
            EventoId = evento.Id,
            NombreEvento = evento.Nombre,
            NumeroAsiento = asiento.Numero
        };

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Confirmar(int eventoId, int numeroAsiento)
    {
        var evento = _eventosService.ObtenerPorId(eventoId);

        if (evento == null)
        {
            return NotFound("El evento no existe.");
        }

        var asiento = _asientosService.ObtenerAsientos()
            .FirstOrDefault(a => a.Numero == numeroAsiento);

        if (asiento == null || !asiento.Disponible)
        {
            return BadRequest("El asiento no existe o no está disponible.");
        }

        var modelo = new SolicitudReservaViewModel
        {
            EventoId = evento.Id,
            NombreEvento = evento.Nombre,
            NumeroAsiento = asiento.Numero,

            // Identificador de prueba: no se registra una reserva real.
            SolicitudId = Guid.NewGuid()
        };

        return View("Recibida", modelo);
    }
}