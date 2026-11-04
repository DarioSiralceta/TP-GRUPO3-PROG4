# Sistema Distribuido de Reserva de Asientos para Eventos
## Programación IV — UTN / TUP
### Grupo 3.

> Integrantes y su Tarea asignada:

| integrante | Tarea |
|-----------|----------------|
| Alexia Corach | authorization/authentication|
| Cherednichenko Dmytro | Booking |
| Franco Brienza | Booking |
| Juan Pablo Gonzalez franco | Documentacion y diagrama: Availability - Booking |
| Lautaro Heinse | Documentacion y diagrama: Payment, consistenciaEventual, Notificaciones |
| Federico Diaz | Payment |
| Gabriel Rossi | gRPC Payment |
| Dario Siralceta | Availability API |
| Thiago Nicolas Borro | Availability FRONT |

### Arbol de carpetas

```
TP-GRUPO3-PROG4
    ├── docs                                     # Documentación y diagramas del proyecto
    │   └── Prompts                              # Prompts de IA utilizados por el equipo
    ├── start-all.sh                             # Script de inicio automatizado de infraestructura y servicios
    ├── Availability                             # Componente de consulta de disponibilidad y frontend web
    │   ├── Availability.API                     # API REST de catálogo y recepción de solicitudes
    │   └── Availability.Web                     # Frontend ASP.NET Core MVC (OIDC / Keycloak)
    ├── Booking                                  # Microservicio de gestión de reservas y máquina de estados
    │   └── src
    │       └── Booking.API                      # API REST, worker de Azure Service Bus y cliente gRPC
    ├── Payment                                  # Microservicio de procesamiento de pagos
    │   └── src
    │       └── Payment.API                      # Servidor gRPC de cobros (Protos/payment.proto)
    ├── Authorization                            # Infraestructura de seguridad e identidad
    │   ├── Keycloak                             # Configuración Docker Compose, realm (.json) y plantilla de variables
    │   └── Shared.Authentication                # Biblioteca C# / .NET 8 compartida para validación de JWT
    ├── FormatoLogsAuditoria.md                  # Estándar obligatorio de logging de auditoría y trazabilidad
    ├── TablaDeComponentes.md                    # Descripción de roles y componentes del sistema
    └── README.md                                # Guía rápida de inicio local e integración
```

## Resumen del Sistema

**El sistema permite gestionar la reserva de asientos para eventos de forma concurrente y distribuida. El ciclo de vida de una reserva contempla cuatro estados principales:**

* **Retenida:** Asiento apartado temporalmente a la espera del cobro transaccional.
* **Confirmada:** Pago procesado de forma exitosa vía gRPC y registrado en la base de datos de Booking.
* **Rechazada:** Pago fallido, servicio de cobro no disponible o conflicto de ocupación/concurrencia sobre el asiento.
* **Vencida:** Tiempo de retención agotado sin procesamiento o confirmación de pago.

## Flujo Funcional de una Reserva

* **Consulta:** El usuario visualiza el catálogo de eventos y la disponibilidad de los asientos en el *Availability Site* (frontend MVC) de forma pública y sin requerir autenticación inicial.
* **Autenticación:** Para solicitar una reserva, el usuario inicia sesión mediante Keycloak (soporta login con credenciales o federación con Google OAuth2) obteniendo una sesión basada en cookies/OIDC y un token JWT emitido por el *Authorization Server*.
* **Solicitud:** El usuario envía la petición desde *Availability Web / API*, la cual genera un identificador único de correlación (`TraceId` / `X-Correlation-ID`), publica el mensaje en el *Message Broker* (Azure Service Bus, tópico `grupo3-booking-topic`) y responde de inmediato confirmando la recepción de la solicitud con un `requestId` (GUID).
* **Procesamiento y Retención:** El Background Worker de *Booking REST API* consume el mensaje encolado desde la suscripción `booking`. Si el asiento está libre, registra la reserva en estado **Retenida**; si está ocupado, la marca como **Rechazada**.
* **Cobro Síncrono:** *Booking REST API* obtiene un token de servicio vía *Client Credentials* (`booking-service` con audiencia `payment-api` y rol `payments.process`) e invoca síncronamente a *Payment gRPC API* adjuntando el JWT en los metadatos HTTP/2 (`authorization: Bearer <JWT>`).
* **Resolución:** Según la respuesta binaria devuelta por *Payment gRPC API*, *Booking REST API* actualiza el estado transaccional de la reserva a **Confirmada** o **Rechazada**.
* **Seguimiento:** El usuario consulta el estado actualizado de su reserva desde la interfaz web (*Availability Web*) o vía API mediante `GET /Reservations/{requestId}`, el cual consulta a *Booking REST API*.
* **Vencimiento:** Si una reserva retenida no completa el cobro antes de agotar su tiempo de tolerancia, un proceso temporizado en *Booking REST API* cambia su estado a **Vencida** y libera el asiento.

## Estilo de Microservicios y Stack Tecnológico

**La solución está desarrollada integralmente en .NET 8 bajo un estilo arquitectónico de microservicios con responsabilidades de dominio delimitadas:**

* **Availability Site & API:** *Availability.Web* es una aplicación ASP.NET Core MVC (.NET 8) integrada con Keycloak vía OpenID Connect (PKCE) y cookies de sesión. *Availability.API* implementa **Clean Architecture** con Dominio Anémico. Expone catálogo, disponibilidad y recepción de solicitudes de reserva.
* **Booking REST API:** Implementado bajo arquitectura **N-Layer** con Dominio Anémico. Concentra la máquina de estados de las reservas, gestiona la concurrencia optimista sobre los asientos, consume las solicitudes desde Azure Service Bus y actúa como cliente gRPC hacia Payment API.
* **Payment gRPC API:** Implementado bajo arquitectura **N-Layer** con Dominio Anémico. Expone un servidor gRPC con contrato Protocol Buffers (`payment.proto`) para procesar cobros de manera síncrona sobre HTTP/2. Está protegido mediante la biblioteca compartida `Shared.Authentication` con la política `CanProcessPayment` (requiere el rol `payments.process`).
* **Authorization Server & Shared.Authentication:** Servidor de identidad basado en **Keycloak 26.x** (OAuth2 / OpenID Connect / JWT) desplegado en Docker (`realm-tp-prog4`). Adicionalmente, el proyecto incluye **Shared.Authentication**, una biblioteca C# compartida que encapsula los middlewares de validación JWT Bearer (`AddKeycloakAuthentication` / `AddKeycloakAuthorization`) y el servicio `KeycloakTokenService` para flujos M2M Client Credentials.
* **Message Broker (Azure Service Bus):** Administra la mensajería asíncrona mediante el tópico `grupo3-booking-topic` y la suscripción `booking`, canalizado vía código (.NET SDK) para garantizar el desacoplamiento y la resiliencia entre Availability y Booking.
* **Servicio de Notificaciones:** Componente incorporado exclusivamente a nivel de diseño y diagrama de arquitectura (patrón Pub/Sub). Se suscribe al evento de confirmación en el broker para notificar al cliente vía Email/SMS sin bloquear la ejecución de las APIs.
* **Persistencia Aislada:** Cada microservicio gestiona su propia base de datos relacional independiente en SQL Server / LocalDB (`AvailabilityDb`, `BookingDb`, `PaymentDb`), cumpliendo estrictamente con el patrón **Database-per-service**.

### Consistencia Eventual (Teorema de CAP)
El sistema adopta un modelo **AP** (Alta Disponibilidad y Tolerancia al Particionado) derivado del **Teorema de CAP**. Debido al desacoplamiento asíncrono que introduce el *Message Broker* (Azure Service Bus), se define la **Zona de Consistencia Eventual** entre la respuesta inmediata enviada por *Availability Site* al cliente al recepcionar la solicitud y la confirmación o rechazo definitivo grabado en la base de datos de *Booking REST API*.
Durante esa ventana temporal, la reserva se procesa en segundo plano y la información del asiento se sincroniza de forma eventual tras procesarse el mensaje y resolverse el cobro por gRPC.

## Prerrequisitos de Entorno

**Para compilar, desplegar y ejecutar la solución distribuida en un entorno local, se requiere contar con las siguientes herramientas e infraestructura instaladas:**
* **.NET 8 SDK:** Necesario para la compilación, restauración de paquetes NuGet y ejecución de las soluciones en C# (*Availability.Web*, *Availability.API*, *Booking.API*, *Payment.API* y *Shared.Authentication*).
* **Certificado HTTPS de Desarrollo:** Requerido para habilitar el cifrado SSL/TLS en entornos locales (`dotnet dev-certs https --trust`).
* **SQL Server / LocalDB:** Motor de base de datos relacional requerido para alojar las bases de datos independientes de cada microservicio (`AvailabilityDb`, `BookingDb`, `PaymentDb`), respetando el patrón Database-per-service.
* **Docker Desktop / Docker Compose:** Requerido para levantar el contenedor del servidor de autorización (**Keycloak 26.x**) e infraestructura de soporte.
* **PowerShell 7+ / Bash CLI:** Entornos de ejecución para ejecutar el script de inicio unificado (`./start-all.sh`) o scripts de pruebas de integración e QA (`IntegrationTests.ps1`).


## Configuración de Variables de Entorno y Secretos

### 1. Azure Service Bus (Mensajería Asíncrona)
La comunicación asíncrona entre Availability y Booking se gestiona mediante Azure Service Bus utilizando la configuración del tópico y la suscripción. La estructura se especifica en los archivos `appsettings.json` o mediante `dotnet user-secrets`:

```json
{
  "AzureServiceBus": {
    "ConnectionString": "<AZURE_SERVICE_BUS_CONNECTION_STRING>",
    "TopicName": "grupo3-booking-topic",
    "SubscriptionName": "booking"
  }
}
```

#### Comandos de User Secrets (.NET CLI):
```bash
# Booking.API
dotnet user-secrets set "AzureServiceBus:ConnectionString" "<AZURE_CONNECTION_STRING>" --project Booking/src/Booking.API

# Availability.API
dotnet user-secrets set "AzureServiceBus:ConnectionString" "<AZURE_CONNECTION_STRING>" --project Availability/Availability.API
dotnet user-secrets set "AzureServiceBus:TopicName" "grupo3-booking-topic" --project Availability/Availability.API
```

### Cadenas de Conexión a SQL Server (Database-per-service)

Cada microservicio gestiona su propia persistencia transaccional independiente en SQL Server / LocalDB, alineado al patrón Database-per-service. Las cadenas de conexión se parametrizan individualmente en los archivos appsettings.json de cada API:

#### **En Availability (src/Availability/appsettings.json)**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\mssqllocaldb;Database=AvailabilityDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

#### **En Booking (src/Booking/appsettings.json)**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\mssqllocaldb;Database=BookingDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

#### **En Payment (src/Payment/appsettings.json)**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\mssqllocaldb;Database=PaymentDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 3. Keycloak, JWT y Secretos
El servidor Keycloak se despliega localmente vía Docker Compose en los puertos `http://localhost:8081` / `8080` (Realm: `realm-tp-prog4`).

#### Estructura del archivo de variables en `Authorization/Keycloak/.env`:
```dotenv
KEYCLOAK_ADMIN_PASSWORD=<KEYCLOAK_ADMIN_PASS>
BOOKING_CLIENT_SECRET=<BOOKING_SECRET>
AVAILABILITY_CLIENT_SECRET=<AVAILABILITY_SECRET>
MVC_CLIENT_SECRET=<MVC_SECRET>
GOOGLE_CLIENT_ID=<GOOGLE_CLIENT_ID>
GOOGLE_CLIENT_SECRET=<GOOGLE_CLIENT_SECRET>
```

#### Clientes, Audiencias y Roles Configurados:
| Cliente | Tipo / Flujo | Audiencia (`aud`) | Rol / Política | Callback / Uso |
|---|---|---|---|---|
| **availability-web** | OIDC (Authorization Code + PKCE) | `availability-api` | Login Usuarios | `https://localhost:7165/signin-oidc` |
| **availability-service** | Client Credentials (M2M) | `booking-api` | `booking.read` / `CanReadBooking` | Consultas entre Availability y Booking |
| **booking-service** | Client Credentials (M2M) | `payment-api` | `payments.process` / `CanProcessPayment` | Llamadas gRPC de Booking a Payment |

#### Configuración de Secretos en Availability.Web:
```bash
dotnet user-secrets set "Keycloak:ClientSecret" "<MVC_CLIENT_SECRET>" --project Availability/Availability.Web
```

## Guía de Despliegue y Ejecución Local

### Opción A: Despliegue Automatizado (Recomendado)
El repositorio incluye el script `start-all.sh` en la raíz para preparar el entorno y ejecutar todos los componentes en paralelo con un único comando:

1. **Otorgar permisos de ejecución** *(primera vez)*:
```bash
chmod +x start-all.sh
```
2. **Ejecutar el script de inicio:**
```bash
./start-all.sh
```
3. **Detener la solución:** Presionar `Ctrl + C` en la terminal.

### Opción B: Despliegue Manual Secuencial
Si se prefiere ejecutar los componentes individualmente en terminales separadas, seguir este orden estricto:

#### Paso 1: Servidor de Autorización (Keycloak)
1. Navegar a la carpeta del contenedor:
```bash
cd Authorization/Keycloak
```
2. Copiar `.env.example` a `.env` *(si no existe)* y levantar el contenedor:
```bash
docker compose up -d
```
3. Confirmar que la consola de administración esté activa en `http://localhost:8081/admin/` (Realm: `realm-tp-prog4`).

#### Paso 2: Payment gRPC API
1. Abrir una nueva terminal y navegar a la API de Payment:
```bash
cd Payment/src/Payment.API
```
2. Ejecutar el servicio gRPC:
```bash
dotnet run
```

#### Paso 3: Booking REST API
1. Abrir una nueva terminal y navegar a Booking API:
```bash
cd Booking/src/Booking.API
```
2. Iniciar la API REST y el Worker de Azure Service Bus:
```bash
dotnet run
```
3. Disponible en `http://localhost:5194/swagger`.

#### Paso 4: Availability API
1. Abrir una nueva terminal y navegar a Availability API:
```bash
cd Availability/Availability.API
```
2. Ejecutar la API REST de catálogo:
```bash
dotnet run
```

#### Paso 5: Availability Site (Web MVC)
1. Abrir una nueva terminal y navegar al frontend MVC:
```bash
cd Availability/Availability.Web
```
2. Iniciar el sitio web con el perfil HTTPS:
```bash
dotnet run --launch-profile https
```
3. Acceder desde el navegador a **`https://localhost:7165/`**.

---
## Tabla de Referencia de Servicios y Puertos

| Servicio / Componente | Protocolo / URL | Notas y Advertencias de Seguridad |
|---|---|---|
| **Availability Web UI** | `https://localhost:7165` | **Debe utilizarse el puerto HTTPS 7165**. El acceso por HTTP (`5089`) fallará en las redirecciones de OIDC/Google. |
| **Availability API** | `https://localhost:7004`<br>`http://localhost:5000` | Expone endpoints JSON de catálogo, asientos y recepción de solicitudes. |
| **Booking REST API** | `http://localhost:5194/swagger` | Orquestador de reservas, máquina de estados y consumidor del broker. |
| **Payment gRPC API** | `http://localhost:5000` *(o puerto gRPC)* | Servidor gRPC binario HTTP/2 protegido con la política `CanProcessPayment`. |
| **Keycloak Admin UI** | `http://localhost:8081` | Panel de administración de identidades, realms (`realm-tp-prog4`) y clientes. |

## Referencia de Endpoints y Verificación de QA (Happy / Failure Path)

### 1. Mapa de Endpoints Principales por Microservicio

| Microservicio | Método / Protocolo | Endpoint | Autenticación / Rol | Descripción |
| :--- | :--- | :--- | :--- | :--- |
| **Availability.API** | GET REST | `/events` | Público | Consulta el catálogo de eventos. |
| **Availability.API** | GET REST | `/events/{eventId}/seats` | Público | Consulta disponibilidad física de asientos. |
| **Availability.API** | POST REST | `/Reservations` | User JWT (Bearer) | Registra la solicitud de reserva y publica en Azure Service Bus. |
| **Availability.API** | GET REST | `/Reservations/{requestId}` | User JWT (Bearer) | Consulta el estado de seguimiento de la reserva (habla con Booking). |
| **Booking.API** | GET REST | `/api/bookings/{solicitudId}` | Service JWT (`booking.read`) | Endpoint interno de seguimiento de estado transaccional. |
| **Payment.API** | gRPC (HTTP/2) | `payment.PaymentService/ProcessPayment` | Service JWT (`payments.process`) | Invocación síncrona binaria para cobro transaccional. |

---

### 2. Guía de Ejecución de Pruebas de Integración (QA)

#### Escenario 1: Happy Path (Flujo Exitoso)
1. **Iniciar Infraestructura**: Ejecutar `./start-all.sh` (o levantar Keycloak y las APIs nativamente con `dotnet run`).
2. **Autenticación**: Iniciar sesión en `https://localhost:7165` vía Keycloak/Google.
3. **Solicitar Reserva**: Seleccionar un evento y un asiento libre. Se generará un `requestId` (GUID).
4. **Procesamiento**: El worker de Booking consumirá el mensaje de Azure Service Bus, retendrá el asiento, cobrará vía gRPC a Payment y confirmará la reserva.
5. **Verificación**: Consultar la pantalla de seguimiento o hacer `GET /Reservations/{requestId}`; la reserva devolverá estado **`Confirmada`**.

#### Escenario 2: Failure Path (Manejo de Fallas y Resiliencia)
* **Caso A (Falla de Cobro / Rechazo)**: Si Payment gRPC simula un pago rechazado, el estado de la reserva se actualizará a **`Rechazada`** liberando el asiento.
* **Caso B (Seguridad - Acceso No Autorizado)**: Consultar `GET /api/bookings/{solicitudId}` en Booking sin el token de servicio devolverá **`HTTP 401 Unauthorized`**.
* **Caso C (Indisponibilidad de Servicio)**: Al apagar el proceso de `Booking.API` o detener Azure Service Bus, `Availability.API` responderá **`HTTP 503 Service Unavailable`** sin romper la aplicación.
