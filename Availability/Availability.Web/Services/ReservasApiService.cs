using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Availability.Web.Models;

namespace Availability.Web.Services;

public class ReservasApiService
{
    private readonly HttpClient _httpClient;

    public ReservasApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Guid> SolicitarAsync(
        int eventoId,
        int seatId,
        string accessToken,
        string correlacionId)
    {
        using var peticion = new HttpRequestMessage(
            HttpMethod.Post,
            "Reservations");

        // El token se asigna a esta petición para evitar compartir
        // credenciales entre usuarios mediante encabezados del cliente.
        peticion.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        peticion.Headers.Add("X-Correlation-ID", correlacionId);

        peticion.Content = JsonContent.Create(new
        {
            EventId = eventoId,
            SeatId = seatId
        });

        // No reintentamos automáticamente un POST:
        // una respuesta perdida podría corresponder a una solicitud creada.
        using var respuesta = await _httpClient.SendAsync(peticion);

        respuesta.EnsureSuccessStatusCode();

        var resultado = await respuesta.Content
            .ReadFromJsonAsync<SolicitudApiResponse>();

        if (resultado == null || resultado.RequestId == Guid.Empty)
        {
            throw new JsonException(
                "La API no devolvió un identificador de solicitud válido.");
        }

        return resultado.RequestId;
    }
}