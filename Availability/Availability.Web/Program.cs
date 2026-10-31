using Availability.Web.Middleware;
using Availability.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<AsientosService>();

// HttpClient administra las llamadas HTTP a Availability.API.
// La dirección se configura fuera del servicio para poder cambiar de entorno.
builder.Services.AddHttpClient<EventosService>(client =>
{
    var baseUrl = builder.Configuration["AvailabilityApi:BaseUrl"]
        ?? throw new InvalidOperationException(
            "Falta configurar AvailabilityApi:BaseUrl.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
});

// Conserva las solicitudes simuladas entre peticiones.
// Se pierden al reiniciar la aplicación.
builder.Services.AddSingleton<SolicitudesService>();

// Keycloak realiza la autenticación mediante OpenID Connect.
// Las cookies mantienen la sesión dentro del MVC.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.Authority =
            "http://127.0.0.1:8081/realms/realm-tp-prog4";

        options.ClientId = "availability-web";

        // Se obtiene desde User Secrets o variables de entorno.
        options.ClientSecret =
            builder.Configuration["Keycloak:ClientSecret"];

        // Authorization Code + PKCE evita implementar
        // manualmente el intercambio de credenciales y tokens.
        options.ResponseType = "code";
        options.UsePkce = true;

        // Conservamos el access token para la futura llamada a la API.
        options.SaveTokens = true;

        // Permitimos metadata HTTP únicamente en desarrollo local.
        options.RequireHttpsMetadata =
            !builder.Environment.IsDevelopment();

        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";

        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Audita las peticiones antes de autenticar y ejecutar los controladores.
app.UseMiddleware<AuditoriaMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();