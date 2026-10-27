using Availability.Web.Models;
using Availability.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Availability.Web.Controllers;

public class ReservasController : Controller
{
    private readonly EventosService _eventosService;
    private readonly AsientosService _asientosService;
    private readonly SolicitudesService _solicitudesService;

    public ReservasController(
        EventosService eventosService,
        AsientosService asientosService,
        SolicitudesService solicitudesService)
    {
        _eventosService = eventosService;
        _asientosService = asientosService;
        _solicitudesService = solicitudesService;
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

        var solicitud = _solicitudesService.CrearSolicitud(
            evento.Nombre,
            asiento.Numero);

        var modelo = new SolicitudReservaViewModel
        {
            EventoId = evento.Id,
            NombreEvento = evento.Nombre,
            NumeroAsiento = asiento.Numero,
            SolicitudId = solicitud.SolicitudId
        };

        return View("Recibida", modelo);
    }

    [HttpGet]
    public IActionResult Seguimiento(string? solicitudId)
    {
        if (string.IsNullOrWhiteSpace(solicitudId))
        {
            return View();
        }

        if (!Guid.TryParse(solicitudId, out var id))
        {
            ViewBag.Error = "Ingresá un identificador válido.";
            return View();
        }

        var solicitud = _solicitudesService.ObtenerPorId(id);

        if (solicitud == null)
        {
            ViewBag.Error = "No se encontró una solicitud con ese identificador.";
            return View();
        }

        return View(solicitud);
    }
}