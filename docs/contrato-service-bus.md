# Contrato inicial de Azure Service Bus

Este documento registra el acuerdo inicial entre Availability y Booking para
transportar solicitudes de reserva. No contiene credenciales.

## Valores acordados

| Elemento | Valor |
|---|---|
| Tipo de entidad | Topic |
| Topic | `grupo3-booking-topic` |
| Suscripción que consume Booking | `booking` |
| Campos del mensaje | `RequestId`, `EventId`, `SeatId` |

El topic y la suscripción fueron probados por el equipo. La connection string se
configura localmente y no se incluye aquí ni en archivos versionados.

## Mensaje

La solicitud contiene un identificador único de tipo GUID y los identificadores
del evento y del asiento. En la implementación actual de Availability, estos dos
últimos campos son números enteros. El mensaje serializado con `System.Text.Json`
tiene esta forma:

```json
{
  "RequestId": "00000000-0000-0000-0000-000000000001",
  "EventId": 12,
  "SeatId": 34
}
```

Los valores del ejemplo son ficticios. Booking acepta `EventId` y `SeatId` como
número o texto; para evitar diferencias futuras, Availability y Booking deberían
mantener los tipos acordados y revisar cualquier cambio del mensaje juntos.

## Recorrido esperado

1. Availability crea un `RequestId` y publica el mensaje en
   `grupo3-booking-topic`.
2. Booking recibe el mensaje desde la suscripción `booking`.
3. Booking procesa la solicitud y confirma el mensaje a Service Bus solo después
   de terminar correctamente. Si hay un error, debe aplicar la política de reintento
   o de mensaje no procesable definida por su consumidor.

## Pendiente de validar durante la integración

- Que la ruta HTTP de Availability invoque el caso de uso y publique el mensaje.
- Que los nombres y los tipos del mensaje publicado coincidan con el DTO que
  Booking deserializa.
- Que Booking inicie el consumidor con la misma suscripción `booking`.
- Probar en conjunto una publicación y recepción reales, siguiendo el mismo
  `RequestId` en los logs.
- Acordar por separado el evento de reserva confirmada que necesitaría el diseño
  futuro de Notificaciones. No forma parte de este mensaje de solicitud.
