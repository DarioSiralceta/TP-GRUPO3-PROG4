using System.Diagnostics;
using Availability.Web.Models;
using Availability.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Availability.Web.Controllers;

public class ReservasController : Controller
{
    private readonly EventosService _eventosService;
    private readonly AsientosService _asientosService;
    private readonly SolicitudesService _solicitudesService;
    private readonly ILogger<ReservasController> _logger;

    public ReservasController(
        EventosService eventosService,
        AsientosService asientosService,
        SolicitudesService solicitudesService,
        ILogger<ReservasController> logger)
    {
        _eventosService = eventosService;
        _asientosService = asientosService;
        _solicitudesService = solicitudesService;
        _logger = logger;
    }

    // El middleware registra el inicio y fin de cada petición.
    // Los eventos del controlador usan la misma correlación para
    // asociar la operación HTTP con la solicitud simulada.
    private string CorrelacionId =>
        Activity.Current?.TraceId.ToString()
        ?? HttpContext.TraceIdentifier;

    [HttpGet]
    public async Task<IActionResult> Solicitar(int eventoId, int numeroAsiento)
    {
        // Mostramos un aviso antes del formulario si no hay sesión.
        // Conservamos el evento y asiento para regresar después del login.
        if (User.Identity?.IsAuthenticated != true)
        {
            var returnUrl = Url.Action(
                nameof(Solicitar),
                "Reservas",
                new { eventoId, numeroAsiento });

            return RedirectToAction(
                "RequiereLogin",
                "Account",
                new { returnUrl });
        }

        var evento = await _eventosService.ObtenerPorIdAsync(eventoId);

        if (evento == null)
        {
            _logger.LogWarning(
                "AUDITORIA EVENTO_NO_ENCONTRADO " +
                "CorrelacionId={CorrelacionId} EventoId={EventoId}",
                CorrelacionId,
                eventoId);

            return NotFound("El evento no existe.");
        }

        var asiento = _asientosService.ObtenerAsientos()
            .FirstOrDefault(a => a.Numero == numeroAsiento);

        if (asiento == null || !asiento.Disponible)
        {
            _logger.LogWarning(
                "AUDITORIA ASIENTO_NO_DISPONIBLE " +
                "CorrelacionId={CorrelacionId} " +
                "EventoId={EventoId} NumeroAsiento={NumeroAsiento}",
                CorrelacionId,
                eventoId,
                numeroAsiento);

            return BadRequest(
                "El asiento no existe o no está disponible.");
        }

        var modelo = new SolicitudReservaViewModel
        {
            EventoId = evento.Id,
            NombreEvento = evento.Nombre,
            NumeroAsiento = asiento.Numero
        };

        return View(modelo);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(int eventoId, int numeroAsiento)
    {
        var evento = await _eventosService.ObtenerPorIdAsync(eventoId);

        if (evento == null)
        {
            _logger.LogWarning(
                "AUDITORIA EVENTO_NO_ENCONTRADO " +
                "CorrelacionId={CorrelacionId} EventoId={EventoId}",
                CorrelacionId,
                eventoId);

            return NotFound("El evento no existe.");
        }

        var asiento = _asientosService.ObtenerAsientos()
            .FirstOrDefault(a => a.Numero == numeroAsiento);

        if (asiento == null || !asiento.Disponible)
        {
            _logger.LogWarning(
                "AUDITORIA ASIENTO_NO_DISPONIBLE " +
                "CorrelacionId={CorrelacionId} " +
                "EventoId={EventoId} NumeroAsiento={NumeroAsiento}",
                CorrelacionId,
                eventoId,
                numeroAsiento);

            return BadRequest(
                "El asiento no existe o no está disponible.");
        }

        var solicitud = _solicitudesService.CrearSolicitud(
            evento.Id,
            evento.Nombre,
            asiento.Numero);

        _logger.LogInformation(
            "AUDITORIA SOLICITUD_SIMULADA_CREADA " +
            "CorrelacionId={CorrelacionId} SolicitudId={SolicitudId} " +
            "EventoId={EventoId} NumeroAsiento={NumeroAsiento}",
            CorrelacionId,
            solicitud.SolicitudId,
            evento.Id,
            asiento.Numero);

        // Post/Redirect/Get: redirigimos después del POST para que
        // recargar la página de resultado no reenvíe el formulario.
        return RedirectToAction(
            nameof(Recibida),
            new { solicitudId = solicitud.SolicitudId });
    }

    [HttpGet]
    public IActionResult Recibida(Guid solicitudId)
    {
        var solicitud = _solicitudesService.ObtenerPorId(solicitudId);

        if (solicitud == null)
        {
            _logger.LogWarning(
                "AUDITORIA SOLICITUD_SIMULADA_NO_ENCONTRADA " +
                "CorrelacionId={CorrelacionId} SolicitudId={SolicitudId}",
                CorrelacionId,
                solicitudId);

            return NotFound(
                "La solicitud no existe o se perdió al reiniciar la aplicación.");
        }

        var modelo = new SolicitudReservaViewModel
        {
            EventoId = solicitud.EventoId,
            NombreEvento = solicitud.NombreEvento,
            NumeroAsiento = solicitud.NumeroAsiento,
            SolicitudId = solicitud.SolicitudId
        };

        return View(modelo);
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
            // No registramos el texto recibido: alcanza con identificar
            // la petición y el motivo por el que se rechazó la consulta.
            _logger.LogWarning(
                "AUDITORIA IDENTIFICADOR_INVALIDO " +
                "CorrelacionId={CorrelacionId}",
                CorrelacionId);

            ViewBag.Error = "Ingresá un identificador válido.";
            return View();
        }

        var solicitud = _solicitudesService.ObtenerPorId(id);

        _logger.LogInformation(
            "AUDITORIA SEGUIMIENTO_SIMULADO " +
            "CorrelacionId={CorrelacionId} SolicitudId={SolicitudId} " +
            "Encontrada={Encontrada} Estado={Estado}",
            CorrelacionId,
            id,
            solicitud != null,
            solicitud?.Estado);

        if (solicitud == null)
        {
            ViewBag.Error =
                "No se encontró una solicitud con ese identificador.";

            return View();
        }

        return View(solicitud);
    }
}