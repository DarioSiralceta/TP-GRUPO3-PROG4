# Guía de integración: login MVC con Keycloak y Google

**Proyecto:** `Availability/Availability.Web` (.NET 8)  
**Responsable de integración:** equipo de MVC (Thiago)  
**Objetivo de la primera prueba:** iniciar sesión desde MVC → Keycloak → Google → volver al MVC autenticado.

> Esta guía es para el **frontend ASP.NET Core MVC**. No hay que registrar JWT Bearer de `Shared.Authentication` como esquema de login del MVC: aquí se usan **OpenID Connect + cookies**. La biblioteca compartida se usará para validar JWT en las APIs.

## 1. Datos de Keycloak

| Dato | Valor de desarrollo |
|---|---|
| Realm | `realm-tp-prog4` |
| Authority | `http://127.0.0.1:8080/realms/realm-tp-prog4` |
| Client ID | `availability-web` |
| Callback | `https://localhost:7165/signin-oidc` |
| Logout callback | `https://localhost:7165/signout-callback-oidc` |
| Flujo | Authorization Code + PKCE (`S256`) |
| Audience del access token | `availability-api` |

**Atención:** el proveedor Google está configurado en el Keycloak original. `127.0.0.1` apunta a **la máquina donde corre MVC**: si Thiago ejecuta MVC en su propia PC, esta URL **no accede** al Keycloak de otra persona. Necesita una instancia de Keycloak accesible desde su PC y con Google configurado, o hacer la primera prueba en la misma máquina donde corre ese Keycloak. El Docker Compose compartido aún no incluye credenciales de Google.

## 2. Instalar paquete NuGet

En el proyecto `Availability.Web`, instalar la versión **8.x** de:

```
Microsoft.AspNetCore.Authentication.OpenIdConnect
```

Desde la terminal, ubicada en la raíz del repositorio:

```powershell
dotnet add Availability/Availability.Web/Availability.Web.csproj package Microsoft.AspNetCore.Authentication.OpenIdConnect --version 8.0.21
```

También se puede instalar desde **Manage NuGet Packages** en Visual Studio, eligiendo una versión 8.x compatible.

## 3. Guardar el secreto de manera privada

En Keycloak → Clients → `availability-web` → **Credentials**, obtener el `Client Secret`.

En Visual Studio: clic derecho en `Availability.Web` → **Manage User Secrets**. Guardar (con el valor real, sin subirlo a Git):

```json
{
  "Keycloak": {
    "ClientSecret": "REEMPLAZAR_LOCALMENTE"
  }
}
```

No publicar el secreto en repositorio, chat grupal ni capturas.

## 4. Configurar `Program.cs`

Agregar estos `using` al inicio de `Availability.Web/Program.cs`:

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
```

**Antes de** `var app = builder.Build();` agregar:

```csharp
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.Authority = "http://127.0.0.1:8080/realms/realm-tp-prog4";
        options.ClientId = "availability-web";
        options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"]
            ?? throw new InvalidOperationException("Falta Keycloak:ClientSecret");
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true; // Inicialmente, para facilitar la integración MVC → API
        options.RequireHttpsMetadata = false; // SOLO Keycloak local por HTTP
        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
    });

builder.Services.AddAuthorization();
```

En el pipeline, **después de** `app.UseRouting()` y **antes de** mapear los controladores, dejar en este orden:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Conservar los servicios MVC, `AuditoriaMiddleware`, rutas y demás configuración existente. **No reemplazar `Program.cs` entero.**

## 5. Crear `AccountController.cs`

En `Availability.Web/Controllers/AccountController.cs`:

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace Availability.Web.Controllers;

public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return Challenge(
            new AuthenticationProperties { RedirectUri = "/" },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}
```

Para iniciar sesión, navegar a `/Account/Login`. Para cerrar sesión, utilizar un **formulario POST con antiforgery**, no un enlace GET.

## 6. Proteger la reserva, no la consulta pública

En `ReservasController`, incorporar `using Microsoft.AspNetCore.Authorization;` y agregar `[Authorize]` a la acción `Confirmar` (POST). Si quieren exigir login antes de mostrar el formulario, pueden proteger también `Solicitar` (GET). Mantener públicas las páginas de consulta de eventos y asientos.

## 7. Primera prueba (sin integrar todavía Availability API)

1. Ejecutar MVC en el perfil **HTTPS** (`https://localhost:7165`).
2. Abrir `https://localhost:7165/Account/Login`.
3. Confirmar redirección a Keycloak, iniciar sesión con Google y volver al MVC.
4. Confirmar que `User.Identity?.IsAuthenticated == true` en el servidor.
5. Probar que la acción protegida redirige a login cuando no hay sesión.

Si el login de Google no aparece, revisar que el proveedor esté configurado en **esa instancia** de Keycloak. Si hay error de `redirect_uri`, comparar URL y puerto con la configuración del cliente.

## 8. Siguiente integración: enviar access token a Availability API

Cuando funcione el login, desde una acción/servicio MVC **del lado del servidor**:

```csharp
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;

string? accessToken = await HttpContext.GetTokenAsync("access_token");
if (string.IsNullOrEmpty(accessToken))
{
    return Challenge();
}

using var request = new HttpRequestMessage(HttpMethod.Post, urlDeAvailability);
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
// Agregar JSON/DTO requerido por Availability y enviar mediante HttpClient.
```

`urlDeAvailability` es un **marcador de posición**, a reemplazar por la URL y ruta real definidas por el equipo de Availability. Nunca incluir el token en HTML o JavaScript del navegador.

Availability API validará el JWT con:

```json
{
  "Keycloak": {
    "Authority": "http://127.0.0.1:8080/realms/realm-tp-prog4",
    "Audience": "availability-api"
  }
}
```

La API debe exigir autenticación en el endpoint de reserva y obtener el identificador del usuario desde el `sub` **validado** del token, no de un `userId` confiado al cliente.

## 9. Qué queda pendiente

- Probar login MVC + Google realmente integrado (no solo desde la consola de Keycloak).
- Conectar la solicitud real a Availability API y revisar el contrato HTTP.
- Considerar renovación del access token, tamaño de cookies y manejo de expiración (por ahora `SaveTokens = true` simplifica la prueba local).
- Usar HTTPS y URLs/credenciales adecuadas en despliegues reales. `RequireHttpsMetadata = false` **solo** para el Keycloak local de desarrollo.
