using Availability.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Availability.Web.Controllers;

public class EventosController : Controller
{
    private readonly EventosService _eventosService;

    public EventosController(EventosService eventosService)
    {
        _eventosService = eventosService;
    }

    public IActionResult Index()
    {
        var eventos = _eventosService.ObtenerEventos();

        return View(eventos);
    }
}