# Sistema de reserva de asientos

Trabajo práctico de Programación IV. La solución está pensada como un sistema de
microservicios para consultar disponibilidad y reservar asientos para eventos.

## Estado actual del repositorio

Este repositorio está en una etapa inicial: por ahora solo está creado el esqueleto
de `Availability` en .NET 8. Booking, Payment y Authorization todavía no están
implementados. La API de Availability aún conserva el ejemplo `WeatherForecast`;
todavía no implementa el flujo de reservas ni está conectada a Azure Service Bus.

Por eso, los pasos siguientes sirven para ejecutar y comprobar el esqueleto actual.
No significan que el flujo completo de reservas ya esté disponible.

## Requisitos

- .NET SDK 8.
- No se necesita Docker para ejecutar el proyecto actual.

SQL Server/LocalDB y Azure Service Bus serán necesarios cuando se incorporen las
funciones que los usan. En este momento el código no configura ni usa esos servicios.

## Ejecutar Availability

Desde la carpeta raíz del repositorio, abrir una terminal y ejecutar:

```powershell
dotnet run --project .\Availability\Availability.API\Availability.API.csproj --launch-profile http
```

La API estará disponible en `http://localhost:5057`. En el estado actual se puede
probar el endpoint de ejemplo en `http://localhost:5057/weatherforecast`.

Para compilar la solución:

```powershell
dotnet build .\Availability.sln
```

## Configuración local y secretos

No guardar contraseñas ni connection strings reales en archivos versionados.
La connection string de Azure Service Bus debe configurarse localmente (por
ejemplo, con .NET User Secrets o una variable de entorno) y nunca subirse al
repositorio ni compartirse en mensajes.

## Contrato inicial de Azure Service Bus

El equipo confirmó el topic `grupo3-booking-topic`, la suscripción de Booking
`booking` y los campos `RequestId`, `EventId` y `SeatId` para la solicitud de
reserva. El contrato y los puntos técnicos que aún deben validarse están en
[`docs/contrato-service-bus.md`](./docs/contrato-service-bus.md).

La confirmación de los nombres no significa que el flujo esté integrado en la
rama principal: los cambios de Availability y Booking todavía deben incorporarse
y probarse juntos.

## Componentes previstos

- **Availability:** consulta de disponibilidad y recepción de solicitudes.
- **Booking:** procesamiento de solicitudes y estado de las reservas.
- **Payment:** procesamiento del pago mediante gRPC.
- **Authorization:** emisión y validación de tokens.
- **Azure Service Bus:** transporte asíncrono de mensajes entre servicios.
- **Notificaciones:** componente previsto en el diseño; no se implementa en código.

El diseño inicial del componente de Notificaciones está en
[`docs/notificaciones.md`](./docs/notificaciones.md).
