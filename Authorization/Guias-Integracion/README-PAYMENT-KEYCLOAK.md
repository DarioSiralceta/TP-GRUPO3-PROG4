# Integración de Keycloak con Payment API (gRPC)

## 1. Objetivo

Integrar autenticación y autorización en `Payment.API` utilizando Keycloak y la librería compartida `Shared.Authentication`.

**Resultado esperado:** solamente el servicio Booking, autenticado con un JWT válido que incluya el rol `payments.process`, podrá invocar las operaciones de pago protegidas de Payment.

### Arquitectura

```text
Booking API
    |
    | Solicita token con client_credentials
    v
Keycloak
    |
    | Emite JWT
    | aud: payment-api
    | rol: payments.process
    v
Booking API
    |
    | gRPC ProcessPayment
    | Authorization: Bearer <JWT>
    v
Payment API
    |
    | Valida firma, emisor, audiencia,
    | vencimiento y rol
    v
PaymentService (lógica de negocio)
```

## 2. Configuración existente en Keycloak

La configuración automatizada de Keycloak ya incluye lo necesario para Payment.

| Elemento | Valor |
|---|---|
| Realm | `realm-tp-prog4` |
| Cliente emisor | `booking-service` |
| Audiencia de Payment | `payment-api` |
| Rol requerido | `payments.process` |
| Política de autorización | `CanProcessPayment` |

Booking obtiene el token mediante el flujo `client_credentials`.

Payment **no necesita conocer el Client Secret de Booking**. Solamente valida los JWT recibidos utilizando la configuración pública de Keycloak.

Los Client Secrets deben permanecer en el servicio que solicita los tokens y nunca incluirse en código fuente ni en el repositorio.

## 3. Estructura del proyecto

En la rama `feature/gRPC` se encuentran estos archivos:

```text
Payment/
└── Payment.API/
    ├── Program.cs
    ├── Payment.API.csproj
    ├── appsettings.json
    ├── Protos/
    │   └── payment.proto
    └── Services/
        └── PaymentGrpcService.cs

Authorization/
└── Shared.Authentication/
    ├── Shared.Authentication.csproj
    └── Extensions/
        ├── KeyCloakAuthenticationExtensions.cs
        └── KeycloakAuthorizationExtensions.cs
```

La integración se hará principalmente en los archivos de `Payment.API`.

## 4. Paso 1 — Agregar referencia a Shared.Authentication

Abrir:

`Payment/Payment.API/Payment.API.csproj`

Dentro del grupo de referencias a proyectos, incorporar:

```xml
<ProjectReference Include="..\..\Authorization\Shared.Authentication\Shared.Authentication.csproj" />
```

Ejemplo:

```xml
<ItemGroup>
  <ProjectReference Include="..\Payment.BLL\Payment.BLL.csproj" />
  <ProjectReference Include="..\..\Authorization\Shared.Authentication\Shared.Authentication.csproj" />
</ItemGroup>
```

**Importante:** mantener las dependencias y referencias existentes del proyecto. No reemplazar el archivo completo.

La librería contiene:

- `AddKeycloakAuthentication`: registra la validación JWT.
- `AddKeycloakAuthorization`: registra las políticas de autorización.
- `KeycloakTokenService`: permite solicitar tokens a Keycloak; se utiliza en Booking, no es necesario para validar tokens en Payment.

## 5. Paso 2 — Configurar Keycloak en appsettings.json

Abrir:

`Payment/Payment.API/appsettings.json`

Agregar la sección `Keycloak`, conservando las secciones existentes:

```json
{
  "Keycloak": {
    "Authority": "http://127.0.0.1:8080/realms/realm-tp-prog4",
    "Audience": "payment-api"
  }
}
```

### Significado de los valores

**Authority:** dirección del Realm que emite los JWT. Payment obtiene desde allí la información necesaria para verificar los tokens.

**Audience:** identifica al servicio receptor del token. Payment solamente debe aceptar JWT destinados a `payment-api`.

La dirección `127.0.0.1:8080` corresponde a una instalación local donde Keycloak utiliza ese puerto. Si el entorno utiliza `8081` u otra URL, modificar `Authority` para que coincida.

Si Payment se ejecuta dentro de un contenedor, `127.0.0.1` apunta al propio contenedor, no al de Keycloak. En ese caso, será necesario configurar una dirección accesible desde Payment y que coincida con el `issuer` del token.

La configuración HTTP se utiliza solamente para desarrollo local. En producción corresponde utilizar HTTPS.

## 6. Paso 3 — Registrar autenticación y autorización en Program.cs

Abrir:

`Payment/Payment.API/Program.cs`

Agregar:

```csharp
using Shared.Authentication.Extensions;
```

Después de crear `builder`, incorporar:

```csharp
builder.Services.AddKeycloakAuthentication(builder.Configuration);

builder.Services.AddKeycloakAuthorization();
```

### Ejemplo completo basado en el estado actual de la rama

```csharp
using Payment.API.Services;
using Payment.BLL.Interfaces;
using Payment.BLL.Services;
using Shared.Authentication.Extensions;

var builder = WebApplication.CreateBuilder(args);

// gRPC
builder.Services.AddGrpc();

// Autenticación y autorización con Keycloak
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAuthorization();

// Servicios de pago
builder.Services.AddSingleton<MockPaymentProvider>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Mantener aquí los registros adicionales
// de DbContext, repositorios y servicios
// que incorpore el equipo de Payment.

var app = builder.Build();

// El orden es importante
app.UseAuthentication();
app.UseAuthorization();

// Endpoints gRPC
app.MapGrpcService<PaymentGrpcService>();

app.Run();
```

**No duplicar** llamadas a `AddAuthentication`, `AddAuthorization`, `UseAuthentication` ni `UseAuthorization` si ya existen en una versión posterior.

La librería valida:

- Firma del JWT.
- Emisor (`issuer`).
- Audiencia (`aud`).
- Fecha de expiración.
- Autenticación del solicitante.

La política `CanProcessPayment` comprueba adicionalmente el rol `payments.process` del token.

## 7. Paso 4 — Proteger PaymentGrpcService

Abrir:

`Payment/Payment.API/Services/PaymentGrpcService.cs`

Actualmente la clase hereda de:

```csharp
Protos.PaymentService.PaymentServiceBase
```

El archivo `payment.proto` declara la operación:

```protobuf
rpc ProcessPayment (ProcessPaymentRequest)
    returns (ProcessPaymentResponse);
```

Para proteger el servicio gRPC, agregar el atributo `Authorize` con la política correspondiente.

```csharp
using Microsoft.AspNetCore.Authorization;
using Payment.API.Protos;

namespace Payment.API.Services;

[Authorize(Policy = "CanProcessPayment")]
public class PaymentGrpcService : Protos.PaymentService.PaymentServiceBase
{
    // El equipo Payment implementa aquí ProcessPayment
    // y la lógica de negocio correspondiente.
}
```

Con este atributo, todas las operaciones gRPC de esta clase quedan protegidas por la política `CanProcessPayment`.

**Importante:** este fragmento muestra únicamente el cambio de autorización. No reemplazar ni eliminar la implementación de `ProcessPayment` que esté desarrollando el equipo.

También es posible aplicar el atributo directamente al método si el servicio incorpora en el futuro operaciones públicas y privadas diferentes.

## 8. Paso 5 — Verificar el contrato gRPC

El archivo actual:

`Payment/Payment.API/Protos/payment.proto`

Define:

```protobuf
service PaymentService {
  rpc ProcessPayment (ProcessPaymentRequest)
      returns (ProcessPaymentResponse);
}
```

No hace falta modificar el `.proto` para agregar autenticación JWT.

En gRPC, Booking enviará el token como metadato de la llamada:

```text
authorization: Bearer <JWT>
```

La autenticación se procesa mediante el middleware de ASP.NET Core y la autorización mediante la política aplicada al servicio o método gRPC.

### Responsabilidad de Booking

Booking debe:

1. Solicitar un JWT a Keycloak utilizando `booking-service`.
2. Enviar ese token en los metadatos de `ProcessPayment`.
3. Gestionar los errores de autenticación y autorización devueltos por Payment.

Para solicitar el token puede reutilizar `KeycloakTokenService`, disponible en `Shared.Authentication`.

**Payment no debe solicitar un token a Keycloak para autorizar esta llamada entrante.** Su responsabilidad es validar el token enviado por Booking.

## 9. Paso 6 — Ejecutar Keycloak y Payment

### 9.1. Levantar Keycloak

Seguir las instrucciones de:

`Authorization/Keycloak/README.md`

Si se utiliza el puerto local 8080, la Authority será:

```text
http://127.0.0.1:8080/realms/realm-tp-prog4
```

Comprobar que el Realm se importó correctamente.

### 9.2. Ejecutar Payment.API

Desde Visual Studio, iniciar `Payment.API` con el perfil que corresponda.

Verificar que la aplicación inicia sin errores de configuración de JWT.

El servicio gRPC debe estar disponible en el puerto indicado por su configuración de ejecución.

Para las pruebas gRPC se recomienda usar HTTPS con HTTP/2, respetando el puerto y las opciones configuradas por el equipo Payment.

### 9.3. Comprobar el token de Booking

Booking deberá obtener un token mediante `client_credentials`.

El token debe incluir:

```json
{
  "aud": ["payment-api"],
  "realm_access": {
    "roles": ["payments.process"]
  }
}
```

Este JSON es ilustrativo: el token real también tendrá otros claims y puede incluir otras audiencias o roles.

No registrar tokens completos ni Client Secrets en los logs.

## 10. Paso 7 — Pruebas de seguridad

Una vez que el equipo implemente `ProcessPayment`, verificar los siguientes escenarios:

| Escenario | Resultado esperado |
|---|---|
| Llamada gRPC sin JWT | `Unauthenticated` |
| JWT inválido o vencido | `Unauthenticated` |
| JWT válido con audiencia incorrecta | `Unauthenticated` |
| JWT válido para Payment, pero sin rol `payments.process` | `PermissionDenied` |
| JWT válido con audiencia y rol correctos | Se ejecuta `ProcessPayment` |

Los códigos indicados son estados gRPC. Su representación HTTP puede diferir de la que aparece en una API REST.

### Herramientas de prueba

Se puede utilizar:

- Postman con soporte gRPC.
- Un cliente gRPC .NET.
- `grpcurl`, si se configura reflection o se proporciona el contrato `.proto`.

En Postman, para una llamada autorizada:

1. Crear una solicitud gRPC hacia la dirección de Payment.
2. Seleccionar `payment.PaymentService/ProcessPayment`.
3. Agregar el metadato `authorization` con valor `Bearer <JWT>`.
4. Enviar una petición válida definida por `payment.proto`.
5. Comprobar la respuesta o el estado gRPC.

Repetir la prueba sin el metadato para comprobar el rechazo de solicitudes anónimas.

## 11. Responsabilidades de cada equipo

### Equipo Payment — Gabriel y Fede

- Referenciar `Shared.Authentication`.
- Configurar `Authority` y `Audience`.
- Registrar middleware de autenticación y autorización.
- Proteger `PaymentGrpcService` con `CanProcessPayment`.
- Implementar la lógica real de `ProcessPayment`.
- Verificar que el servicio rechaza llamadas no autorizadas.

### Equipo Booking

- Configurar `booking-service`.
- Obtener JWT con `KeycloakTokenService`.
- Enviar el token Bearer en la llamada gRPC.
- Manejar correctamente los errores de Payment.

### Equipo Authentication

- Mantener la configuración de Keycloak.
- Mantener las políticas y librería compartida.
- Revisar que la integración respete la arquitectura definida.
- Coordinar las pruebas de JWT, audiencias y roles.

## 12. Criterios para dar por terminada la integración

- [ ] `Payment.API` referencia `Shared.Authentication`.
- [ ] Se configura la audiencia `payment-api`.
- [ ] Se registra la autenticación y autorización en `Program.cs`.
- [ ] `ProcessPayment` está protegido mediante `CanProcessPayment`.
- [ ] Una llamada sin token es rechazada.
- [ ] Un token sin permisos es rechazado.
- [ ] Un token válido de Booking permite llegar al procesamiento.
- [ ] Se verifica el funcionamiento mediante una llamada gRPC real.

**La integración se considera completa cuando la autorización funciona correctamente en ejecución, no solamente cuando el código compila.**