using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Availability.Application.Interfaces;
using Availability.Application.Reservations.Messages;
using Azure.Messaging.ServiceBus;
using System.Text.Json;


namespace Availability.Infraestructure.Messageing
{
    public class AzureServiceBusReservationRequestPublisher : IReservationRequestPublisher, IAsyncDisposable
    {
        private ServiceBusSender _sender;

        public AzureServiceBusReservationRequestPublisher(ServiceBusClient client, string topicName)
        {
            _sender = client.CreateSender(topicName);
        }

        public async Task PublishAsync(ReservationRequested message)
        {

            string jsonMessage = JsonSerializer.Serialize(message);

            ServiceBusMessage serviceBusMessage = new ServiceBusMessage(jsonMessage);

            await _sender.SendMessageAsync(serviceBusMessage);

            

            
        }

        public async ValueTask DisposeAsync()
        {
            await _sender.DisposeAsync();
        }
    }
}
