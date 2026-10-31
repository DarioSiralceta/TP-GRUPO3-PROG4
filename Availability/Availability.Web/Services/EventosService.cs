using System.Net.Http.Json;
using System.Text.Json;
using Availability.Web.Models;

namespace Availability.Web.Services;

public class EventosService
{
    private readonly HttpClient _httpClient;

    public EventosService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<EventoViewModel>> ObtenerEventosAsync()
    {
        var eventos = await _httpClient
            .GetFromJsonAsync<List<EventoApiResponse>>("events");

        if (eventos == null)
        {
            throw new JsonException(
                "Availability.API devolvió una respuesta vacía.");
        }

        // Adaptamos el contrato de la API al modelo de presentación.
        return eventos.Select(evento => new EventoViewModel
        {
            Id = evento.Id,
            Nombre = evento.Name,
            Lugar = evento.Location,

            // La API informa UTC; mostramos la hora local del servidor MVC.
            Fecha = evento.DateUtc.LocalDateTime
        }).ToList();
    }

    public async Task<EventoViewModel?> ObtenerPorIdAsync(int id)
    {
        // Por ahora la API expone el listado, sin un GET individual.
        var eventos = await ObtenerEventosAsync();

        return eventos.FirstOrDefault(evento => evento.Id == id);
    }
}