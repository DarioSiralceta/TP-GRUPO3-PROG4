# Shared.Authentication — TP Programación IV

## 1. Descripción

`Shared.Authentication` es una biblioteca compartida desarrollada en **C# / .NET 8** que centraliza la autenticación y autorización de los microservicios del sistema de reservas.

Utiliza **Keycloak** como servidor de identidad y **JWT Bearer** para proteger las comunicaciones entre las APIs.

### Funcionalidades implementadas

- Validación de tokens JWT emitidos por Keycloak.
- Validación de firma, emisor, audiencia y vencimiento.
- Políticas de autorización basadas en roles.
- Obtención de access tokens mediante OAuth2 Client Credentials.
- Configuración reutilizable mediante `IConfiguration`.
- Integración con Availability, Booking y Payment.

## 2. Configuración de Keycloak

**Realm:** `realm-tp-prog4`

**Authority local:**

```text
http://127.0.0.1:8081/realms/realm-tp-prog4
```

### Clientes y permisos

| Cliente | Audiencia del token | Rol / Función |
|---|---|---|
| `availability-web` | `availability-api` | Autenticación de usuarios desde MVC |
| `availability-service` | `booking-api` | `booking.read` |
| `booking-service` | `payment-api` | `payments.process` |

El frontend MVC utiliza OpenID Connect y cookies para autenticar usuarios mediante Keycloak y Google.

Las APIs utilizan JWT Bearer para validar los access tokens recibidos.

## 3. Integración en las APIs

### 3.1. Agregar la referencia al proyecto

Desde Visual Studio Community:

1. Clic derecho sobre **Dependencies** del proyecto.
2. Seleccionar **Add Project Reference**.
3. Marcar `Shared.Authentication`.
4. Confirmar.

### 3.2. Configurar la autenticación

Cada API debe definir los valores de `Authority` y `Audience` en su configuración.

Ejemplo para Payment:

```json
{
  "Keycloak": {
    "Authority": "http://127.0.0.1:8081/realms/realm-tp-prog4",
    "Audience": "payment-api"
  }
}
```

Las audiencias utilizadas son:

| API receptora | Audience |
|---|---|
| Availability.API | `availability-api` |
| Booking.API | `booking-api` |
| Payment.API | `payment-api` |

**Importante:** la dirección del servidor Keycloak debe ser compatible con el emisor (`iss`) del JWT. Para la validación del emisor, `localhost` y `127.0.0.1` no son valores equivalentes.

### 3.3. Registrar autenticación y autorización

En `Program.cs`:

```csharp
using Shared.Authentication.Extensions;

builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAuthorization();
```

En el pipeline de ASP.NET Core:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Estos middlewares deben ejecutarse antes de los endpoints protegidos.

## 4. Validación de JWT

La extensión `AddKeycloakAuthentication` configura el esquema de autenticación JWT Bearer.

```csharp
options.Authority = authority;
options.Audience = audience;

options.TokenValidationParameters =
    new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
```

### Validaciones realizadas

- **Issuer (`iss`):** comprueba que el token provenga del servidor de identidad esperado.
- **Audience (`aud`):** verifica que el token esté destinado a la API receptora.
- **Lifetime (`exp`):** comprueba que el token no esté vencido.
- **Firma:** valida la autenticidad e integridad del token.

La API obtiene la información pública de Keycloak necesaria para validar los JWT.

## 5. Políticas de autorización

La biblioteca incluye políticas reutilizables para controlar el acceso a operaciones protegidas.

### CanReadBooking

Utilizada para las consultas protegidas de Booking.

Requiere un token válido con el permiso `booking.read`.

Flujo:

1. Availability solicita un token mediante `availability-service`.
2. Keycloak emite un JWT con audiencia `booking-api`.
3. Availability envía el token a Booking.
4. Booking valida el JWT y el permiso correspondiente.

### CanProcessPayment

Utilizada para proteger Payment.API.

Requiere un token válido con el rol `payments.process`.

Flujo:

1. Booking obtiene un token mediante `booking-service`.
2. Keycloak emite un JWT con audiencia `payment-api`.
3. Booking adjunta el token a la llamada gRPC.
4. Payment valida el JWT y aplica la política `CanProcessPayment`.

Ejemplo de protección del servicio gRPC:

```csharp
app.MapGrpcService<PaymentGrpcService>()
   .RequireAuthorization("CanProcessPayment");
```

La autenticación verifica la identidad del solicitante y la autorización determina si tiene permiso para realizar la operación.

## 6. Obtención de tokens mediante Client Credentials

La biblioteca incluye `KeycloakTokenService`, que permite obtener tokens de acceso mediante OAuth2 Client Credentials.

Este flujo se utiliza para las comunicaciones **servicio a servicio**, sin intervención del usuario final.

### Registrar el servicio

En `Program.cs`:

```csharp
using Shared.Authentication.Services;

builder.Services.AddHttpClient<KeycloakTokenService>();
```

### Solicitar un access token

```csharp
string jwt = await tokenService.GetAccessTokenAsync(
    authority,
    clientId,
    clientSecret
);
```

El método realiza una solicitud al endpoint de tokens de Keycloak:

```text
/realms/realm-tp-prog4/protocol/openid-connect/token
```

Utilizando:

```text
grant_type=client_credentials
```

Keycloak valida las credenciales del cliente y devuelve un access token.

### Envío del token

En comunicaciones HTTP REST:

```text
Authorization: Bearer <JWT>
```

En comunicaciones gRPC, el token se transmite mediante los metadatos de autorización:

```text
authorization: Bearer <JWT>
```

Los Client Secrets deben almacenarse mediante User Secrets o variables de entorno, nunca directamente en el código fuente.

## 7. Integración con los componentes

| Componente | Responsabilidad |
|---|---|
| Availability.Web | Autenticación de usuarios con Keycloak y Google mediante OpenID Connect y cookies |
| Availability.API | Validar JWT del usuario y obtener tokens para consultar Booking |
| Booking.API | Validar tokens provenientes de Availability y obtener tokens para llamar a Payment |
| Payment.API | Validar tokens provenientes de Booking y aplicar `CanProcessPayment` |
| Shared.Authentication | Centralizar validación JWT, políticas y obtención de tokens |

### Flujo general de autenticación

```text
Usuario
   |
   | OpenID Connect / Google
   v
Keycloak
   |
   | Access Token del usuario
   v
Availability.Web
   |
   | JWT - aud: availability-api
   v
Availability.API
   |
   | Client Credentials
   | availability-service
   | JWT - aud: booking-api
   v
Booking.API
   |
   | Client Credentials
   | booking-service
   | JWT - aud: payment-api
   | Rol: payments.process
   v
Payment.API (gRPC)
```

Cada API valida los tokens que recibe y aplica las políticas correspondientes.

## 8. Pruebas y verificaciones

Se realizaron pruebas de:

- Obtención de tokens mediante Client Credentials.
- Validación de JWT con audiencia correcta.
- Rechazo de solicitudes sin token (`401 Unauthorized`).
- Rechazo de tokens con audiencia incorrecta (`401 Unauthorized`).
- Acceso a endpoints protegidos con tokens válidos.
- Configuración de roles y audiencias en Keycloak.
- Inicio de sesión con Google desde Availability.Web.

La integración de autenticación entre Availability, Booking y Payment está incorporada al código del proyecto.

**Validación pendiente en el entorno local utilizado para las últimas pruebas:** completar el circuito funcional de reservas y verificar su procesamiento hasta Payment. El POST de reserva desde MVC presentó una respuesta `401 Unauthorized`, cuyo origen todavía no se determinó.

## 9. Consideraciones de seguridad

- Los Client Secrets no deben almacenarse en archivos versionados.
- Los access tokens deben validarse antes de autorizar operaciones protegidas.
- Cada API debe comprobar la audiencia correspondiente.
- Los roles permiten aplicar el principio de mínimo privilegio.
- El uso de HTTP para Keycloak está destinado exclusivamente al desarrollo local.
- En producción se requiere HTTPS y una gestión segura de las credenciales.
- `Auth.TestApi` se utiliza para pruebas de autenticación y no forma parte del circuito principal de reservas.

---

Para la instalación, configuración del realm, clientes, roles y proveedor Google, consultar `Authorization/Keycloak/README.md`.