**REGISTRO DE INTERACCIONES CON IA**

**Dario Siralceta**

**Trabajo Práctico — Programación IV (UTN)\**
Componente: Availability API\
Herramienta utilizada: ChatGPT

## Interacción 1 — Análisis y arquitectura

**Prompt (reformulado):** “Analizá la consigna y ayúdame a organizar
Availability API con Clean Architecture y dominio anémico, delimitando
sus responsabilidades frente a Booking.”

**Resumen de la respuesta:** Se revisó el alcance del microservicio y se
organizó la solución en API, Aplicación, Domain e Infraestructura.

**Archivos creados o modificados:** Availability.sln y proyectos
Availability.API, Availability.Application, Availability.Domain y
Availability.Infraestructure.

**Decisiones técnicas:** Availability mantiene la información de
disponibilidad física; Booking conserva la responsabilidad sobre
reservas y ocupación. Se respetan los límites entre microservicios y las
dependencias de Clean Architecture.

## Interacción 2 — Dominio y persistencia

**Prompt (reformulado):** “Ayúdame a modelar la solicitud de reserva y
configurar su persistencia con Entity Framework Core y SQLite,
manteniendo el dominio anémico.”

**Resumen de la respuesta:** Se trabajó sobre ReservationRequest,
AvailabilityDbContext, la configuración de SQLite, la migración inicial
y el repositorio para guardar y recuperar solicitudes.

**Archivos creados o modificados:** ReservationRequest,
AvailabilityDbContext, configuración de conexión, migración
InitialCreate y ReservationRequestRepository.

**Decisiones técnicas:** Las entidades contienen datos; el acceso a
persistencia se realiza mediante repositorios e infraestructura, sin
acoplar los casos de uso a EF Core.

## Interacción 3 — Consulta de eventos y asientos

**Prompt (reformulado):** “Ayúdame a implementar los casos de uso y
repositorios para consultar eventos y sus asientos, considerando que
Booking informa cuáles están ocupados.”

**Resumen de la respuesta:** Se organizaron los casos de uso de consulta
de eventos y asientos, sus interfaces y repositorios. Se contempló la
consulta HTTP a Booking para obtener la ocupación.

**Archivos creados o modificados:** GetEvents, GetEventSeats,
IEventRepository, EventRepository, ISeatRepository, SeatRepository y
BookingAvailabilityHttpClient.

**Decisiones técnicas:** Availability conserva los datos físicos del
evento y sus asientos; la ocupación de reservas se consulta a Booking
para evitar duplicar esa responsabilidad.

## Interacción 4 — Solicitud de reserva y mensajería

**Prompt (reformulado):** “Revisemos RequestReservation para registrar
una solicitud, generar un RequestId y publicar ReservationRequested
mediante Azure Service Bus.”

**Resumen de la respuesta:** Se implementó el caso de uso, el resultado
inicial Received, la persistencia y el publisher para enviar el mensaje
al topic utilizado por Booking.

**Archivos creados o modificados:** RequestReservationCommand,
RequestReservationResult, RequestReservationUseCase, RequestStatus,
IReservationRequestPublisher, ReservationRequested y
AzureServiceBusReservationRequestPublisher.

**Decisiones técnicas:** La solicitud se identifica mediante RequestId y
se comunica de forma asíncrona. Application depende de una interfaz;
Service Bus queda en Infraestructure.

## Interacción 5 — Autenticación con Keycloak

**Prompt (reformulado):** “Ayúdame a configurar la validación JWT de
Availability API con Keycloak y proteger los endpoints; además, necesito
autenticar las llamadas salientes hacia Booking.”

**Resumen de la respuesta:** Se trabajó en la validación de tokens
entrantes y en la obtención de tokens mediante client credentials para
las consultas HTTP entre servicios.

**Archivos creados o modificados:** Configuración de autenticación en
Availability.API, registro de servicios y componentes de
obtención/adjunción de token para el cliente HTTP.

**Decisiones técnicas:** Se diferenció la autenticación de usuarios que
acceden a Availability de la autenticación de servicio a servicio. No se
incluyen credenciales.

## Interacción 6 — Integración y seguimiento con Booking

**Prompt (reformulado):** “Ayúdame a implementar clientes HTTP para
consultar asientos ocupados y el estado de una reserva en Booking, y
exponer el seguimiento por RequestId.”

**Resumen de la respuesta:** Se trabajó sobre los clientes HTTP y
GetReservationStatus para que Availability pueda devolver el estado
informado por Booking.

**Archivos creados o modificados:** IBookingReservationClient,
BookingReservationHttpClient, GetReservationStatusQuery,
GetReservationStatusResult, GetReservationStatusUseCase y
ReservationsController.

**Decisiones técnicas:** Booking es la fuente de verdad del estado de la
reserva. Availability expone el seguimiento usando RequestId y no
mantiene una copia independiente del estado.

## Interacción 7 — Auditoría, pruebas y depuración

**Prompt (reformulado):** “Ayúdame a revisar el flujo integrado de
Availability, detectar errores de configuración o autenticación y
verificar la trazabilidad de la solicitud.”

**Resumen de la respuesta:** Se revisaron compilación, ejecución,
pruebas desde Swagger, respuestas HTTP, logs y el recorrido de la
solicitud entre Availability y Booking.

**Archivos creados o modificados:** Se revisaron controladores,
configuración, clientes HTTP, logs y componentes de integración ya
mencionados; no se identifica un único archivo nuevo para esta etapa.

**Decisiones técnicas:** Se utiliza RequestId/identificador de
correlación para relacionar eventos y registros. Se verifican por
separado autenticación, persistencia, publicación y consulta posterior
del estado.
