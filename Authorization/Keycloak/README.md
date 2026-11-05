# Keycloak — Autenticación y autorización

## Objetivo

Implementar un servidor de autenticación y autorización mediante **Keycloak 26.4.6**, utilizando OAuth2, OpenID Connect y JWT para proteger el sistema distribuido de reservas.

Keycloak se ejecuta en Docker y utiliza el realm `realm-tp-prog4`.

### Configuración implementada

- Autenticación de usuarios mediante Keycloak y Google.
- Inicio de sesión del frontend MVC con OpenID Connect, Authorization Code y PKCE.
- Autenticación entre microservicios mediante Client Credentials.
- Emisión y validación de tokens JWT.
- Control de acceso mediante roles y audiencias.

## 1. Clientes configurados

| Cliente | Función |
|---|---|
| `availability-web` | Login del usuario desde el frontend MVC |
| `availability-service` | Obtener tokens para comunicarse con Booking |
| `booking-service` | Obtener tokens para comunicarse con Payment |
| `payment-api` | Identificar la API receptora de pagos |

Las APIs validan los tokens destinados a sus respectivas audiencias:

| API | Audiencia |
|---|---|
| Availability | `availability-api` |
| Booking | `booking-api` |
| Payment | `payment-api` |

### Roles y permisos

- `booking.read`: autoriza las consultas protegidas a Booking.
- `payments.process`: autoriza el procesamiento de pagos en Payment.

Estos permisos se verifican mediante las políticas definidas en `Shared.Authentication`.

## 2. Requisitos

- Docker Desktop y Docker Compose.
- Credenciales OAuth de Google para el entorno de demostración.
- Puerto `8081` disponible.

## 3. Configuración de credenciales

En `Authorization/Keycloak`, copiar `.env.example` a un archivo nuevo llamado `.env`.

Completar:

```dotenv
KEYCLOAK_ADMIN_PASSWORD=

BOOKING_CLIENT_SECRET=
AVAILABILITY_CLIENT_SECRET=
MVC_CLIENT_SECRET=

GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
```

Los valores reales deben ser proporcionados por el equipo mediante un canal privado.

**Nunca subir `.env`, Client Secrets ni credenciales de Google al repositorio.**

## 4. Iniciar Keycloak

Desde una terminal ubicada en `Authorization/Keycloak`:

```powershell
docker compose config --quiet
docker compose up -d
```

Consultar los logs:

```powershell
docker compose logs --tail=50
```

Keycloak queda disponible en:

`http://127.0.0.1:8081`

La consola de administración se encuentra en:

`http://127.0.0.1:8081/admin/`

Usuario administrador: `admin`.

Contraseña: valor configurado en `KEYCLOAK_ADMIN_PASSWORD`.

Seleccionar el realm `realm-tp-prog4`.

## 5. Autenticación con Google

Se configuró Google como proveedor de identidad externo de Keycloak.

El flujo de autenticación es:

1. El usuario selecciona iniciar sesión desde Availability.Web.
2. El MVC redirige al usuario a Keycloak.
3. Keycloak permite autenticarse mediante Google.
4. Google devuelve el resultado de autenticación a Keycloak.
5. Keycloak completa el flujo OpenID Connect y devuelve al usuario al MVC.
6. El MVC establece una sesión mediante cookies y conserva el access token para las llamadas protegidas.

### Direcciones de retorno

**Google → Keycloak:**

```text
http://127.0.0.1:8081/realms/realm-tp-prog4/broker/google/endpoint
```

**Keycloak → MVC:**

```text
https://localhost:7165/signin-oidc
```

Las direcciones deben coincidir con las URI autorizadas en Google Cloud y con la configuración de Keycloak.

El login con Google fue probado correctamente desde el frontend MVC.

## 6. Autenticación entre microservicios

La comunicación entre servicios utiliza OAuth2 mediante el flujo **Client Credentials**.

### Availability → Booking

1. Availability solicita un access token a Keycloak utilizando `availability-service`.
2. Keycloak emite un JWT destinado a `booking-api`.
3. Availability envía el token en la cabecera `Authorization: Bearer`.
4. Booking valida el token y los permisos correspondientes.

### Booking → Payment

1. Booking solicita un access token utilizando `booking-service`.
2. Keycloak emite un JWT con audiencia `payment-api` y el rol `payments.process`.
3. Booking envía el JWT en los metadatos de la llamada gRPC.
4. Payment valida el token y aplica la política `CanProcessPayment`.

### Validación de JWT

La biblioteca `Shared.Authentication` centraliza la validación de:

- Firma del token.
- Emisor (`iss`).
- Audiencia (`aud`).
- Vencimiento (`exp`).
- Roles y permisos mediante políticas de autorización.

Cada API debe utilizar una dirección de Keycloak compatible con el emisor de los tokens recibidos.

## 7. Verificaciones y pruebas

Se verificaron:

- Inicio de Keycloak mediante Docker.
- Importación y disponibilidad del realm `realm-tp-prog4`.
- Obtención de tokens mediante Client Credentials.
- Configuración de audiencias y roles.
- Inicio de sesión con Google desde Availability.Web.
- Acceso del frontend al catálogo de eventos y asientos.

**Pendiente de validación en el entorno local:** el circuito completo de solicitud, procesamiento y seguimiento de una reserva.

## 8. Consideraciones de seguridad

- Las credenciales se almacenan en `.env`, User Secrets o variables de entorno, según el componente.
- Los Client Secrets no deben incluirse en el código fuente.
- Los tokens deben enviarse mediante cabeceras de autorización o metadatos gRPC, nunca como parámetros de URL.
- El uso de HTTP para Keycloak y `start-dev` corresponde exclusivamente al entorno local de desarrollo.
- Para producción se requiere HTTPS y una configuración de seguridad apropiada.

## 9. Detener Keycloak

Desde `Authorization/Keycloak`:

```powershell
docker compose down
```

La instancia local utiliza Keycloak en modo desarrollo con importación automática del realm. Las modificaciones manuales que no se incorporen a la configuración exportada pueden perderse al recrear el contenedor.

No utilizar `docker compose down -v` si se desea conservar los datos almacenados en volúmenes.

---

Para la implementación de validación JWT, políticas y obtención de tokens, consultar `Authorization/Shared.Authentication/README.md`.