using Availability.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Availability.Web.Controllers;

public class AsientosController : Controller
{
    public IActionResult Index()
    {
        var asientos = new List<AsientoViewModel>
        {
            new() { Numero = 1, Disponible = true },
            new() { Numero = 2, Disponible = false },
            new() { Numero = 3, Disponible = true },
            new() { Numero = 4, Disponible = true }
        };

        return View(asientos);
    }
}