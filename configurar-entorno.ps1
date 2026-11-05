
# ==========================================
# TP PROGRAMACION IV - GRUPO 3
# Configuracion inicial del entorno
# ==========================================

$ErrorActionPreference = "Stop"

# Ubicarnos en la raiz del repositorio.
Set-Location $PSScriptRoot

Write-Host ""
Write-Host "========================================="
Write-Host "  CONFIGURACION INICIAL - GRUPO 3"
Write-Host "========================================="
Write-Host ""

# Verificar .NET
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: No se encontro .NET SDK." -ForegroundColor Red
    exit 1
}

Write-Host "OK: .NET SDK instalado." -ForegroundColor Green

# Verificar Docker
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: No se encontro Docker." -ForegroundColor Red
    exit 1
}

Write-Host "OK: Docker instalado." -ForegroundColor Green

# Verificar que Docker Desktop este funcionando
docker info *> $null

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Docker Desktop no esta iniciado." -ForegroundColor Red
    exit 1
}

Write-Host "OK: Docker Desktop funcionando." -ForegroundColor Green

# Verificar Entity Framework CLI
dotnet ef --version *> $null

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Falta dotnet-ef." -ForegroundColor Red
    Write-Host "Instalar con: dotnet tool install --global dotnet-ef"
    exit 1
}

Write-Host "OK: Entity Framework CLI instalado." -ForegroundColor Green
Write-Host ""
Write-Host "Verificacion inicial completada." -ForegroundColor Cyan


# ==========================================
# PASO 2 - CONFIGURACION DE KEYCLOAK
# ==========================================

Write-Host ""
Write-Host "Verificando configuracion de Keycloak..." -ForegroundColor Cyan

$envPath = Join-Path $PSScriptRoot "Authorization\Keycloak\.env"
$examplePath = Join-Path $PSScriptRoot "Authorization\Keycloak\.env.example"

# Si no existe el archivo .env, lo creamos desde la plantilla.
if (-not (Test-Path $envPath)) {

    if (-not (Test-Path $examplePath)) {
        Write-Host "ERROR: No se encontro .env.example de Keycloak." -ForegroundColor Red
        exit 1
    }

    Copy-Item $examplePath $envPath

    Write-Host "Se creo el archivo .env de Keycloak." -ForegroundColor Yellow
    Write-Host "Completa sus credenciales y ejecuta nuevamente este script."
    exit 0
}

Write-Host "OK: Archivo .env encontrado." -ForegroundColor Green

# Variables obligatorias para iniciar Keycloak.
$requiredVariables = @(
    "KEYCLOAK_ADMIN_PASSWORD",
    "BOOKING_CLIENT_SECRET",
    "AVAILABILITY_CLIENT_SECRET",
    "MVC_CLIENT_SECRET",
    "GOOGLE_CLIENT_ID",
    "GOOGLE_CLIENT_SECRET"
)

# Leer las variables sin mostrar sus valores.
$envValues = @{}

foreach ($line in Get-Content $envPath) {

    if ($line -match '^\s*([A-Z_]+)\s*=(.*)$') {
        $name = $matches[1]
        $value = $matches[2].Trim()
        $envValues[$name] = $value
    }
}

$missingVariables = @()

foreach ($variable in $requiredVariables) {

    if (
        -not $envValues.ContainsKey($variable) -or
        [string]::IsNullOrWhiteSpace($envValues[$variable]) -or
        $envValues[$variable] -eq "cambiar_contrasena"
    ) {
        $missingVariables += $variable
    }
}

if ($missingVariables.Count -gt 0) {

    Write-Host "Faltan variables de Keycloak:" -ForegroundColor Yellow

    foreach ($variable in $missingVariables) {
        Write-Host " - $variable"
    }

    Write-Host ""
    Write-Host "Completa el archivo Authorization\Keycloak\.env"
    exit 1
}

Write-Host "OK: Las variables de Keycloak estan completas." -ForegroundColor Green


# ==========================================
# PASO 3 - USER SECRETS DE KEYCLOAK
# ==========================================

Write-Host ""
Write-Host "Configurando User Secrets de Keycloak..." -ForegroundColor Cyan

# Relacion entre variables del .env y proyectos .NET.
$projects = @(
    @{
        Name = "Availability.Web"
        Path = "Availability/Availability.Web/Availability.Web.csproj"
        EnvVariable = "MVC_CLIENT_SECRET"
    },
    @{
        Name = "Availability.API"
        Path = "Availability/Availability.API/Availability.API.csproj"
        EnvVariable = "AVAILABILITY_CLIENT_SECRET"
    },
    @{
        Name = "Booking.API"
        Path = "Booking/src/Booking.API/Booking.API.csproj"
        EnvVariable = "BOOKING_CLIENT_SECRET"
    }
)

foreach ($project in $projects) {

    $projectPath = Join-Path $PSScriptRoot $project.Path

    if (-not (Test-Path $projectPath)) {
        Write-Host "ERROR: No existe el proyecto $($project.Name)" -ForegroundColor Red
        exit 1
    }

    # Capturamos la configuracion sin imprimir sus valores.
    $existingSecrets = @(
        dotnet user-secrets list --project $projectPath 2>&1
    )

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: No se pudieron consultar los User Secrets de $($project.Name)." -ForegroundColor Red
        exit 1
    }

    $alreadyConfigured = @(
        $existingSecrets | Where-Object {
            $_ -match '^Keycloak:ClientSecret\s*='
        }
    ).Count -gt 0

    if ($alreadyConfigured) {
        Write-Host "OK: $($project.Name) ya tiene su Client Secret. Se conserva." -ForegroundColor Green
        continue
    }

    $secretValue = $envValues[$project.EnvVariable]

    # Guardar el secreto localmente, sin imprimir su valor.
    $null = & dotnet user-secrets set `
        "Keycloak:ClientSecret" `
        $secretValue `
        --project $projectPath 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: No se pudo configurar $($project.Name)." -ForegroundColor Red
        exit 1
    }

    Write-Host "OK: Client Secret configurado en $($project.Name)." -ForegroundColor Green
}

Write-Host ""
Write-Host "Configuracion de User Secrets de Keycloak completada." -ForegroundColor Cyan



# ==========================================
# PASO 4 - AZURE SERVICE BUS
# ==========================================

Write-Host ""
Write-Host "Verificando Azure Service Bus..." -ForegroundColor Cyan

$availabilityProject = Join-Path $PSScriptRoot "Availability/Availability.API/Availability.API.csproj"
$bookingProject = Join-Path $PSScriptRoot "Booking/src/Booking.API/Booking.API.csproj"

function Test-UserSecret {
    param(
        [string]$ProjectPath,
        [string]$SecretName
    )

    $secrets = @(dotnet user-secrets list --project $ProjectPath 2>&1)

    if ($LASTEXITCODE -ne 0) {
        throw "No se pudieron consultar los User Secrets."
    }

    $escapedName = [regex]::Escape($SecretName)

    return @(
        $secrets | Where-Object {
            $_ -match "^$escapedName\s*=\s*\S+"
        }
    ).Count -gt 0
}

$key = "AzureServiceBus:ConnectionString"

$availabilityHasConnection = Test-UserSecret $availabilityProject $key
$bookingHasConnection = Test-UserSecret $bookingProject $key

if ($availabilityHasConnection) {
    Write-Host "OK: Availability ya tiene Connection String." -ForegroundColor Green
}

if ($bookingHasConnection) {
    Write-Host "OK: Booking ya tiene Connection String." -ForegroundColor Green
}

if (-not $availabilityHasConnection -or -not $bookingHasConnection) {

    Write-Host "Falta configurar Azure Service Bus en uno o mas proyectos."
    Write-Host "La credencial no se mostrara mientras escribis."

    $secureConnection = Read-Host "Connection String de Azure Service Bus" -AsSecureString

    $connectionString = [System.Net.NetworkCredential]::new(
        "",
        $secureConnection
    ).Password

    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        throw "La Connection String no puede estar vacia."
    }

    if (-not $availabilityHasConnection) {
        $null = & dotnet user-secrets set $key $connectionString `
            --project $availabilityProject 2>&1

        if ($LASTEXITCODE -ne 0) {
            throw "Error configurando Azure Service Bus en Availability."
        }

        Write-Host "OK: Availability configurado." -ForegroundColor Green
    }

    if (-not $bookingHasConnection) {
        $null = & dotnet user-secrets set $key $connectionString `
            --project $bookingProject 2>&1

        if ($LASTEXITCODE -ne 0) {
            throw "Error configurando Azure Service Bus en Booking."
        }

        Write-Host "OK: Booking configurado." -ForegroundColor Green
    }

    Remove-Variable connectionString -ErrorAction SilentlyContinue
}

# Availability utiliza este topico para publicar solicitudes.
$topicKey = "AzureServiceBus:TopicName"

if (-not (Test-UserSecret $availabilityProject $topicKey)) {

    $null = & dotnet user-secrets set $topicKey "grupo3-booking-topic" `
        --project $availabilityProject 2>&1

    if ($LASTEXITCODE -ne 0) {
        throw "Error configurando el TopicName de Availability."
    }

    Write-Host "OK: TopicName configurado en Availability." -ForegroundColor Green
}
else {
    Write-Host "OK: Availability ya tiene TopicName." -ForegroundColor Green
}

Write-Host ""
Write-Host "Configuracion de Azure Service Bus completada." -ForegroundColor Cyan
