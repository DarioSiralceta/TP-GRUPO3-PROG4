using System;
using Availability.Application.Interfaces;

namespace Availability.Application.Events.Queries.GetEvents;

public class GetEventsUseCase
{
    private readonly IEventRepository _eventRepository;

    public GetEventsUseCase(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<List<GetEventsResult>> ExecuteAsync()
    {
        return await _eventRepository.GetAllAsync();
    }
}