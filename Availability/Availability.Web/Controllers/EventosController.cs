using System.Text.Json;
using Availability.Web.Models;
using Availability.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Availability.Web.Controllers;

public class EventosController : Controller
{
    private readonly EventosService _eventosService;
    private readonly ILogger<EventosController> _logger;

    public EventosController(
        EventosService eventosService,
        ILogger<EventosController> logger)
    {
        _eventosService = eventosService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var eventos = await _eventosService.ObtenerEventosAsync();

            return View(eventos);
        }
        catch (Exception ex) when (
            ex is HttpRequestException
            or TaskCanceledException
            or JsonException)
        {
            _logger.LogError(
                ex,
                "No se pudo consultar el catálogo de Availability.API.");

            Response.StatusCode =
                StatusCodes.Status503ServiceUnavailable;

            ViewBag.Error =
                "No pudimos cargar los eventos. Intentá nuevamente en unos momentos.";

            return View(new List<EventoViewModel>());
        }
    }
}