using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Availability.Application.Interfaces;
using Availability.Application.Reservations.Tracking;
using Microsoft.Extensions.Configuration;
using Shared.Authentication.Services;

namespace Availability.Infraestructure.Clients;

public class BookingReservationHttpClient : IBookingReservationClient
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakTokenService _tokenService;
    private readonly IConfiguration _configuration;

    public BookingReservationHttpClient(
        HttpClient httpClient,
        KeycloakTokenService tokenService,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<BookingReservationResponse?> GetReservationByIdAsync(
        Guid requestId)
    {
        // Llamada service-to-service: Booking exige el rol "booking.read" en
        // CanReadBooking, asi que pedimos un token propio via client_credentials
        // (mismo patron que Booking usa contra Payment con KeycloakTokenService).
        var authority = _configuration["Keycloak:Authority"]
            ?? throw new InvalidOperationException("Falta configurar Keycloak:Authority.");
        var clientId = _configuration["Keycloak:ClientId"]
            ?? throw new InvalidOperationException("Falta configurar Keycloak:ClientId.");
        var clientSecret = _configuration["Keycloak:ClientSecret"]
            ?? throw new InvalidOperationException("Falta configurar Keycloak:ClientSecret.");

        var accessToken = await _tokenService.GetAccessTokenAsync(
            authority, clientId, clientSecret);

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/Bookings/{requestId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var booking = await response.Content
            .ReadFromJsonAsync<BookingReservationResponse>();

        return booking ?? throw new InvalidOperationException(
            "Booking devolvió una respuesta vacía.");
    }
}