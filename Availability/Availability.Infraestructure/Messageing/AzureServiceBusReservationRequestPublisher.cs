
using Availability.Application.Interfaces;
using Availability.Application.Reservations.Messages;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Availability.Infraestructure.Messageing
{
    public class AzureServiceBusReservationRequestPublisher
        : IReservationRequestPublisher, IAsyncDisposable
    {
        private readonly ServiceBusSender _sender;
        private readonly ILogger<AzureServiceBusReservationRequestPublisher> _logger;

        public AzureServiceBusReservationRequestPublisher(
            ServiceBusClient client,
            string topicName,
            ILogger<AzureServiceBusReservationRequestPublisher> logger)
        {
            _sender = client.CreateSender(topicName);
            _logger = logger;
        }

        public async Task PublishAsync(ReservationRequested message)
        {
            try
            {
                string jsonMessage = JsonSerializer.Serialize(message);

                var serviceBusMessage = new ServiceBusMessage(jsonMessage)
                {
                    MessageId = message.RequestId.ToString(),
                    ContentType = "application/json"
                };

                await _sender.SendMessageAsync(serviceBusMessage);

                _logger.LogInformation(
                    "Solicitud {RequestId} publicada correctamente en Azure Service Bus.",
                    message.RequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al publicar la solicitud {RequestId} en Azure Service Bus.",
                    message.RequestId);

                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _sender.DisposeAsync();
        }
    }
}
