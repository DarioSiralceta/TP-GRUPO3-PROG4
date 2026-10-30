using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Availability.Application.Events.Queries.GetEventSeats;
using Availability.Application.Interfaces;
using Availability.Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Availability.Infraestructure.Persistence.Repositories;

public class SeatRepository : ISeatRepository
{
    private readonly AvailabilityDbContext _context;

    public SeatRepository(AvailabilityDbContext context)
    {
        _context = context;
    }

    public async Task<bool> EventExistsAsync(int eventId)
    {
        return await _context.Events
            .AnyAsync(e => e.Id == eventId);
    }

    public async Task<List<GetEventSeatsResult>> GetByEventIdAsync(int eventId)
    {
        return await _context.Events
            .Where(e => e.Id == eventId)
            .SelectMany(e => e.Room.Seats)
            .AsNoTracking()
            .OrderBy(s => s.Number)
            .Select(s => new GetEventSeatsResult
            {
                SeatId = s.Id,
                Number = s.Number,
                IsAvailable = true
            })
            .ToListAsync();
    }
}
