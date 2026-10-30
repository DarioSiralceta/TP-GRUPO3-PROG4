using System;
using Availability.Application.Interfaces;
using System.Net.Http.Json;

namespace Availability.Infraestructure.Clients;

public class BookingAvailabilityHttpClient : IBookingAvailabilityClient
{
    private readonly HttpClient _httpClient;

    public BookingAvailabilityHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<string>> GetOccupiedSeatsAsync(int eventId)
    {
        var occupiedSeats = await _httpClient.GetFromJsonAsync<List<string>>(
            $"api/Bookings/events/{eventId}/occupied-seats");

        return occupiedSeats ?? new List<string>();
    }
}