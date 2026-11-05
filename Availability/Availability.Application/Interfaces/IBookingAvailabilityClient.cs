using System;


namespace Availability.Application.Interfaces;

public interface IBookingAvailabilityClient
{
    Task<List<string>> GetOccupiedSeatsAsync(int eventId);
}
