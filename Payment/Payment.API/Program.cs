using Microsoft.EntityFrameworkCore;
using Payment.API.Services;
using Payment.BLL.Interfaces;
using Payment.BLL.Services;
using Payment.DAL.Context;
using Payment.DAL.Interfaces;
using Payment.DAL.Repositories;
using Shared.Authentication.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAuthorization();

// Registrar los servicios de pago
builder.Services.AddSingleton<IPaymentProvider, MockPaymentProvider>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// DbContext (SQLite) y repositorio
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PaymentDb")));
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<PaymentGrpcService>()
    .RequireAuthorization("CanProcessPayment");

app.Run();