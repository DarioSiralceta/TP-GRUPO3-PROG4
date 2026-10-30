using Microsoft.OpenApi.Models;
using Availability.Infraestructure.Persistence.Repositories;
using Azure.Messaging.ServiceBus;
using Availability.Application.Interfaces;
using Availability.Application.Reservations.Request;
using Availability.Infraestructure.Messageing;
using Availability.Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Authentication.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;





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
        topicName));

builder.Services.AddScoped<RequestReservationUseCase>();

// Add services to the container.

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
