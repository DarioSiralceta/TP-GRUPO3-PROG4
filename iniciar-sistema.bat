
@echo off
setlocal
title Inicio del sistema de reservas

rem ==========================================
rem TP PROGRAMACION IV - GRUPO 3
rem Inicio automatizado del sistema
rem ==========================================

rem Las credenciales se cargan desde .env y User Secrets.
rem Trabajar siempre desde la raiz del repositorio.
pushd "%~dp0"

rem ==========================================
rem 1. VERIFICACION DE HERRAMIENTAS
rem ==========================================

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: No se encontro dotnet en PATH.
    goto :error
)

where docker >nul 2>&1
if errorlevel 1 (
    echo ERROR: No se encontro Docker en PATH.
    goto :error
)

if not exist "docker-compose.yml" (
    echo ERROR: Falta docker-compose.yml.
    goto :error
)

if not exist "Authorization\Keycloak\.env" (
    echo ERROR: Falta Authorization\Keycloak\.env.
    echo Ejecuta primero configurar-entorno.ps1.
    goto :error
)

if not exist "Booking\src\Booking.API\Booking.API.csproj" goto :faltanProyectos
if not exist "Availability\Availability.API\Availability.API.csproj" goto :faltanProyectos
if not exist "Availability\Availability.Web\Availability.Web.csproj" goto :faltanProyectos
if not exist "Payment\Payment.API\Payment.API.csproj" goto :faltanProyectos

rem ==========================================
rem 2. CERTIFICADO HTTPS DE DESARROLLO
rem ==========================================

echo.
echo Comprobando certificado HTTPS...

dotnet dev-certs https --check --trust >nul 2>&1
if errorlevel 1 (
    echo No se encontro un certificado HTTPS de desarrollo confiable.
    echo Se intentara configurarlo.
    dotnet dev-certs https --trust
    if errorlevel 1 (
        echo ERROR: No se pudo configurar el certificado HTTPS.
        goto :error
    )
)

echo OK: Certificado HTTPS verificado.

rem ==========================================
rem 3. INICIAR DOCKER
rem ==========================================

echo.
echo Comprobando Docker...

docker info >nul 2>&1
if not errorlevel 1 goto :dockerListo

if not exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe" (
    echo ERROR: Abri Docker Desktop y volve a ejecutar este archivo.
    goto :error
)

start "" "%ProgramFiles%\Docker\Docker\Docker Desktop.exe"

echo Esperando hasta 60 segundos a Docker Desktop...

powershell -NoProfile -Command "$limite=(Get-Date).AddSeconds(60); do { docker info *> $null; if ($LASTEXITCODE -eq 0) { exit 0 }; Start-Sleep -Seconds 2 } while ((Get-Date) -lt $limite); exit 1"

if errorlevel 1 (
    echo ERROR: Docker no esta listo.
    goto :error
)

:dockerListo

rem ==========================================
rem 4. INICIAR POSTGRESQL
rem ==========================================

echo.
echo Iniciando PostgreSQL...

docker compose -f "docker-compose.yml" up -d postgres
if errorlevel 1 goto :error

powershell -NoProfile -Command "$limite=(Get-Date).AddSeconds(60); do { docker exec postgres-booking pg_isready -U postgres -d booking_db *> $null; if ($LASTEXITCODE -eq 0) { exit 0 }; Start-Sleep -Seconds 2 } while ((Get-Date) -lt $limite); exit 1"

if errorlevel 1 (
    echo ERROR: PostgreSQL no respondio a tiempo.
    goto :error
)

echo OK: PostgreSQL disponible.

rem ==========================================
rem 5. INICIAR KEYCLOAK
rem ==========================================

echo.
echo Iniciando Keycloak...

docker compose -f "Authorization\Keycloak\docker-compose.yml" up -d
if errorlevel 1 goto :error

echo Esperando el realm de Keycloak en el puerto 8081...

powershell -NoProfile -Command "$limite=(Get-Date).AddSeconds(90); do { try { $r=Invoke-WebRequest -UseBasicParsing -Uri 'http://127.0.0.1:8081/realms/realm-tp-prog4/.well-known/openid-configuration' -TimeoutSec 3; if ($r.StatusCode -eq 200) { exit 0 } } catch {}; Start-Sleep -Seconds 2 } while ((Get-Date) -lt $limite); exit 1"

if errorlevel 1 (
    echo ERROR: Keycloak no esta listo.
    echo Verifica los logs y el puerto 8081.
    goto :error
)

echo OK: Keycloak disponible.

rem ==========================================
rem 6. MIGRACIONES DE BASES DE DATOS
rem ==========================================

echo.
echo Aplicando migraciones de bases de datos...

dotnet ef --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: No se encontro dotnet-ef.
    echo Instalar con: dotnet tool install --global dotnet-ef
    goto :error
)

echo.
echo Migraciones de Booking (PostgreSQL)...

dotnet ef database update --project "Booking\src\Booking.Infrastructure\Booking.Infrastructure.csproj" --startup-project "Booking\src\Booking.API\Booking.API.csproj"
if errorlevel 1 (
    echo ERROR: Fallaron las migraciones de Booking.
    goto :error
)

echo.
echo Migraciones de Availability (SQLite)...

dotnet ef database update --project "Availability\Availability.Infraestructure\Availability.Infraestructure.csproj" --startup-project "Availability\Availability.API\Availability.API.csproj"
if errorlevel 1 (
    echo ERROR: Fallaron las migraciones de Availability.
    goto :error
)

echo.
echo Migraciones de Payment (SQLite)...

dotnet ef database update --project "Payment\Payment.DAL\Payment.DAL.csproj" --startup-project "Payment\Payment.API\Payment.API.csproj"
if errorlevel 1 (
    echo ERROR: Fallaron las migraciones de Payment.
    goto :error
)

echo.
echo OK: Migraciones completadas correctamente.

rem ==========================================
rem 7. INICIAR MICROSERVICIOS
rem ==========================================

echo.
echo Iniciando microservicios...

rem Primero Payment porque Booking necesita gRPC.

call :iniciar "Payment.API" "Payment\Payment.API" 7016
if errorlevel 1 goto :error

call :iniciar "Booking.API" "Booking\src\Booking.API" 7296
if errorlevel 1 goto :error

call :iniciar "Availability.API" "Availability\Availability.API" 7004
if errorlevel 1 goto :error

call :iniciar "Availability.Web" "Availability\Availability.Web" 7165
if errorlevel 1 goto :error

rem ==========================================
rem 8. FINALIZACION
rem ==========================================

echo.
echo ==========================================
echo   SISTEMA INICIADO
echo ==========================================
echo.
echo Web MVC:       https://localhost:7165
echo Availability:  https://localhost:7004/swagger
echo Booking:       https://localhost:7296/swagger
echo Payment gRPC:  https://localhost:7016
echo Keycloak:      http://127.0.0.1:8081
echo.
echo Revisa las consolas por posibles errores.
echo Para detener los servicios, presiona Ctrl+C en cada consola.
echo Los contenedores se pueden detener desde Docker Desktop.
echo Auth.TestApi es opcional y no se inicia automaticamente.
echo.

start "" "https://localhost:7165"

popd
pause
exit /b 0

rem ==========================================
rem FUNCION PARA INICIAR CADA SERVICIO
rem ==========================================

:iniciar

echo.
echo Comprobando %~1...

powershell -NoProfile -Command "$c=New-Object Net.Sockets.TcpClient; try { $t=$c.ConnectAsync('localhost',%~3); if ($t.Wait(1000) -and $c.Connected) { exit 0 }; exit 1 } catch { exit 1 } finally { $c.Dispose() }"

if not errorlevel 1 (
    echo Puerto %~3 ocupado: se conserva el proceso existente.
    echo Verifica que corresponda a %~1.
    exit /b 0
)

start "%~1" cmd /k "dotnet run --project %~2 --launch-profile https"

echo Esperando %~1 en el puerto %~3...

powershell -NoProfile -Command "$limite=(Get-Date).AddSeconds(90); do { $c=New-Object Net.Sockets.TcpClient; try { $t=$c.ConnectAsync('localhost',%~3); if ($t.Wait(1000) -and $c.Connected) { exit 0 } } catch {} finally { $c.Dispose() }; Start-Sleep -Seconds 2 } while ((Get-Date) -lt $limite); exit 1"

if errorlevel 1 (
    echo ERROR: %~1 no inicio a tiempo.
    echo Revisa su consola.
    echo Las aplicaciones ya abiertas siguen ejecutandose.
    exit /b 1
)

echo OK: %~1 disponible.
exit /b 0

rem ==========================================
rem MANEJO DE ERRORES
rem ==========================================

:faltanProyectos
echo ERROR: Faltan proyectos.
echo Verifica que el BAT este en la raiz del repositorio.
goto :error

:error
echo.
echo No se pudo completar el inicio.
echo Revisa el mensaje anterior.
echo No se eliminaron datos ni se detuvieron procesos existentes.

popd
pause
exit /b 1
