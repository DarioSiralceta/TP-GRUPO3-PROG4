using System.Text.Json;
using Availability.Web.Models;
using Availability.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Availability.Web.Controllers;

public class AsientosController : Controller
{
    private readonly AsientosService _asientosService;
    private readonly EventosService _eventosService;
    private readonly ILogger<AsientosController> _logger;

    public AsientosController(
        AsientosService asientosService,
        EventosService eventosService,
        ILogger<AsientosController> logger)
    {
        _asientosService = asientosService;
        _eventosService = eventosService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int eventoId)
    {
        ViewBag.EventoId = eventoId;

        try
        {
            var evento = await _eventosService
                .ObtenerPorIdAsync(eventoId);

            if (evento == null)
            {
                return NotFound("El evento no existe.");
            }

            ViewBag.NombreEvento = evento.Nombre;

            var asientos = await _asientosService
                .ObtenerAsientosAsync(eventoId);

            return View(asientos);
        }
        catch (Exception ex) when (
            ex is HttpRequestException
            or TaskCanceledException
            or JsonException)
        {
            _logger.LogError(
                ex,
                "No se pudieron consultar los asientos. " +
                "EventoId={EventoId} TraceId={TraceId}",
                eventoId,
                HttpContext.TraceIdentifier);

            // Si falla la consulta, no mostramos asientos como disponibles.
            Response.StatusCode =
                StatusCodes.Status503ServiceUnavailable;

            ViewBag.Error =
                "No pudimos consultar la disponibilidad en este momento. " +
                "Intentá nuevamente en unos instantes.";

            return View(new List<AsientoViewModel>());
        }
    }
}