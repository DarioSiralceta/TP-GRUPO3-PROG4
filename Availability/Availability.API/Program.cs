using Availability.Application.Events.Queries.GetEvents;
using Availability.Application.Events.Queries.GetEventSeats;
using Availability.Application.Interfaces;
using Availability.Application.Reservations.Request;
using Availability.Infraestructure.Clients;
using Availability.Infraestructure.Messageing;
using Availability.Infraestructure.Persistence;
using Availability.Infraestructure.Persistence.Repositories;
using Availability.Infraestructure.Repositories;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Shared.Authentication.Extensions;








var builder = WebApplication.CreateBuilder(args);
builder.Services.AddKeycloakAuthentication(
    builder.Configuration);

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
        topicName,
        sp.GetRequiredService<ILogger<AzureServiceBusReservationRequestPublisher>>()));

builder.Services.AddScoped<RequestReservationUseCase>();

// Add services to the container.

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<GetEventsUseCase>();

builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<GetEventSeatsUseCase>();

builder.Services.AddHttpClient<IBookingAvailabilityClient, BookingAvailabilityHttpClient>(
    client =>
    {
        var baseUrl = builder.Configuration["BookingApi:BaseUrl"]
            ?? throw new InvalidOperationException(
                "Falta configurar BookingApi:BaseUrl.");

        client.BaseAddress = new Uri(baseUrl);
    });

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresá el access token de Keycloak."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


builder.Services.AddDbContext<AvailabilityDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("AvailabilityDb")));

builder.Services.AddScoped<IReservationRequestRepository, ReservationRequestRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
