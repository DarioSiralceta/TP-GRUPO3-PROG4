namespace Booking.Infrastructure.Configuration;

public class AzureServiceBusOptions
{
    public const string SectionName = "AzureServiceBus";

    public string ConnectionString { get; set; } = string.Empty;

    public string TopicName { get; set; } = "grupo3-booking-topic";

    public string SubscriptionName { get; set; } = "booking-subscription";

    public int MaxConcurrentCalls { get; set; } = 1;
}
