// src/Booking.API/Program.cs
using System.Text.Json.Serialization;
using Booking.API.Workers;
using Booking.Infrastructure.Configuration;
using Booking.Infrastructure.Persistence;
using Booking.Service.Services;
using Booking.Service.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Payment.API.Protos;
using Shared.Authentication.Extensions;
using Shared.Authentication.Services;


var builder = WebApplication.CreateBuilder(args);

// Keycloak Authentication and Authorization from Shared.Authentication
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAuthorization();

// Controllers and OpenAPI / Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Service Bus Options Configuration
builder.Services.Configure<AzureServiceBusOptions>(
    builder.Configuration.GetSection(AzureServiceBusOptions.SectionName));

// Business services and clients (N-Layer architecture)
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentServiceClient, PaymentServiceClient>();
builder.Services.AddHttpClient<KeycloakTokenService>();

var paymentGrpcAddress = builder.Configuration["Payment:GrpcAddress"];
if (!Uri.TryCreate(paymentGrpcAddress, UriKind.Absolute, out var paymentAddress))
{
    throw new InvalidOperationException("La URL gRPC de Payment no está configurada correctamente en Payment:GrpcAddress.");
}

builder.Services.AddGrpcClient<PaymentService.PaymentServiceClient>(options =>
    options.Address = paymentAddress);

// Register PostgreSQL DbContext
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Background Worker for processing reservation messages from Availability via Azure Service Bus
builder.Services.AddHostedService<BookingReservationWorker>();

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
