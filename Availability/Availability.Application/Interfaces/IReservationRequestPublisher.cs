using Availability.Application.Reservations.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Availability.Application.Interfaces
{
    public interface IReservationRequestPublisher
    {

        Task PublishAsync(ReservationRequested message);
    }
}
