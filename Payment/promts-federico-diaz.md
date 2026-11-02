# Historial de prompts de IA — Federico Diaz

## Prompt 1

crea el proyecto Payment con la siguiente estructura:

Payment/
├── Payment.API/
│   ├── Protos/payment.proto
│   ├── Services/PaymentGrpcService.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── Payment.API.csproj
├── Payment.BLL/
│   ├── Services/PaymentService.cs
│   ├── Interfaces/IPaymentService.cs
│   └── Payment.BLL.csproj
├── Payment.DAL/
│   ├── Context/PaymentDbContext.cs
│   ├── Repositories/PaymentRepository.cs
│   ├── Interfaces/IPaymentRepository.cs
│   ├── Migrations/
│   └── Payment.DAL.csproj
└── Payment.Domain/
    ├── Entities/PaymentAttempt.cs
    ├── Enums/PaymentStatus.cs
    └── Payment.Domain.csproj

debe seguir el patron de arquitectura N-Layer con una api, business logic layer, data access layer y domain. Agrega las referencias que hagan falta al .sln raiz

## Prompt 2

Edita la interface IProcessPaymentAsync y su implementacion para que reciba por parametros los datos de booking (string bookingId, string userId, decimal amount, string currency).
Agrega al proyecto las dependencias necesarias para implementar un DbContext que use sqlite.
El metodo ProcessPaymentAsync se debe encargar de crear el PaymentAttempt y persistir cada intento en caso de fallo.

## Prompt 3

Implementa PaymentGrpcService para que llame a IPaymentService.ProcessPaymentAsync con los datos del request que se recibe de booking y traduzca el PaymentAttempt devuelto a ProcessPaymentResponse (incluyendo transaction_id = attempt.Id.ToString()). Cuando el resultado del cobro no es exitoso, PaymentGrpcService se tiene que encargar de mapear el campo error_message con un texto correspondiente segun el codigo del status

