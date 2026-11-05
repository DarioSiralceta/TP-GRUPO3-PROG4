## Programación IV — UTN / TUP
### Grupo 3

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
    ├── start-all.sh                             # Script de inicio automatizado (Linux/macOS)
    ├── iniciar-sistema.bat                      # Script de inicio automatizado (Windows)
    ├── Availability                             # Componente de consulta de disponibilidad y frontend web
    │   ├── Availability.API                     # API REST de catálogo y recepción de solicitudes
    │   ├── Availability.Web                     # Frontend ASP.NET Core MVC (OIDC / Keycloak)
    |   └── Availability.Infraestructure         # Biblioteca compartida de infraestructura y servicios
    ├── Booking                                  # Microservicio de gestión de reservas y máquina de estados
    │   └── src 
    │       ├── Booking.API                      # API REST, worker de Azure Service Bus y cliente gRPC
    |	    ├── Booking.Infrastructure          
    |	    └── Booking.Service                  # Lógica de aplicación, cliente gRPC y contratos de Payment
    ├── Payment                                  # Microservicio de procesamiento de pagos
    │       ├── Payment.API                      # Servidor gRPC de cobros (Protos/payment.proto)
    |       ├── Payment.BLL                      # Biblioteca de lógica de negocio y servicios de dominio
    |       └── Payment.DAL                      # Biblioteca de persistencia y repositorios (SQLite)
    ├── Authorization                            # Infraestructura de seguridad e identidad
    │   ├── Auth.TestApi                         # API de pruebas de autenticación
    │   ├── Keycloak                             # Configuración Docker Compose, realm (.json) y plantilla de variables
    │   └── Shared.Authentication                # Biblioteca C# / .NET 8 compartida para validación de JWT    ├── FormatoLogsAuditoria.md                  # Estándar obligatorio de logging de auditoría y trazabilidad
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

La solución está desarrollada sobre .NET 8 y .NET 10 según el microservicio. Booking utiliza .NET 10, mientras que Payment, Availability y Shared.Authentication utilizan .NET 8.

* **Availability Site & API:** *Availability.Web* es una aplicación ASP.NET Core MVC (.NET 8) integrada con Keycloak vía OpenID Connect y cookies de sesión. *Availability.API* implementa **Clean Architecture** con Dominio Anémico. Expone catálogo, disponibilidad y recepción de solicitudes de reserva.
* **Booking REST API:** Implementado bajo arquitectura **N-Layer** con Dominio Anémico. Concentra la máquina de estados de las reservas, gestiona la concurrencia optimista sobre los asientos, consume las solicitudes desde Azure Service Bus y actúa como cliente gRPC hacia Payment API.
* **Payment gRPC API:** Implementado bajo arquitectura **N-Layer** con Dominio Anémico. Expone un servidor gRPC con contrato Protocol Buffers (`payment.proto`) para procesar cobros de manera síncrona sobre HTTP/2. Está protegido mediante la biblioteca compartida `Shared.Authentication` con la política `CanProcessPayment` (requiere el rol `payments.process`).
* **Authorization Server & Shared.Authentication:** Servidor de identidad basado en **Keycloak 26.x** (OAuth2 / OpenID Connect / JWT) desplegado en Docker (`realm-tp-prog4`). Adicionalmente, el proyecto incluye **Shared.Authentication**, una biblioteca C# compartida que encapsula los middlewares de validación JWT Bearer (`AddKeycloakAuthentication` / `AddKeycloakAuthorization`) y el servicio `KeycloakTokenService` para flujos M2M Client Credentials.
* **Message Broker (Azure Service Bus):** Administra la mensajería asíncrona mediante el tópico `grupo3-booking-topic` y la suscripción `booking`, canalizado vía código (.NET SDK) para garantizar el desacoplamiento y la resiliencia entre Availability y Booking.
* **Servicio de Notificaciones:** Componente incorporado exclusivamente a nivel de diseño y diagrama de arquitectura (patrón Pub/Sub). Se suscribe al evento de confirmación en el broker para notificar al cliente vía Email/SMS sin bloquear la ejecución de las APIs.
* **Persistencia aislada:** Cada microservicio administra su propia persistencia siguiendo el patrón **Database-per-service**. Booking utiliza PostgreSQL, mientras que Availability y Payment utilizan SQLite.
* **Entity Framework Core CLI:** necesaria para ejecutar las migraciones: `dotnet tool install --global dotnet-ef`


### Consistencia eventual

El sistema presenta consistencia eventual entre Availability y Booking debido al procesamiento asíncrono mediante Azure Service Bus.
Availability puede responder inicialmente con un `requestId`, mientras Booking procesa la solicitud en segundo plano y actualiza posteriormente el estado de la reserva. 
Durante ese intervalo, el estado observado por los distintos servicios puede no estar sincronizado inmediatamente.

## Prerrequisitos de Entorno

**Para compilar, desplegar y ejecutar la solución distribuida en un entorno local, se requiere contar con las siguientes herramientas e infraestructura instaladas:**

- **.NET SDK compatible con .NET 8 y .NET 10:** Necesario para la compilación, restauración de paquetes NuGet y ejecución de las soluciones en C# (*Availability.Web*, *Availability.API*, *Booking.API*, *Payment.API* y *Shared.Authentication*).
- **Certificado HTTPS de Desarrollo:** Requerido para habilitar el cifrado SSL/TLS en entornos locales (`dotnet dev-certs https --trust`).
- **Docker Desktop / Docker Compose:** necesarios para ejecutar PostgreSQL y Keycloak.
- **PostgreSQL:** utilizado por Booking y levantado mediante Docker Compose.
- **SQLite:** utilizado por Availability y Payment mediante archivos locales `.db`.
- **PowerShell / Bash CLI / Windows CMD:** Entornos de ejecución para los scripts automatizados de inicio unificado (`./start-all.sh` para Linux/macOS o `iniciar-sistema.bat` para Windows) y scripts de pruebas.

## Configuración de Variables de Entorno y Secretos

### 1. Azure Service Bus (Mensajería Asíncrona)

La comunicación asíncrona entre Availability y Booking se gestiona mediante Azure Service Bus utilizando la configuración del tópico y la suscripción. La estructura se especifica en los archivos `appsettings.json` o mediante `dotnet user-secrets`:

```json
{
  "AzureServiceBus": {
    "ConnectionString": "<AZURE_SERVICE_BUS_CONNECTION_STRING>",
    "TopicName": "grupo3-booking-topic",
    "SubscriptionName": "booking",
    "MaxConcurrentCalls": 1
  }
}
```

#### Comandos de User Secrets (.NET CLI):
```bash
# Booking.API
dotnet user-secrets set "AzureServiceBus:ConnectionString" "<AZURE_CONNECTION_STRING>" --project Booking/src/Booking.API
dotnet user-secrets set "Keycloak:ClientSecret" "<BOOKING_CLIENT_SECRET>" --project Booking/src/Booking.API

# Availability.API
dotnet user-secrets set "AzureServiceBus:ConnectionString" "<AZURE_CONNECTION_STRING>" --project Availability/Availability.API
dotnet user-secrets set "AzureServiceBus:TopicName" "grupo3-booking-topic" --project Availability/Availability.API
```

### Persistencia independiente por microservicio

Cada microservicio administra su propia persistencia, siguiendo el patrón Database-per-service:
- Booking utiliza PostgreSQL.
- Availability utiliza SQLite.
- Payment utiliza SQLite.

#### **En Availability (Availability/Availability.API/appsettings.json)**

```json
{
  "ConnectionStrings": {
    "AvailabilityDb": "Data Source=availability.db"
  }
}
```

#### **En Booking (Booking/src/Booking.API/appsettings.json)**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=booking_db;Username=postgres;Password=password123"
  }
}
```

#### **En Payment (Payment/Payment.API/appsettings.json)**

```json
{
  "ConnectionStrings": {
    "PaymentDb": "Data Source=payment.db"
  }
}
```

### 3. Keycloak, JWT y Secretos

Keycloak se ejecuta en Docker. El contenedor utiliza internamente el puerto `8080` y se publica localmente mediante:

`http://127.0.0.1:8081`

El realm utilizado es `realm-tp-prog4`.

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

El repositorio incluye scripts automatizados para preparar la infraestructura y ejecutar parte de los servicios.

`start-all.sh` inicia todos los servicios .NET, incluido Payment y Auth.TestApi.

`iniciar-sistema.bat` inicia PostgreSQL, Keycloak, Booking.API, Availability.API y Availability.Web, pero actualmente no inicia Payment.API ni Auth.TestApi.
 Estos dos servicios deben ejecutarse manualmente en Windows.

#### En Linux / macOS (Bash):
1. **Otorgar permisos de ejecución** *(primera vez)*:
   ```bash
   chmod +x start-all.sh
   ```
2. **Ejecutar el script de inicio**:
   ```bash
   ./start-all.sh
   ```
En la primera ejecución, si `Authorization/Keycloak/.env` no existe, el script lo crea a partir de `.env.example` y finaliza. 
Debes completar las credenciales del archivo y ejecutar nuevamente `./start-all.sh`.
3. **Detener la solución:** Presionar `Ctrl + C` en la terminal.

#### En Windows (CMD / PowerShell)

1. Ejecutar `iniciar-sistema.bat` haciendo doble clic sobre el archivo o desde una terminal.
2. Como el script no inicia `Payment.API` ni `Auth.TestApi`, ejecutar manualmente: `cd Payment/Payment.API` y `dotnet run --launch-profile https` 
en otra terminal para Payment, y lo mismo en `cd Authorization/Auth.TestApi` y `dotnet run --launch-profile http` para Auth.TestApi.
El script inicia Booking en `https://localhost:7296/swagger`, Availability API en `https://localhost:7004/swagger` y Availability Web en `https://localhost:7165`.

### Opción B: Despliegue Manual Secuencial

Si se prefiere ejecutar los componentes individualmente en terminales separadas, seguir este orden estricto:

#### Paso 1: Servidor de Autorización (Keycloak)
1. Navegar a la carpeta del contenedor:

```bash
cd Authorization/Keycloak
```

2. Copiar la plantilla `.env.example` a `.env`.

Para Linux/macOS:

``` bash
cp .env.example .env
```
* Para Windows PowerShell:
* 
```bash
Copy-Item .env.example .env
```

Completar las variables de `Authorization/Keycloak/.env` con valores reales.

Luego iniciar Keycloak:
```bash
docker compose up -d
```

3. Confirmar que la consola de administración esté activa en http://localhost:8081/admin/ (Realm: realm-tp-prog4).

#### Paso 2: Payment gRPC API
1. Abrir una nueva terminal y navegar a la API de Payment:

```bash
cd Payment/Payment.API
```

2. Ejecutar el servicio gRPC con el perfil HTTPS:

```bash
dotnet run --launch-profile https
```

Booking está configurado para conectarse a Payment mediante `https://localhost:7016`, por lo que debe utilizarse el perfil HTTPS.

#### Paso 3: Auth.TestApi

1. Abrir una nueva terminal y navegar al proyecto:

```bash
cd Authorization/Auth.TestApi
```

2. Ejecutar el servicio con el perfil HTTP:

```bash
dotnet run --launch-profile http
```

El servicio estará disponible en `http://localhost:5009/swagger`.
Alternativamente, si quieres HTTPS:

```
dotnet run --launch-profile https
```

y la URL correcta sería: https://localhost:7102/swagger

#### Paso 4: Booking REST API
1. Abrir una nueva terminal y navegar a Booking API:
```bash
cd Booking/src/Booking.API
```
2. Iniciar la API REST y el Worker de Azure Service Bus:
```bash
dotnet run --launch-profile http
```
3. Disponible en `http://localhost:5194/swagger`.
O, si quieres usar HTTPS:
```bash
dotnet run --launch-profile https
```
Disponible en `https://localhost:7296/swagger`.

#### Paso 5: Availability API
1. Abrir una nueva terminal y navegar a Availability API:
```bash
cd Availability/Availability.API
```
2. Ejecutar la API REST de catálogo:
```bash
dotnet run --launch-profile https
```
La API estará disponible en `https://localhost:7004/swagger`.

#### Paso 6: Availability Site (Web MVC)
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
| **Availability API** | `https://localhost:7004/swagger`<br>`http://localhost:5057/swagger` | Expone endpoints JSON de catálogo, asientos y recepción de solicitudes. |
| **Booking REST API** | `http://localhost:5194/swagger`<br>`https://localhost:7296/swagger`  | Orquestador de reservas, máquina de estados y consumidor del broker. |
| **Payment gRPC API** | `https://localhost:7016`<br>`http://localhost:5062` | Servidor gRPC HTTP/2 protegido con la política `CanProcessPayment`. |
| **Keycloak Admin UI** | `http://localhost:8081` | Panel de administración de identidades, realms (`realm-tp-prog4`) y clientes. |
| **Auth Test API** | `http://localhost:5009/swagger` | API de pruebas de autenticación y autorización con Keycloak. |

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
* **Caso C (Indisponibilidad de Servicio)**: Si `Booking.API` o Azure Service Bus no están disponibles, la operación de reserva o seguimiento puede fallar. La aplicación debería traducir esta situación a `HTTP 503 Service Unavailable`.