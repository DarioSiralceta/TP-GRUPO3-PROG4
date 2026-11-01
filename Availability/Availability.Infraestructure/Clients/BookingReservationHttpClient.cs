using System.Net;
using System.Net.Http.Json;
using Availability.Application.Interfaces;
using Availability.Application.Reservations.Tracking;

namespace Availability.Infraestructure.Clients;

public class BookingReservationHttpClient : IBookingReservationClient
{
    private readonly HttpClient _httpClient;

    public BookingReservationHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BookingReservationResponse?> GetReservationByIdAsync(
        Guid requestId)
    {
        var response = await _httpClient.GetAsync(
            $"api/Bookings/{requestId}");

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