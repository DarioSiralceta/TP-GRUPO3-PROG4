using System;
using Availability.Application.Events.Queries.GetEvents;
using Availability.Application.Interfaces;
using Availability.Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Availability.Infraestructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AvailabilityDbContext _context;

    public EventRepository(AvailabilityDbContext context)
    {
        _context = context;
    }

    public async Task<List<GetEventsResult>> GetAllAsync()
    {
        return await _context.Events
            .AsNoTracking()
            .Include(e => e.Room)
            .Select(e => new GetEventsResult
            {
                Id = e.Id,
                Name = e.Name,
                Location = e.Location,
                DateUtc = e.DateUtc,
                RoomId = e.RoomId,
                RoomName = e.Room.Name,
                Capacity = e.Room.Capacity
            })
            .ToListAsync();
    }
}
