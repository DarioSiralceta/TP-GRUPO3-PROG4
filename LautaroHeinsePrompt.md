### Prompts IA: Lautaro Heinse.

*Usado en "1. estructura de mensaje (json): [FormatoLogsAuditoria.md]"* 

Necesito que me generes un objeto JSON estrictamente estructurado para un log de auditoría.
Debe cumplir exactamente con las siguientes especificaciones:
* Timestamp: Fecha y hora actual en formato 27-10-2026T. pero con horario aleatorio
* Level: Valor "Information".
* Service: Cadena de texto con el nombre del servicio.
* TraceId: Un identificador único global en formato GUID v4.
* EventType: Debe ser exactamente "START_CHAIN" o "END_CHAIN".
* Action: Nombre de la acción realizada
* UserId: Identificador del usuario
* Details: Mensaje descriptivo indicando el inicio o fin del proceso, 
  especificando un número de asiento `X` y un número de evento `Y`.

Devuelve ÚNICAMENTE el objeto JSON dentro de un bloque de código markdown, 
sin ningún texto explicatorio antes ni después del msj del json.