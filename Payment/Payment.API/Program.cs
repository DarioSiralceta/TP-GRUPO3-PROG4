using Payment.API.Services;
using Payment.BLL.Interfaces;
using Payment.BLL.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

// Registrar los servicios de pago
builder.Services.AddSingleton<MockPaymentProvider>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// TODO: registrar DbContext, repositorios y autenticación.

var app = builder.Build();

app.MapGrpcService<PaymentGrpcService>();

app.Run();