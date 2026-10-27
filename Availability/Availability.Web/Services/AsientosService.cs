using Availability.Web.Models;

namespace Availability.Web.Services;

public class AsientosService
{
    public List<AsientoViewModel> ObtenerAsientos()
    {
        // Datos temporales hasta conectar con Availability.API.
        return new List<AsientoViewModel>
        {
            new() { Numero = 1, Disponible = true },
            new() { Numero = 2, Disponible = false },
            new() { Numero = 3, Disponible = true },
            new() { Numero = 4, Disponible = true }
        };
    }
}