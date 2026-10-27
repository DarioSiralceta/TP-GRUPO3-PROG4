using Availability.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Availability.Web.Controllers;

public class AsientosController : Controller
{
    private readonly AsientosService _asientosService;
    private readonly EventosService _eventosService;

    public AsientosController(
        AsientosService asientosService,
        EventosService eventosService)
    {
        _asientosService = asientosService;
        _eventosService = eventosService;
    }

    public IActionResult Index(int eventoId)
    {
        var evento = _eventosService.ObtenerPorId(eventoId);

        if (evento == null)
        {
            return NotFound("El evento no existe.");
        }

        var asientos = _asientosService.ObtenerAsientos();

        ViewBag.NombreEvento = evento.Nombre;

        return View(asientos);
    }
}