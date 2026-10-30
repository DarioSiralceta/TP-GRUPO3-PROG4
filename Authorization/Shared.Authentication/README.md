# Shared.Authentication — TP Programación IV

## 1. Descripción

Biblioteca compartida de autenticación y autorización para los microservicios del TP de reserva de asientos.

Utiliza **Keycloak** como servidor de identidad y **JWT Bearer** para validar los tokens de acceso.

Desarrollada en **C# / .NET 8**.

### Funcionalidades implementadas

- Validación de JWT emitidos por Keycloak.
- Validación de emisor, audiencia, vencimiento y firma.
- Políticas de autorización basadas en roles de Keycloak.
- Obtención de access tokens mediante OAuth2 Client Credentials.
- Configuración reutilizable mediante `IConfiguration`.

## 2. Configuración de Keycloak

**Realm:** `realm-tp-prog4`

| Cliente | Audiencia | Rol |
|---|---|---|
| `booking-service` | `payment-api` | `payments.process` |
| `availability-service` | `booking-api` | `booking.read` |
| `availability-web` | Pendiente de integración | Login de usuarios con Google |

Las dos primeras configuraciones se probaron correctamente desde Keycloak y PowerShell.

El proveedor de identidad Google también se configuró y se probó mediante la Account Console de Keycloak.

## 3. Integración en las APIs .NET 8

### 3.1. Agregar la referencia al proyecto

Desde Visual Studio:

1. Clic derecho sobre **Dependencies** del proyecto que necesita autenticación.
2. Seleccionar **Add Project Reference**.
3. Marcar `Shared.Authentication`.
4. Confirmar.

### 3.2. Configurar appsettings.Development.json

Ejemplo para Payment:

```json
{
  "Keycloak": {
    "Authority": "http://127.0.0.1:8080/realms/realm-tp-prog4",
    "Audience": "payment-api"
  }
}
```

Para Booking se utiliza `booking-api` como audiencia.

La URL `127.0.0.1` corresponde al entorno local de desarrollo; debe adaptarse cuando los servicios se ejecuten en contenedores.

### 3.3. Registrar la autenticación y autorización

En `Program.cs`:

```csharp
using Shared.Authentication.Extensions;

builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAuthorization();
```

Después de crear la aplicación:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Estos middlewares deben ejecutarse antes de los endpoints protegidos.

### 3.4. Proteger endpoints

Ejemplo de Minimal API para Payment:

```csharp
app.MapPost("/payments", () =>
{
    return Results.Ok();
})
.RequireAuthorization("CanProcessPayment");
```

Esta política exige un JWT válido con el rol `payments.process`.

Para consultas protegidas de Booking:

```csharp
app.MapGet("/bookings/{id}/status", (string id) =>
{
    return Results.Ok();
})
.RequireAuthorization("CanReadBooking");
```

La política exige el rol `booking.read`.

Los ejemplos ilustran únicamente la autorización; no implementan la lógica de negocio.

## 4. Obtención de tokens mediante Client Credentials

La biblioteca contiene `KeycloakTokenService`, que permite solicitar un access token desde C#.

Registrar en `Program.cs`:

```csharp
using Shared.Authentication.Services;

builder.Services.AddHttpClient<KeycloakTokenService>();
```

Ejemplo de uso desde un servicio:

```csharp
string jwt = await tokenService.GetAccessTokenAsync(
    authority,
    clientId,
    clientSecret
);
```

**Importante:** no escribir Client Secrets directamente en el código ni subirlos al repositorio. Utilizar User Secrets para desarrollo o variables de entorno.

Para llamadas gRPC, el token debe enviarse en los metadatos de autorización como `Bearer`.

## 5. Responsabilidades de los componentes

| Componente | Integración requerida |
|---|---|
| Frontend MVC | OpenID Connect con Keycloak, login con Google y sesión mediante cookies |
| Availability API | Validación de JWT de usuarios y Client Credentials para consultar Booking |
| Booking API | Validación de tokens de servicio de Availability y obtención de JWT para llamar a Payment |
| Payment gRPC API | Validación de JWT con audiencia `payment-api` y rol `payments.process` |

## 6. Pruebas realizadas

- Solicitud sin token: `401 Unauthorized`.
- Token con audiencia incorrecta: `401 Unauthorized`.
- Token válido de `booking-service` con permiso correcto: `200 OK`.
- Obtención automática de JWT desde `KeycloakTokenService`.
- Integración HTTP simulada: C# obtiene un token y accede correctamente a un endpoint protegido.

**Pruebas pendientes:** `403 Forbidden` por ausencia de rol, integración real con gRPC, login desde MVC y despliegue compartido.

## 7. Consideraciones

- La configuración actual de Keycloak con HTTP se utiliza únicamente para desarrollo local.
- Para producción debe utilizarse HTTPS y reforzarse la validación y gestión de credenciales.
- `Auth.TestApi` es una aplicación de pruebas, no un microservicio de producción.
- Las rutas de prueba que solicitan tokens no deben exponerse públicamente.
- La integración de JWT en las APIs reales todavía está pendiente.
- La configuración de Keycloak deberá exportarse o automatizarse para que el resto del equipo pueda reproducirla.