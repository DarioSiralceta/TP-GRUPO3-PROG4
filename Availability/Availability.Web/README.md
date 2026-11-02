# Availability.Web

Interfaz web del TP Programación IV, desarrollada con ASP.NET Core MVC y .NET 8. Permite consultar eventos y disponibilidad de asientos, iniciar sesión con Keycloak, solicitar una reserva y consultar su resultado.

## Responsabilidad y dependencias

El MVC actúa como intermediario entre el navegador y **Availability.API**. Renderiza vistas Razor, adapta las respuestas HTTP a modelos de presentación y envía el token del usuario al solicitar reservas y consultar su seguimiento.

- **Availability.API:** proporciona catálogo, disponibilidad, recepción de solicitudes y seguimiento.
- **Keycloak:** autentica al usuario mediante OpenID Connect.
- **Booking, Payment y Azure Service Bus:** participan en el procesamiento a través del backend. Este proyecto no los invoca directamente ni consume mensajes del broker.

Availability.Web no tiene base de datos propia. La persistencia de solicitudes corresponde a Availability.API y la de reservas a Booking. El comprobante mostrado después del envío utiliza `TempData` y no reemplaza esa persistencia.

## Organización interna

| Ubicación | Responsabilidad |
| --- | --- |
| `Controllers/EventosController.cs` | Consulta y presentación del catálogo. |
| `Controllers/AsientosController.cs` | Consulta de disponibilidad por evento. |
| `Controllers/ReservasController.cs` | Formulario, envío, comprobante y seguimiento. |
| `Controllers/AccountController.cs` | Inicio de sesión, aviso de autenticación requerida y cierre de sesión. |
| `Controllers/HomeController.cs` | Inicio y páginas generales. |
| `Services/EventosService.cs` | Cliente HTTP del catálogo; convierte fechas UTC a hora local del servidor MVC. |
| `Services/AsientosService.cs` | Cliente HTTP de asientos y adaptación al modelo de vista. |
| `Services/ReservasApiService.cs` | Cliente HTTP autenticado para solicitar reservas y consultar resultados. |
| `Models/` | Contratos de respuesta de la API y modelos de las vistas. |
| `Views/` | Vistas Razor y componentes compartidos. |
| `Middleware/AuditoriaMiddleware.cs` | Registro del inicio, fin y errores de las peticiones que atraviesan el middleware. |
| `wwwroot/` | CSS, JavaScript y bibliotecas estáticas. |
| `Program.cs` | Registro de clientes HTTP, autenticación, autorización, middleware y rutas. |

La organización de este proyecto es MVC. La separación de dominio, aplicación e infraestructura de Availability está implementada en los otros proyectos del componente. El `.csproj` de Web no referencia directamente esos proyectos: la integración es HTTP.

`Services/SolicitudesService.cs` permanece de la etapa de simulación. No está registrado en `Program.cs` ni se utiliza en el controlador de reservas actual.

## Requisitos y configuración

- SDK de .NET compatible con `net8.0` (por ejemplo, SDK .NET 8).
- Certificado HTTPS de desarrollo confiable.
- Availability.API disponible y configurada con sus dependencias.
- Keycloak iniciado con el realm y cliente correspondientes.

Todos los comandos siguientes se ejecutan **desde la raíz del repositorio**, donde se encuentra `Availability.sln`.

### Dirección de Availability.API

En `appsettings.json`:

```json
{
  "AvailabilityApi": {
    "BaseUrl": "https://localhost:7004/"
  }
}
```

Conservar la barra final. Los clientes agregan rutas relativas a esta dirección y tienen un tiempo de espera de **15 segundos**. La clave también puede sobrescribirse mediante la variable de entorno `AvailabilityApi__BaseUrl`.

### Keycloak

Los siguientes valores se encuentran actualmente en `Program.cs`:

| Parámetro | Valor |
| --- | --- |
| Authority | `http://127.0.0.1:8081/realms/realm-tp-prog4` |
| ClientId | `availability-web` |
| Flujo | Authorization Code con PKCE |
| Scopes | `openid`, `profile`, `email` |
| Callback de ingreso | `/signin-oidc` |
| Callback de salida | `/signout-callback-oidc` |

Para el perfil HTTPS local, el cliente de Keycloak debe permitir el callback `https://localhost:7165/signin-oidc` y la configuración de salida correspondiente a `https://localhost:7165/signout-callback-oidc`. Estos paths los procesa el middleware OIDC; no son vistas ni acciones propias.

Si se cambia el puerto o el hostname, actualizar también la configuración del cliente en Keycloak. El uso de metadata HTTP está permitido únicamente en Development; el Authority actual es de desarrollo local y debe adaptarse para otro entorno.

Guardar el secreto del cliente mediante User Secrets, reemplazando el marcador por el valor correspondiente:

```powershell
dotnet user-secrets set "Keycloak:ClientSecret" "REEMPLAZAR_POR_EL_SECRETO" --project Availability/Availability.Web
```

El proyecto ya incluye `UserSecretsId`; no hace falta inicializarlo nuevamente. Como alternativa, puede usarse la variable de entorno `Keycloak__ClientSecret`. No publicar el secreto en el repositorio.

## Ejecución local

Preparar el certificado, restaurar dependencias e iniciar el MVC:

```powershell
dotnet dev-certs https --trust
dotnet restore Availability/Availability.Web/Availability.Web.csproj
dotnet run --project Availability/Availability.Web --launch-profile https
```

Abrir **https://localhost:7165/**. El perfil también configura `http://localhost:5089`; se recomienda usar HTTPS para mantener coherencia con los callbacks. El perfil establece el entorno `Development`.

Antes de probar el circuito, iniciar Keycloak, Availability.API y las dependencias necesarias del backend siguiendo sus propias guías. Las migraciones y los secretos de esos componentes no se administran desde Web.

Para compilar este proyecto de forma independiente:

```powershell
dotnet build Availability/Availability.Web/Availability.Web.csproj
```

Detener las instancias en ejecución con `Ctrl + C` antes de recompilar si Windows informa que un ejecutable o DLL está bloqueado.

## Rutas del MVC

La ruta convencional es `{controller=Home}/{action=Index}/{id?}`. Estas direcciones devuelven páginas o redirecciones; no son los endpoints JSON del backend.

| Método | Ruta y parámetros | Acceso y comportamiento |
| --- | --- | --- |
| GET | `/` | Inicio público. |
| GET | `/Eventos/Index` | Catálogo público. |
| GET | `/Asientos/Index?eventoId=1` | Disponibilidad pública del evento. |
| GET | `/Reservas/Solicitar?eventoId=1&seatId=2` | Requiere sesión; muestra el formulario si el asiento está disponible. |
| POST | `/Reservas/Confirmar` | Requiere sesión y antiforgery; recibe `eventoId` y `seatId` del formulario y envía la solicitud. |
| GET | `/Reservas/Recibida?solicitudId={guid}` | Requiere sesión; muestra el último comprobante disponible en el navegador. |
| GET | `/Reservas/Seguimiento?solicitudId={guid}` | Requiere sesión; sin identificador muestra el formulario de consulta. |
| GET | `/Account/RequiereLogin?returnUrl=...` | Aviso previo al login. |
| GET | `/Account/Login?returnUrl=...` | Inicia autenticación en Keycloak. Solo acepta destinos locales de retorno. |
| POST | `/Account/Logout` | Requiere sesión y antiforgery; cierra sesión en MVC y Keycloak. |

En Solicitar y Seguimiento, la comprobación de sesión redirige al aviso de login conservando el destino. Confirmar, Recibida y Logout utilizan `[Authorize]`.

**`seatId` es el identificador real del asiento.** El número visible se utiliza para presentación y no debe reemplazarlo en las llamadas a la API.

## Contratos HTTP consumidos

Las rutas de esta sección pertenecen a **Availability.API**, respecto de `AvailabilityApi:BaseUrl`.

| Método | Endpoint | Autenticación | Servicio consumidor |
| --- | --- | --- | --- |
| GET | `/events` | Público | `EventosService` |
| GET | `/events/{eventId}/seats` | Público | `AsientosService` |
| POST | `/Reservations` | Bearer del usuario | `ReservasApiService` |
| GET | `/Reservations/{requestId}` | Bearer del usuario | `ReservasApiService` |

### Catálogo y asientos

Ejemplo de los campos del catálogo utilizados por Web:

```json
[
  {
    "id": 1,
    "name": "Concierto de prueba",
    "location": "Teatro Central",
    "dateUtc": "2026-12-10T20:00:00Z"
  }
]
```

La API puede devolver campos adicionales. `ObtenerPorIdAsync` consulta el catálogo y busca el evento en esa lista; no utiliza un endpoint individual de eventos.

Ejemplo de disponibilidad:

```json
[
  { "seatId": 1, "number": 1, "isAvailable": false },
  { "seatId": 2, "number": 2, "isAvailable": true }
]
```

### Solicitud de reserva

`POST /Reservations` recibe:

```json
{
  "eventId": 1,
  "seatId": 2
}
```

El usuario se identifica mediante el token; Web no envía un `userId` elegido en el formulario. La respuesta exitosa actual del backend es **HTTP 200** y contiene `requestId` y `status`. Web consume `requestId` y exige un GUID no vacío.

**Esta respuesta confirma la recepción de la solicitud, no la reserva del asiento.** El procesamiento posterior corresponde al backend.

### Seguimiento

Ejemplo de respuesta de `GET /Reservations/{requestId}`:

```json
{
  "requestId": "211766bc-86f6-4999-9520-fe9d24fc5248",
  "eventId": 1,
  "seatId": 1,
  "status": "Confirmada",
  "rejectionReason": null
}
```

Web valida que el GUID coincida con el consultado, que los identificadores sean positivos y que el estado no esté vacío. Availability.API verifica la pertenencia al usuario y consulta a Booking.

La pantalla contempla `Pendiente`, `Retenida`, `Confirmada`, `Rechazada` y `Vencida`, y muestra el motivo de rechazo cuando existe. `Pendiente` indica que Availability tiene la solicitud pero aún no obtiene una reserva de Booking; por sí solo no garantiza que el procesamiento vaya a finalizar. El resultado se actualiza manualmente mediante **Actualizar estado**.

## Flujo y manejo de errores

1. El visitante consulta eventos y asientos sin iniciar sesión.
2. Al solicitar una reserva, se autentica y regresa al formulario seleccionado.
3. El MVC consulta nuevamente el evento y el asiento antes de enviar el POST. Esa comprobación no bloquea el asiento ni garantiza disponibilidad frente a solicitudes simultáneas.
4. Envía el POST con Bearer y `X-Correlation-ID`, recibe el GUID y redirige al comprobante mediante Post/Redirect/Get.
5. El usuario consulta el estado usando ese GUID; el MVC llama a Availability.API con su token.

Los clientes no reintentan automáticamente el POST. Ante una respuesta perdida, la solicitud podría haberse registrado y un reenvío podría crear otra solicitud. Recargar el comprobante tampoco repite el POST.

| Situación | Comportamiento del MVC |
| --- | --- |
| Error de conexión, timeout o JSON inválido en catálogo/disponibilidad | Respuesta 503 y mensaje; no inventa asientos disponibles. |
| Token ausente o respuesta 401 en operaciones autenticadas | Redirección al login, conservando un destino GET. |
| Respuesta 403 al solicitar o consultar | Mensaje de falta de permisos y estado HTTP 403. |
| Respuesta 400 al enviar una solicitud | Mensaje de datos rechazados y estado HTTP 400. |
| Respuesta 404 en seguimiento | Mensaje de solicitud inexistente y estado HTTP 404. |
| Fallo técnico al enviar o consultar seguimiento | Respuesta 503; al enviar advierte que la solicitud podría haberse registrado. |

La cookie conserva la sesión MVC y `SaveTokens` permite recuperar el access token. El token se asigna a cada petición HTTP, sin usar encabezados predeterminados compartidos entre usuarios. El código actual no implementa una renovación explícita del access token.

## Auditoría y mantenimiento

`AuditoriaMiddleware` registra `AUDITORIA INICIO`, `AUDITORIA FIN` y excepciones, con componente, correlación, método, ruta, estado HTTP y duración. Agrega `X-Correlation-ID` a la respuesta. Los archivos estáticos servidos antes del middleware no pasan por este registro.

`ReservasController` registra también envío recibido, consultas de seguimiento, accesos denegados y errores. `ReservasApiService` propaga la correlación en sus llamadas POST y GET. Los clientes de catálogo y asientos no agregan explícitamente ese encabezado. La correlación de la cadena completa debe verificarse junto con los otros servicios.

El middleware no registra cuerpos de peticiones, contraseñas ni tokens. Para diagnosticar, revisar la consola y los campos `CorrelacionId` y `RequestId`.

### Consideraciones actuales

- `TempData` conserva solo el último comprobante; si no está disponible, consultar mediante Seguimiento. Su ausencia no significa que la solicitud se haya eliminado del backend.
- Las fechas se muestran en la zona horaria del servidor MVC, no en una zona elegida por el navegador.
- La disponibilidad y los estados se consultan al cargar o actualizar las páginas; no hay actualización automática en tiempo real.
- La autenticación entre backend y otros microservicios, el broker y el cobro gRPC deben documentarse y verificarse en sus propios componentes.

## Verificación manual sugerida

- Consultar eventos y asientos sin sesión.
- Elegir un asiento, iniciar sesión y comprobar el regreso al formulario.
- Enviar una solicitud y comprobar que recargar el comprobante no repita el envío.
- Consultar el GUID recibido y actualizar hasta obtener el resultado del backend.
- Comprobar que un asiento confirmado aparezca ocupado al actualizar el listado.
- Consultar una solicitud ajena, una inexistente y un identificador inválido.
- Interrumpir Availability.API y comprobar los mensajes de indisponibilidad.
- Cerrar sesión y comprobar que las operaciones protegidas requieran autenticación.

Esta lista es una guía de validación; no implica que todos los escenarios hayan sido probados.
