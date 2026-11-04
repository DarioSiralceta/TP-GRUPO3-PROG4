### Prompts IA: Lautaro Heinse.

**Usado en "1. estructura de mensaje (json): [FormatoLogsAuditoria.md]"** 

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

**Usados para crear ejemplos de diseño de diagramas. del SERVICIO DE NOTIFICACIONES y de la Zona de consistencia eventual.**
 
Diseña un diagrama de arquitectura de software técnico, vectorial, limpio y de aspecto profesional sobre un fondo blanco nítido, que ilustre el flujo asíncrono del Servicio de Notificaciones para un sistema de reservas en la nube de Azure.
El diagrama debe organizarse estrictamente en un flujo secuencial horizontal de izquierda a derecha, mostrando los siguientes componentes interconectados mediante flechas numeradas del 1 al 5:

    "Booking REST API" (.NET 8): Nodo inicial que emite el evento asíncrono ReservaConfirmadaEvent tras una transacción exitosa.

    "Azure Service Bus" (Message Broker): Componente equipado con el icono oficial de Azure, que recibe el mensaje en el tópico grupo3-booking-topic y la suscripción booking.

    "Notification Service" (Consumidor Asíncrono - Pub/Sub): Microservicio destacado visualmente con una etiqueta secundaria que indique "Solo Diseño / Consumer Asincrónico".

    "Azure Communication Services": Servicio en la nube provisto de su icono oficial de Azure, encargado de procesar el envío a canales externos (Email / SMS).

    "Usuario Final": Representación gráfica del cliente recibiendo las notificaciones confirmadas en sus dispositivos.


Diseña un diagrama de arquitectura general de microservicios para un sistema distribuido de reservas de asientos desarrollado en .NET 8, presentado en un estilo vectorial, limpio y profesional sobre un fondo blanco nítido.
El diagrama debe ilustrar la arquitectura completa y las interconexiones de los siguientes componentes organizados de forma lógica (separando capas de presentación, procesamiento, persistencia y seguridad):

    Cliente Web / Public IP: Punto de entrada del usuario final que accede al sistema.

    Availability Site (Clean Architecture): Aplicación frontend/web conectada a su propia base de datos aislada (AvailabilityDb en SQL Server) bajo el patrón Database-per-service.

    Authorization Server (Keycloak): Servidor de identidad basado en OAuth2 / OpenID Connect / JWT, emitiendo tokens de autenticación hacia los microservicios.

    Azure Service Bus (Message Broker): Tópico y suscripción (grupo3-booking-topic) que desacopla de forma asíncrona las solicitudes entre Availability y Booking.

    Booking REST API (N-Layer): Microservicio central con su propia base de datos aislada (BookingDb), encargado de gestionar la máquina de estados y las reservas.

    Payment gRPC API (N-Layer): Microservicio de cobros con su propia base de datos (PaymentDb), conectado de manera síncrona mediante gRPC sobre HTTP/2 desde Booking REST API.

    Servicio de Notificaciones (Consumidor Asíncrono): Componente secundario suscrito al broker mediante el patrón Pub/Sub para procesar notificaciones en segundo plano.

Prompt usado para acomodar la informacion de README.md en base a los demas hechos por el equipo (poner la informacion al dia)

Utilizando como referencia todos los archivos README.md individuales de cada microservicio, los documentos complementarios y
la estructura real del repositorio,
audita y actualiza la información del README.md principal.
Mantén estrictamente la estructura de secciones existente en el README.md Principal entregado, corrigiendo únicamente las inconsistencias de datos, 
rutas de archivos, comandos de configuración, puertos y scripts de ejecución para que reflejen el estado actual del proyecto.