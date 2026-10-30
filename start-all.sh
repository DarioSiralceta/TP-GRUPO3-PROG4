#!/bin/bash

set -e

echo "🚀 Iniciando el stack de microservicios locales..."

# 1. Asegurar que exista el archivo .env para Keycloak
if [ ! -f Authorization/Keycloak/.env ] && [ -f Authorization/Keycloak/.env.example ]; then
    echo "📋 Copiando .env.example de Keycloak..."
    cp Authorization/Keycloak/.env.example Authorization/Keycloak/.env
fi

# 2. Levantamiento de contenedores Docker (Keycloak y bases de datos)
echo "🐳 Levantando contenedores de Docker..."
docker compose -f Authorization/Keycloak/docker-compose.yml up -d
docker compose up -d 2>/dev/null || true

echo "⏳ Esperando 5 segundos para la inicialización de contenedores..."
sleep 5

# 3. Variables de entorno para Auth.TestApi
export Keycloak__Authority="http://127.0.0.1:8081/realms/realm-tp-prog4"
export Keycloak__Audience="payment-api"

# 4. Iniciar microservicios .NET en paralelo
echo "⚡ Iniciando Microservicios .NET..."

dotnet run --project Booking/src/Booking.API &
dotnet run --project Availability/Availability.API &
dotnet run --project Payment/Payment.API &
dotnet run --project Authorization/Auth.TestApi/Auth.TestApi.csproj --launch-profile http &
dotnet run --project Availability/Availability.Web/Availability.Web.csproj --launch-profile https &

# Interceptar Ctrl+C (SIGINT/SIGTERM) para detener todos los procesos dotnet en segundo plano
trap "trap - SIGTERM && kill -- -$$" SIGINT SIGTERM EXIT

# Mantener el script activo para consolidar los logs en la terminal
wait
