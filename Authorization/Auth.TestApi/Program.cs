
using Shared.Authentication.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Registramos la autenticación JWT con Keycloak.
builder.Services.AddKeycloakAuthentication(
    authority: "http://127.0.0.1:8080/realms/realm-tp-prog4",
    audience: "payment-api"
);

// Registramos nuestras políticas de autorización.
builder.Services.AddKeycloakAuthorization();

var app = builder.Build();

// Middleware: primero autenticamos, después autorizamos.
app.UseAuthentication();
app.UseAuthorization();

// Endpoint público: no requiere JWT.
app.MapGet("/", () => "Auth.TestApi funcionando");

// Endpoint protegido: requiere el rol payments.process.
app.MapGet("/test-payment", () =>
{
    return Results.Ok("Acceso autorizado a Payment");
})
.RequireAuthorization("CanProcessPayment");

app.Run();
