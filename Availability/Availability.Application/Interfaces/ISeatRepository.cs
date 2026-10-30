using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Availability.Application.Events.Queries.GetEventSeats;

namespace Availability.Application.Interfaces;

public interface ISeatRepository
{
    Task<bool> EventExistsAsync(int eventId);

    Task<List<GetEventSeatsResult>> GetByEventIdAsync(int eventId);
}