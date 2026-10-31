// src/Booking.API/Program.cs
using Booking.API.Workers;
using Booking.Infrastructure.Configuration;
using Booking.Infrastructure.Persistence;
using Booking.Service.Services;
using Booking.Service.Services.Payment;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers and OpenAPI / Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Service Bus Options Configuration
builder.Services.Configure<AzureServiceBusOptions>(
    builder.Configuration.GetSection(AzureServiceBusOptions.SectionName));

// Business services and clients (N-Layer architecture)
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentServiceClient, PaymentServiceClient>();

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
app.UseAuthorization();
app.MapControllers();

app.Run();
