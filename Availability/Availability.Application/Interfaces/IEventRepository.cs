using Availability.Application.Events.Queries.GetEvents;
using Availability.Domain.Entities;
using System;

namespace Availability.Application.Interfaces;

public interface IEventRepository
{
    Task<List<GetEventsResult>> GetAllAsync();

    Task<Event?> GetByIdAsync(int eventId);
}
