
using Shared.Authentication.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configuramos la validación de JWT de Keycloak.
builder.Services.AddKeycloakAuthentication(builder.Configuration);

// Registramos las políticas de autorización.
builder.Services.AddKeycloakAuthorization();

var app = builder.Build();

// Middleware de autenticación y autorización.
app.UseAuthentication();
app.UseAuthorization();

// Endpoint público para comprobar que la API funciona.
app.MapGet("/", () => "Auth.TestApi funcionando");

// Endpoint protegido que simula Payment.
app.MapGet("/test-payment", () =>
{
    return Results.Ok("Acceso autorizado a Payment");
})
.RequireAuthorization("CanProcessPayment");




/* Eliminamos estos endpoints pq orque utilizan credenciales de servicio y no tienen protección de acceso. Aunque no devuelven el token, no queremos dejar esas operaciones expuestas.

app.MapGet("/test-client-credentials", async (
    KeycloakTokenService tokenService,
    IConfiguration configuration) =>
{
    // Leemos las credenciales desde User Secrets.
    string authority = configuration["Keycloak:Authority"]!;
    string clientId = configuration["Keycloak:ClientId"]!;
    string clientSecret = configuration["Keycloak:ClientSecret"]!;

    // Solicitamos un JWT directamente a Keycloak.
    string jwt = await tokenService.GetAccessTokenAsync(
        authority,
        clientId,
        clientSecret
    );

    // No devolvemos el JWT, solamente confirmamos que se obtuvo.
    return Results.Ok(new
    {
        message = "JWT obtenido correctamente desde C#",
        client = clientId,
        tokenReceived = !string.IsNullOrEmpty(jwt)
    });
});


app.MapGet("/test-integration", async (
    KeycloakTokenService tokenService,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) =>
{
    // 1. Leemos las credenciales locales.
    string authority = configuration["Keycloak:Authority"]!;
    string clientId = configuration["Keycloak:ClientId"]!;
    string clientSecret = configuration["Keycloak:ClientSecret"]!;

    // 2. Solicitamos un JWT a Keycloak.
    string jwt = await tokenService.GetAccessTokenAsync(
        authority,
        clientId,
        clientSecret
    );

    // 3. Creamos un cliente HTTP para llamar al endpoint protegido.
    var httpClient = httpClientFactory.CreateClient();

    using var request = new HttpRequestMessage(
        HttpMethod.Get,
        "https://localhost:7102/test-payment"
    );

    // 4. Enviamos el JWT en el encabezado Authorization.
    request.Headers.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            jwt
        );

    // 5. Llamamos a nuestro endpoint protegido.
    using var response = await httpClient.SendAsync(request);

    return Results.Ok(new
    {
        tokenObtained = true,
        paymentStatusCode = (int)response.StatusCode,
        paymentAuthorized = response.IsSuccessStatusCode
    });
});
*/


app.Run();
