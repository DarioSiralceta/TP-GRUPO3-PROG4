using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Availability.Application.Events.Queries.GetEvents;

public class GetEventsResult
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateTime DateUtc { get; set; }

    public int RoomId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public int Capacity { get; set; }
}