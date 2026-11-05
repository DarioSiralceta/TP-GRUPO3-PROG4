using System.Net.Http.Json;
using System.Text.Json;
using Availability.Web.Models;

namespace Availability.Web.Services;

public class AsientosService
{
    private readonly HttpClient _httpClient;

    public AsientosService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<AsientoViewModel>> ObtenerAsientosAsync(
        int eventoId)
    {
        var respuesta = await _httpClient
            .GetFromJsonAsync<List<AsientoApiResponse>>(
                $"events/{eventoId}/seats");

        // Una respuesta inválida no debe interpretarse como una sala vacía.
        if (respuesta == null)
        {
            throw new JsonException(
                "Availability.API devolvió una respuesta vacía.");
        }

        // Adaptamos el contrato HTTP al modelo utilizado por las vistas.
        return respuesta
            .Select(asiento => new AsientoViewModel
            {
                SeatId = asiento.SeatId,
                Numero = asiento.Number,
                Disponible = asiento.IsAvailable
            })
            .ToList();
    }
}