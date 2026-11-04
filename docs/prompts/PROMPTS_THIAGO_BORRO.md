# Historial de prompts — Thiago Borro

Proyecto: TP Programación IV — `TP-GRUPO3-PROG4`
Componente: `Availability.Web` — ASP.NET Core MVC (.NET 8)
Herramienta: ChatGPT

Nota: recopilación basada en las conversaciones disponibles. Los prompts de las secciones de desarrollo están resumidos y reformulados; no constituyen una transcripción literal ni un historial exhaustivo. La última sección conserva mensajes textuales disponibles. La numeración agrupa las consultas por tema, no por fecha exacta.

## Availability.Web — arquitectura y estructura inicial

Rama: `feature/availability`
Herramienta: ChatGPT

### 1. Organización de Availability

> ¿Availability va a tener solo frontend o también backend? Si me toca desarrollar esta parte, ¿qué debería hacer y cómo puedo comenzar?

### 2. Elección de MVC

> Quiero desarrollar la interfaz de Availability con ASP.NET Core MVC y que su backend se comunique con la API que está haciendo mi compañero. ¿Cómo organizamos el proyecto?

### 3. Estructura de la solución

> Ayudame a organizar Availability.Web dentro de la carpeta Availability y a agregarlo a la solución. ¿Dónde van los modelos, las vistas, los controladores y los servicios?

## Availability.Web — eventos, asientos y solicitudes

Rama: `feature/availability`
Herramienta: ChatGPT

### 4. Pantalla de inicio

> Quiero agregar una pantalla de inicio con una descripción del sistema y un botón «Ver eventos» que lleve al listado de eventos.

### 5. Listado de eventos y asientos

> Ayudame a mostrar los eventos y a navegar a los asientos del evento seleccionado, enviando su identificador al controlador correspondiente.

### 6. Solicitar reserva desde un asiento

> Quiero mostrar el asiento, su estado y una acción para solicitar una reserva. El botón debe estar disponible cuando el asiento esté libre y enviar el identificador del evento y el número de asiento.

### 7. Controlador de reservas

> ¿Cómo debería quedar ReservasController y la vista Solicitar? Necesito validar el evento y buscar el asiento seleccionado.

### 8. Servicios con datos de prueba

> ¿Cómo registro EventosService, AsientosService y SolicitudesService en Program.cs para que las pantallas funcionen con datos de prueba mientras esperamos la integración?

## Availability.Web — auditoría

Rama: `feature/availability`
Herramienta: ChatGPT

### 9. Middleware de auditoría

> Tenemos que agregar logs de auditoría para identificar el inicio y el fin de la cadena. ¿Dónde creo AuditoriaMiddleware y cómo lo agrego al pipeline de Program.cs?

### 10. Seguimiento de las solicitudes

> Ayudame a incorporar registros de auditoría en el flujo de reservas y a mantener un identificador de correlación para poder seguir la solicitud entre los servicios.

## Availability.Web — autenticación y autorización

Rama: `feature/availability`
Herramienta: ChatGPT

### 11. Incorporación del login

> Quiero continuar con el login de Availability.Web e integrar la autenticación que está preparando el equipo. ¿Qué cambios necesito en Program.cs y en los controladores?

### 12. Callback de OpenID Connect

> Me piden la URL de callback de Availability.Web. ¿Para qué sirve https://localhost:7165/signin-oidc y qué URL tengo que enviar?

### 13. Configuración del cliente

> Si el cliente availability-web requiere Client Secret, ¿qué cambia respecto de configurarlo sin secreto?

### 14. Integración de la rama de autenticación

> Mi compañera subió la autenticación a su rama. ¿Cómo incorporo esos cambios a feature/availability y resuelvo los conflictos de la solución?

## Availability.Web — integración con Availability.API

Rama: `feature/availability`
Herramienta: ChatGPT

### 15. Contratos y endpoints necesarios

> Mi compañero está desarrollando Availability.API. ¿Qué endpoints tengo que pedirle para consultar eventos, consultar asientos y solicitar una reserva? ¿Podemos avanzar mientras los termina?

### 16. Sustitución de los datos simulados

> Ayudame a conectar los servicios de Availability.Web con Availability.API mediante HTTP para reemplazar los datos de prueba y enviar las solicitudes de reserva.

### 17. Token de acceso

> ¿Cómo envío el token del usuario autenticado en las llamadas a la API para que las operaciones protegidas funcionen?

### 18. Estado de los asientos y errores

> Revisemos cómo se actualiza la disponibilidad después de solicitar una reserva y cómo mostrar los errores que devuelve la API en la interfaz.