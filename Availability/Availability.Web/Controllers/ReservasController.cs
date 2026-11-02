using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Availability.Web.Models;
using Availability.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Availability.Web.Controllers;

public class ReservasController : Controller
{
    private readonly EventosService _eventosService;
    private readonly AsientosService _asientosService;
    private readonly ReservasApiService _reservasApiService;
    private readonly ILogger<ReservasController> _logger;

    private const string ClaveComprobante = "UltimaSolicitudRecibida";

    public ReservasController(
        EventosService eventosService,
        AsientosService asientosService,
        ReservasApiService reservasApiService,
        ILogger<ReservasController> logger)
    {
        _eventosService = eventosService;
        _asientosService = asientosService;
        _reservasApiService = reservasApiService;
        _logger = logger;
    }

    private string CorrelacionId =>
        Activity.Current?.TraceId.ToString()
        ?? HttpContext.TraceIdentifier;

    [HttpGet]
    public async Task<IActionResult> Solicitar(int eventoId, int seatId)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            var returnUrl = Url.Action(
                nameof(Solicitar),
                "Reservas",
                new { eventoId, seatId });

            return RedirectToAction(
                "RequiereLogin",
                "Account",
                new { returnUrl });
        }

        try
        {
            var modelo = await ObtenerModeloAsync(eventoId, seatId);

            if (modelo == null)
            {
                return BadRequest(
                    "El evento o asiento no existe, " +
                    "o el asiento ya no está disponible.");
            }

            return View(modelo);
        }
        catch (Exception ex) when (
            ex is HttpRequestException
            or TaskCanceledException
            or JsonException)
        {
            _logger.LogError(
                ex,
                "AUDITORIA ERROR_CONSULTA_DISPONIBILIDAD " +
                "CorrelacionId={CorrelacionId} EventoId={EventoId}",
                CorrelacionId,
                eventoId);

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "No pudimos consultar la disponibilidad. " +
                "Volvé al listado de asientos.");
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(int eventoId, int seatId)
    {
        var modelo = new SolicitudReservaViewModel
        {
            EventoId = eventoId,
            SeatId = seatId
        };

        // La cookie mantiene la sesión del MVC.
        // El access token permite autenticar la llamada a Availability.API.
        var accessToken = await HttpContext.GetTokenAsync("access_token");

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return VolverAlLogin(eventoId, seatId);
        }

        try
        {
            var modeloActual = await ObtenerModeloAsync(eventoId, seatId);

            if (modeloActual == null)
            {
                return BadRequest(
                    "El evento o asiento no existe, " +
                    "o el asiento ya no está disponible.");
            }

            modelo = modeloActual;
        }
        catch (Exception ex) when (
            ex is HttpRequestException
            or TaskCanceledException
            or JsonException)
        {
            _logger.LogError(
                ex,
                "AUDITORIA ERROR_VALIDACION_SOLICITUD " +
                "CorrelacionId={CorrelacionId} EventoId={EventoId}",
                CorrelacionId,
                eventoId);

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "No pudimos verificar la disponibilidad. " +
                "La solicitud no fue enviada.");
        }

        Guid requestId;

        try
        {
            requestId = await _reservasApiService.SolicitarAsync(
                eventoId,
                seatId,
                accessToken,
                CorrelacionId);
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Después del login regresamos al formulario mediante GET.
            // No reenviamos automáticamente la solicitud.
            return VolverAlLogin(eventoId, seatId);
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return MostrarError(
                modelo,
                StatusCodes.Status403Forbidden,
                "Tu usuario no tiene permiso para solicitar esta reserva.");
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            return MostrarError(
                modelo,
                StatusCodes.Status400BadRequest,
                "La API rechazó los datos de la solicitud. " +
                "Volvé al listado y seleccioná nuevamente el asiento.");
        }
        catch (Exception ex) when (
            ex is HttpRequestException
            or TaskCanceledException
            or JsonException)
        {
            _logger.LogError(
                ex,
                "AUDITORIA ERROR_ENVIO_SOLICITUD " +
                "CorrelacionId={CorrelacionId} " +
                "EventoId={EventoId} SeatId={SeatId}",
                CorrelacionId,
                eventoId,
                seatId);

            return MostrarError(
                modelo,
                StatusCodes.Status503ServiceUnavailable,
                "No pudimos obtener la confirmación de recepción. " +
                "La solicitud podría haberse registrado. " +
                "No la reenvíes automáticamente.");
        }

        modelo.SolicitudId = requestId;

        _logger.LogInformation(
            "AUDITORIA SOLICITUD_RECIBIDA " +
            "CorrelacionId={CorrelacionId} RequestId={RequestId} " +
            "EventoId={EventoId} SeatId={SeatId}",
            CorrelacionId,
            requestId,
            eventoId,
            seatId);

        // TempData conserva únicamente el último comprobante.
        // La persistencia de las solicitudes corresponde a Availability.API.
        TempData[ClaveComprobante] = JsonSerializer.Serialize(modelo);

        // Post/Redirect/Get evita reenviar el formulario al recargar.
        return RedirectToAction(
            nameof(Recibida),
            new { solicitudId = requestId });
    }

    [Authorize]
    [HttpGet]
    public IActionResult Recibida(Guid solicitudId)
    {
        // Peek permite recargar el comprobante sin consumirlo.
        var contenido = TempData.Peek(ClaveComprobante) as string;

        if (string.IsNullOrWhiteSpace(contenido))
        {
            return NotFound(
                "El comprobante no está disponible en este navegador. " +
                "Esto no significa que la solicitud haya sido eliminada.");
        }

        var modelo = JsonSerializer
            .Deserialize<SolicitudReservaViewModel>(contenido);

        if (modelo == null || modelo.SolicitudId != solicitudId)
        {
            return NotFound(
                "No se encontró este comprobante en la sesión actual.");
        }

        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Seguimiento(string? solicitudId)
    {
        ViewBag.SolicitudId = solicitudId;

        var returnUrl = Url.Action(
            nameof(Seguimiento),
            "Reservas",
            new { solicitudId });

        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(
                "RequiereLogin",
                "Account",
                new { returnUrl });
        }

        if (string.IsNullOrWhiteSpace(solicitudId))
        {
            return View();
        }

        if (!Guid.TryParse(solicitudId, out var id) || id == Guid.Empty)
        {
            ViewBag.Error = "Ingresá un identificador de solicitud válido.";
            return View();
        }

        var accessToken = await HttpContext.GetTokenAsync("access_token");

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return RedirectToAction(
                "Login",
                "Account",
                new { returnUrl });
        }

        try
        {
            // Availability.API valida la pertenencia de la solicitud
            // y consulta a Booking para obtener su estado.
            var resultado = await _reservasApiService
                .ObtenerSeguimientoAsync(
                    id,
                    accessToken,
                    CorrelacionId);

            var modelo = new SeguimientoViewModel
            {
                SolicitudId = resultado.RequestId,
                EventoId = resultado.EventId,
                SeatId = resultado.SeatId,
                Estado = resultado.Status,
                MotivoRechazo = resultado.RejectionReason
            };

            _logger.LogInformation(
                "AUDITORIA SEGUIMIENTO_CONSULTADO " +
                "CorrelacionId={CorrelacionId} RequestId={RequestId} " +
                "Estado={Estado}",
                CorrelacionId,
                id,
                resultado.Status);

            return View(modelo);
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction(
                "Login",
                "Account",
                new { returnUrl });
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            _logger.LogWarning(
                "AUDITORIA SEGUIMIENTO_DENEGADO " +
                "CorrelacionId={CorrelacionId} RequestId={RequestId}",
                CorrelacionId,
                id);

            Response.StatusCode = StatusCodes.Status403Forbidden;
            ViewBag.Error = "No tenés permiso para consultar esta solicitud.";

            return View();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            ViewBag.Error =
                "No se encontró una solicitud con ese identificador.";

            return View();
        }
        catch (Exception ex) when (
            ex is HttpRequestException
            or TaskCanceledException
            or JsonException)
        {
            _logger.LogError(
                ex,
                "AUDITORIA ERROR_SEGUIMIENTO " +
                "CorrelacionId={CorrelacionId} RequestId={RequestId}",
                CorrelacionId,
                id);

            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            ViewBag.Error =
                "No pudimos consultar el resultado en este momento. " +
                "Intentá nuevamente en unos instantes.";

            return View();
        }
    }

    private async Task<SolicitudReservaViewModel?> ObtenerModeloAsync(
        int eventoId,
        int seatId)
    {
        var evento = await _eventosService.ObtenerPorIdAsync(eventoId);

        if (evento == null)
        {
            return null;
        }

        var asientos = await _asientosService
            .ObtenerAsientosAsync(eventoId);

        // El identificador real se utiliza para buscar y enviar el asiento.
        // El número visible se obtiene de la API para mostrarlo.
        var asiento = asientos.FirstOrDefault(a => a.SeatId == seatId);

        if (asiento == null || !asiento.Disponible)
        {
            _logger.LogWarning(
                "AUDITORIA ASIENTO_NO_DISPONIBLE " +
                "CorrelacionId={CorrelacionId} " +
                "EventoId={EventoId} SeatId={SeatId}",
                CorrelacionId,
                eventoId,
                seatId);

            return null;
        }

        return new SolicitudReservaViewModel
        {
            EventoId = evento.Id,
            NombreEvento = evento.Nombre,
            SeatId = asiento.SeatId,
            NumeroAsiento = asiento.Numero
        };
    }

    private IActionResult VolverAlLogin(int eventoId, int seatId)
    {
        var returnUrl = Url.Action(
            nameof(Solicitar),
            "Reservas",
            new { eventoId, seatId });

        return RedirectToAction(
            "Login",
            "Account",
            new { returnUrl });
    }

    private IActionResult MostrarError(
        SolicitudReservaViewModel modelo,
        int statusCode,
        string mensaje)
    {
        Response.StatusCode = statusCode;
        ViewBag.Error = mensaje;

        return View("Solicitar", modelo);
    }
}