using System;
using Availability.Application.Events.Queries.GetEvents;

namespace Availability.Application.Interfaces;

public interface IEventRepository
{
    Task<List<GetEventsResult>> GetAllAsync();
}
