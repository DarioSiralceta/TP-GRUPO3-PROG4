### Lautaro Heinse
# Estándar de Logs de Auditoría

Este documento define el formato obligatorio de logging estructurado que deben implementar todos los microservicios 
del sistema de reserva de asientos (Availability, Booking y Payment) para garantizar la observabilidad y trazabilidad end-to-end.

## 1. Principios Clave
**Trace ID / Correlation ID:** Cada solicitud debe generar un identificador único (GUID) 
que acompañe a la transacción a través de llamadas HTTP, 
mensajes en Azure Service Bus y llamadas gRPC.

* **Inicio y fin de la cadena** Es obligatorio registrar explícitamente el inicio y el fin de cada suceso en cada microservicio.

* **Nivel de Log:** 
	 * Information: Eventos de auditoría del negocio (p. ej., recepción de solicitud, cambio de estado, cobro exitoso). 
	 * Warning / Error: Fallos en la cadena (p. ej., pago rechazado, asiento ocupado, indisponibilidad del servicio de cobro)
----
## 2. Estructura del Mensaje (JSON)

Los logs deben registrarse utilizando un formato estructurado
compatible con el logger nativo de .NET; que incluya los siguientes campos:

# json
{
  "Timestamp": "2026-10-27T14:30:00.123Z",
  "Level": "Information",
  "Service": "Availability.Site",
  "TraceId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "EventType": "START_CHAIN", O "END_CHAIN"
  "Action": "SolicitarReserva",
  "UserId": "user_12345",
  "Details": "Inicia el proceso de solicitud de reserva para el asiento `X` del evento `Y`."
}