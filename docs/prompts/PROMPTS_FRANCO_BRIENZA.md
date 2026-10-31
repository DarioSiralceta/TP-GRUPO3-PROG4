# Historial de prompts — Franco Brienza

## Booking REST API — endpoints de estado de asientos

Rama: `feature/booking-seat-status`
Herramienta: GitHub Copilot

### 1. DTO de estado de asiento

> Crea el DTO con eventID tipo int SeatId tipo int isOccupied tipo booleano y status que es tipo string

### 2. DTO de estado de asiento (corrección de tipos)

> Crea el DTO con eventID tipo string SeatId tipo string isOccupied tipo booleano y status que es tipo string

### 3. Firma de GetSeatStatus en la interfaz

> Agrega a IBookingService GetSeatStatus que recibe eventId y seatId y devuelve un SeatStatusDto

### 4. Firma de GetOccupiedSeats en la interfaz

> Agrega a IBookingService GetOccupiedSeats que recibe eventid y devuelve la lista de string con los id de los asientos ocupados

### 5. Explicación del código generado

> Explicame las funciones extra que creaste.

### 6. Endpoints en el controller

> Agregá 2 metodos Getseatstatus y Getoccupuedseats

## Booking — cliente gRPC real de Payment

Rama: `feature/booking-payment-grpc`
Herramienta: GitHub Copilot

### 7. Cliente gRPC de Payment con token de Keycloak

> Tenemos que hacer que Booking llame de verdad a ProcessPayment. Para eso hace que llame a Payment por gRPC usando Paymentserviceclient. Hay que pedir un token con keycloacktokenservice con los clientid y secret de la config.
>
> la currency es ARS
> Si Payment devuelve Success es ok, si es Declined "Pago rechazado", si es Unavailable o falla la llamada "Pago no disponible".
> NO cambies IPaymentServiceClient.

## Script start-all.sh — chequeos antes de levantar el stack

Rama: `develop`
Herramienta: Claude Code

### 8. Mejora de start-all.sh

> Mejorá start-all.sh para que antes de levantar todo revise lo que suele fallar y diga qué falta en vez de esconder errores:
> - que estén dotnet, dotnet-ef, curl y Docker corriendo
> - que el .env de Keycloak tenga todos los secretos
> - que estén los user secrets de Booking y Availability
> - que los puertos de los servicios estén libres
> - si ya hay un Postgres en el 5432 que no levante el contenedor
