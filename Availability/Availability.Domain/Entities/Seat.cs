using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Availability.Domain.Entities;

public class Seat
{
    public int Id { get; set; }

    public int Number { get; set; }

    public int RoomId { get; set; }

    public Room Room { get; set; } = null!;
}
