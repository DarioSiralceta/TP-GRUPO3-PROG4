using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Availability.Domain.Entities;

public class Event
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateTime DateUtc { get; set; }

    public int RoomId { get; set; }

    public Room Room { get; set; } = null!;
}