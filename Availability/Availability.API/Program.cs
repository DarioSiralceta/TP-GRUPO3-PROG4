using Azure.Messaging.ServiceBus;
using Availability.Application.Interfaces;
using Availability.Application.Reservations.Request;
using Availability.Infraestructure.Messageing;






var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration["AzureServiceBus:ConnectionString"]
    ?? throw new InvalidOperationException(
        "Falta configurar AzureServiceBus:ConnectionString.");

var topicName = builder.Configuration["AzureServiceBus:TopicName"]
    ?? throw new InvalidOperationException(
        "Falta configurar AzureServiceBus:TopicName.");

builder.Services.AddSingleton(new ServiceBusClient(connectionString));

builder.Services.AddSingleton<IReservationRequestPublisher>(
    sp => new AzureServiceBusReservationRequestPublisher(
        sp.GetRequiredService<ServiceBusClient>(),
        topicName));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
