namespace Booking.Infrastructure.Configuration;

/// <summary>
/// Opciones de configuración para la integración con Azure Service Bus.
/// </summary>
public class AzureServiceBusOptions
{
    public const string SectionName = "AzureServiceBus";

    /// <summary>
    /// Cadena de conexión completa al namespace de Azure Service Bus.
    /// Puede incluir EntityPath o no.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del Topic donde Availability publica las solicitudes de reserva.
    /// Valor por defecto: "grupo3-booking-topic".
    /// </summary>
    public string TopicName { get; set; } = "grupo3-booking-topic";

    /// <summary>
    /// Nombre de la Suscripción en el Topic de Service Bus.
    /// Permite el patrón Competing Consumers entre réplicas de Booking API.
    /// </summary>
    public string SubscriptionName { get; set; } = "booking-subscription";

    /// <summary>
    /// Cantidad máxima de mensajes procesados concurrentemente por cada réplica.
    /// </summary>
    public int MaxConcurrentCalls { get; set; } = 1;
}
