using Microsoft.EntityFrameworkCore;
using Payment.API.Services;
using Payment.BLL.Interfaces;
using Payment.BLL.Services;
using Payment.DAL.Context;
using Payment.DAL.Interfaces;
using Payment.DAL.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

// Registrar los servicios de pago
builder.Services.AddSingleton<IPaymentProvider, MockPaymentProvider>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// DbContext (SQLite) y repositorio
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PaymentDb")));
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

// TODO: registrar autenticacion.

var app = builder.Build();

app.MapGrpcService<PaymentGrpcService>();

app.Run();