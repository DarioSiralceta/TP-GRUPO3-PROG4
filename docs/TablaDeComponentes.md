### Lautaro Heinse
## `TablaDeComponentes.md`
Este archivo completa el **Punto 1** de la consigna del TP, incluyendo todos los componentes del sistema
(arquitectura, broker, auth y el servicio de notificaciones diseñado por el Grupo E).

# Tabla de Interpretación del Diagrama de Arquitectura

| Componente | Rol | Justifique la participación en el sistema |
| :---	| :--- | :--- |
| **Public IP Addresses / Clientes** | Punto de acceso externo | Permite a los usuarios conectarse a la interfaz pública (Availability Site) para consultar eventos y realizar solicitudes de reserva|
| **Availability Site** | Puerta de entrada pública (Frontend/API) | Desarrollado con **Clean Architecture** y dominio anémico. Permite la consulta pública de disponibilidad sin login y expone el punto de entrada para iniciar una reserva, publicando el evento en el Message Broker|
| **Message Broker (Azure Service Bus)** | Desacoplamiento y comunicación asíncrona | Canalizado por código utilizando el SDK provisto por la cátedra. Permite encolar las solicitudes de reserva de forma asíncrona entre Availability y Booking, garantizando la escalabilidad y resiliencia|
| **Booking REST API** | Orquestador de reservas y máquina de estados | Desarrollado en **N-Layer** con dominio anémico. Concentra la lógica más compleja: maneja la concurrencia de asientos, procesa el vencimiento de retenciones y coordina el cobro síncrono|
| **Payment gRPC API** | Procesamiento de cobros de alta performance | Servidor **gRPC** implementado en **N-Layer**. Simula un proveedor de pagos (mock configurable para happy path y fallos) respondiendo de manera eficiente mediante protocolos binarios sobre HTTP/2|
| **Authorization Server** | Gestión de identidad y seguridad | Emisor de tokens JWT (basado en ASP.NET Core Identity o Duende). Proporciona autenticación de usuarios finales y autenticación servicio a servicio (*client credentials*) para asegurar los microservicios|
| **3rd party Identity providers** | Proveedores de federación externos | Proveen soporte opcional de autenticación externa (OAuth2/OIDC) integrada al servidor de autorización principal|
| **Bases de datos SQL (SQL Server / LocalDB)** | Persistencia transaccional independiente | Cada microservicio (Availability, Booking, Payment y Auth) cuenta con su propia base de datos relacional aislada, cumpliendo con el patrón *Database-per-service* de microservicios|
| **Servicio de Notificaciones** | Informar resultados al usuario | Componente añadido exclusivamente a nivel de diseño y diagrama (sin implementación en código). Se suscribe al evento de éxito del broker para notificar al usuario cuando su reserva ha sido confirmada tras el pago|