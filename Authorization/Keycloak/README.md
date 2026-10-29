# Keycloak — Instalación para el TP Programación IV

## Objetivo

Levantar Keycloak mediante Docker Compose, con la configuración de autenticación y autorización utilizada por el sistema de reservas.

La importación automática incluye:

- Realm `realm-tp-prog4`.
- Clientes `booking-service`, `availability-service` y `availability-web`.
- Roles `payments.process` y `booking.read`.
- Audiencias `payment-api`, `booking-api` y `availability-api`.
- Proveedor de identidad Google.

## Requisitos

- Docker Desktop instalado y en ejecución.
- Credenciales OAuth de Google para el entorno de demostración.
- Puerto local disponible para Keycloak.

## 1. Preparar las variables

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

Los Client Secrets de los servicios deben ser valores privados adecuados para desarrollo.

Las credenciales de Google deben corresponder a una aplicación OAuth configurada en Google Cloud.

**Nunca subir `.env` al repositorio.**

## 2. Configurar el puerto

El archivo `docker-compose.yml` utiliza actualmente:

```yaml
ports:
  - "127.0.0.1:8081:8080"
```

Por lo tanto, Keycloak queda disponible en:

`http://127.0.0.1:8081`

Para utilizar otro puerto local, modificar el número `8081` del lado izquierdo de la asignación.

El puerto elegido también debe coincidir con la configuración del emisor de tokens utilizada por las aplicaciones y con las URI autorizadas en Google Cloud.

## 3. Levantar Keycloak

Desde una terminal ubicada en `Authorization/Keycloak`:

```powershell
docker compose config --quiet
docker compose up -d
```

Verificar los logs:

```powershell
docker compose logs --tail=50
```

Se espera encontrar:

- `Realm 'realm-tp-prog4' imported`
- `Import finished successfully`
- El mensaje de inicio de Keycloak.

## 4. Entrar a la consola de administración

Abrir:

`http://127.0.0.1:8081/admin/`

Iniciar sesión con:

- Usuario: `admin`.
- Contraseña: valor de `KEYCLOAK_ADMIN_PASSWORD`.

Seleccionar el Realm `realm-tp-prog4`.

## 5. Probar Google

Abrir la consola de cuenta:

`http://127.0.0.1:8081/realms/realm-tp-prog4/account/`

Seleccionar Google para iniciar sesión.

Para que funcione, Google Cloud debe autorizar esta URI de retorno:

```text
http://127.0.0.1:8081/realms/realm-tp-prog4/broker/google/endpoint
```

La prueba de Google desde la Account Console no sustituye la integración pendiente del MVC.

## 6. Configurar las aplicaciones .NET

Las aplicaciones que validan JWT deben utilizar la misma dirección de Keycloak que emite sus tokens.

Ejemplo local con puerto 8081:

```json
{
  "Keycloak": {
    "Authority": "http://127.0.0.1:8081/realms/realm-tp-prog4",
    "Audience": "payment-api"
  }
}
```

Las audiencias son:

| Aplicación receptora | Audiencia |
|---|---|
| Payment | `payment-api` |
| Booking | `booking-api` |
| Availability | `availability-api` |

Los servicios que solicitan tokens deben utilizar los Client Secrets correspondientes, almacenados mediante User Secrets o variables de entorno.

## 7. Detener Keycloak

Desde `Authorization/Keycloak`:

```powershell
docker compose down
```

La configuración actual utiliza Keycloak en modo desarrollo y no tiene una base de datos persistente externa.

Al recrear el contenedor se importará nuevamente el Realm desde el JSON. Los cambios manuales realizados después de la importación no se conservan.

## Consideraciones para la entrega

Esta configuración se probó localmente con Keycloak 26.4.6.

Para la entrega final todavía debe coordinarse:

- La distribución privada de credenciales de demostración.
- La configuración definitiva de puertos y URLs.
- La integración de las APIs y el MVC.
- Las demás dependencias del sistema distribuido.

El uso de HTTP y `start-dev` está destinado únicamente al entorno local de desarrollo.