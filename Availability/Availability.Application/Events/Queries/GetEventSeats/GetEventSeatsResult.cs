using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Availability.Application.Events.Queries.GetEventSeats;

public class GetEventSeatsResult
{
    public int SeatId { get; set; }

    public int Number { get; set; }

    public bool IsAvailable { get; set; }
}
