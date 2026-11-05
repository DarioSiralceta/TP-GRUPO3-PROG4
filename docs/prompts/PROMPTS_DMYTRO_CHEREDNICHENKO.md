# Historial de decisiones y desarrollo — Booking

- **Proyecto:** Trabajo Práctico — Programación IV (UTN) — `TP-GRUPO3-PROG4`
- **Componente:** `Booking API` / `Booking Service` / `Booking Infrastructure`
- **Herramientas:** Antigravity (Gemini 3.8 Flash) y Copilot SDK en VS Code

Este documento conserva las consultas que influyeron en la arquitectura, los contratos entre servicios y las implementaciones relevantes. Se omitieron preguntas introductorias, consultas puntuales de errores y mensajes sin impacto técnico en el diseño.

## Arquitectura y flujo de reserva

### Definición de capas y responsabilidades

**Prompt:**
> So the three N-layers live under 1 solution as 3 projects. What about my availability site? Will it live in the same solution? If the availability website is built using clean architecture how many projects will it contain? And at what level is it separated from booking?
>
> My program.cs currently connects to db context. Shouldn’t this be avoided?

**Resultado:** Se organizó Booking en proyectos separados para API, lógica de servicio e infraestructura/persistencia, manteniendo las responsabilidades diferenciadas dentro de la solución compartida.

### Flujo asíncrono desde Availability hasta Payment

**Prompt:**
> The diagram does not illustrate the connection between availability (that includes frontend) and payment api. That means the payment info will travel on the azure bus to booking and then be passed to payment api?
>
> The teacher confirmed it has to travel through booking. Where do I call the gRPC from? From the service?
>
> So the Azure Service Bus message arrives to the controller. The controller passes it to the service. The service maps the data onto the db context entity and sends it to persistence where it is saved. Right? Where does this worker/consumer live?

**Resultado:** Se definió el flujo de solicitudes por Azure Service Bus hacia un worker de Booking y el procesamiento del pago desde la capa de servicio mediante gRPC. El consumidor vive en Booking API; no es un endpoint HTTP de Availability.

### Worker de Azure Service Bus

**Prompt:**
> According to the project can you design a background worker in Booking API tasked with communicating with Availability site via Azure Service Bus?

**Resultado:** Se implementó `BookingReservationWorker` como `BackgroundService`, con `ServiceBusProcessor`, deserialización del mensaje y confirmación o reintento de procesamiento. Las credenciales de conexión se excluyeron del registro.

### Contratos entre servicios

**Prompt:**
> Awesome. So I am responsible for Booking. My colleagues are responsible for gRPC and Availability. What info should we agree on? Contracts for the data sent over the bus from Availability to Booking and contract from Booking to gRPC, right? Anything else?
>
> My classmates and I are agreening on DTOs. I don’t have one. I imagine an expected payload to contain `Guid bookingId`, `string userId`, `decimal amount`, `CancellationToken cancellationToken = default`. Suggest a contract and let me know what I have to request from them.

**Resultado:** Se identificaron los contratos que los equipos deben acordar en ambos límites: el mensaje de reserva Availability → Booking y la solicitud Booking → Payment gRPC.

## Integridad de reservas y persistencia

### Prevención de reservas duplicadas

**Prompt:**
> A user sees seat #7 as available, leaves, and comes back after someone else has booked it. I want to avoid booking it twice: when do I check it and how do I make sure his payment does not go through?
>
> It has been suggested to add a unique constraint on the database seat ID, excluding `Vencida` and `Rechazada`, as another layer to prevent double booking. Does that make sense?

**Resultado:** Se incorporó una defensa a nivel de base de datos mediante un índice único parcial sobre evento y asiento para reservas activas (`Retenida` y `Confirmada`). El procesamiento verifica la ocupación antes de iniciar el cobro.

### Estados de reserva con tipo estricto

**Prompt:**
> Is it enforced anywhere that seats can only have four statuses: `Retenida`, `Confirmada`, `Rechazada`, `Vencida`? The entity contains a string and the service deals with strings. Wouldn't it be better to add an enum?

**Resultado:** Se creó `BookingStatus` y se configuró la persistencia para almacenar sus valores como texto, además de reforzar los estados permitidos en la base.

### Expiración de retenciones e idempotencia

**Prompt:**
> There is repetitive code checking for expired bookings in several methods. `ProcessReservationRequestAsync` should check expired reservations first, and seems to repeat code already covered elsewhere. Verify and fix if necessary.
>
> Would running expiration checks across all reservations on each request significantly increase database load? Would a background worker be better?
>
> Should `ProcessReservationRequestAsync` check expiration first and verify whether the Request ID was already processed second, or keep the current order?

**Resultado:** Se separó el cambio de estado por expiración de las consultas normales mediante `BookingRetentionExpirationWorker`. La comprobación de `RequestId` existente antes de procesar de nuevo preserva la idempotencia frente a entregas duplicadas de Service Bus.

### Recuperación ante colisiones concurrentes en PostgreSQL

**Prompt:**
> When PostgreSQL throws a unique constraint violation (`SqlState == "23505"`), detaching the entity does not reset the aborted transaction. A subsequent `SaveChangesAsync` can fail with `25P02`. How should this recovery be fixed?

**Resultado:** Se analizó la necesidad de recuperar la operación de rechazo en un contexto/transacción utilizable, en lugar de intentar continuar guardando dentro de una transacción abortada.

### Eliminación del flujo HTTP de creación directa

**Prompt:**
> `CreateRetention` is wired to an HTTP POST, but reservations now arrive through the Azure Service Bus worker. Can you verify whether it is obsolete?
>
> Please get rid of it and the associated POST method and any other code that serves no other purpose.

**Resultado:** Se verificó el flujo de entrada por mensajería y se eliminó el POST directo de creación de retenciones junto con la lógica que solo servía a ese recorrido.

### Revisión de endpoints no utilizados

**Prompt:**
> Can you check whether anything in the project uses Get All Bookings and Get Single Seat Status endpoints in the Booking controller and service? If nobody uses them, remove both endpoints and their associated methods.

**Resultado:** Se revisaron los consumidores del repositorio y se eliminaron los endpoints y métodos sin uso, además del DTO que quedó huérfano. Se compiló la solución para verificar los cambios.

## Integraciones y seguridad

### Protección de Booking con Keycloak

**Prompt recibido de una compañera:**
> Ya tenemos la librería `Shared.Authentication` en `Authorization/Shared.Authentication`. Conviene agregarla como referencia y utilizar `AddKeycloakAuthentication(builder.Configuration)` y `AddKeycloakAuthorization()` en lugar de configurar `AddJwtBearer` manualmente.
>
> Para el GET por ID que consume Availability, deberíamos usar `[Authorize(Policy = "CanReadBooking")]` para exigir el rol `booking.read`, no solamente un token válido.
>
> Para invocar Payment, Booking tendrá que obtener un JWT como `booking-service` y enviarlo a Payment. Para eso también tenemos listo `KeycloakTokenService`.
>
> Ajustá primero la referencia a la librería y la política del GET. Después probamos el endpoint con un token de `availability-service`, verificando que permita el acceso autorizado y rechace los demás.

**Prompt de seguimiento:**
> Can you verify whether Availability is sending a token and whether Booking implements token verification correctly?

**Resultado:** Booking se integró con `Shared.Authentication` y la política `CanReadBooking`. La revisión confirmó que Booking valida issuer, firma, expiración y audiencia, y exige el rol `booking.read`; el cliente HTTP de Availability, en cambio, no adjuntaba el JWT de servicio esperado.

### Monto y moneda del mensaje hacia el pago gRPC

**Prompt:**
> Availability is sending amount and currency now via Azure Service Bus. Can you verify that and tell me how to update Booking's side of the contract?
>
> What if I want those fields sent to gRPC but not included in the Booking database?
>
> Implement the changes: update the Booking contract to match Availability and replace the hardcoded values in the gRPC call.

**Resultado:** Se añadieron `amount` y `currency` al DTO del mensaje de Booking y se pasaron a Payment gRPC en lugar de los valores fijos. Se mantuvieron fuera de `BookingEntity`, por lo que no requirieron migración de base de datos. La compilación de Booking API fue exitosa.

### Seguimiento y consistencia eventual

**Prompt:**
> When I consult a booking's status, the screen shows “Pendiente.” That state is not in the Booking database. Where does it come from?

**Resultado:** Se rastreó el seguimiento de Availability: guarda la solicitud localmente y consulta Booking por el mismo ID. Cuando Booking devuelve 404 porque aún no hay registro, Availability presenta `Pendiente` como fallback; no es un estado almacenado en Booking.

### Diagnóstico del consumo de solicitudes

**Prompt:**
> I don't think the reservation reaches the Booking database. Can you check where the problem might be?

**Resultado:** Se identificaron verificaciones concretas para el flujo de punta a punta: conexión y topic de Service Bus, existencia y nombre de la suscripción, logs del worker, dead-letter queue y conexión a la base de Booking. El worker guarda la reserva antes de llamar a Payment, así que un fallo de pago aislado no explica la ausencia completa del registro.
