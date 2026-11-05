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
using Booking.Service.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// AI contribution: Gemini Flash 3.8 — Keycloak authentication and authorization registration.
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<AzureServiceBusOptions>(
    builder.Configuration.GetSection(AzureServiceBusOptions.SectionName));

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

builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// AI contributions: Gemini Flash 3.8 (reservation consumer) and Copilot (retention cleanup).
builder.Services.AddHostedService<BookingReservationWorker>();
builder.Services.AddHostedService<BookingRetentionExpirationWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// AI contribution: Gemini Flash 3.8 — authorization middleware pipeline.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
