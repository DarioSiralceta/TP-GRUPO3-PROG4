using Payment.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

// TODO: registrar DbContext, repositorios, servicios y autenticación una vez definidos.

var app = builder.Build();

app.MapGrpcService<PaymentGrpcService>();

app.Run();
