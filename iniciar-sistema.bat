@echo off
setlocal
title Inicio del sistema de reservas
rem Guardar este archivo en la raiz del repositorio.
rem Las credenciales se cargan desde .env y User Secrets existentes.
pushd "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: No se encontro dotnet en PATH.
    goto :error
)
where docker >nul 2>&1
if errorlevel 1 (
    echo ERROR: No se encontro Docker Desktop en PATH.
    goto :error
)
if not exist "docker-compose.yml" (
    echo ERROR: Coloca este BAT junto al docker-compose.yml del repositorio.
    goto :error
)
if not exist "Authorization\Keycloak\.env" (
    echo ERROR: Falta Authorization\Keycloak\.env con las credenciales locales.
    goto :error
)
if not exist "Booking\src\Booking.API\Booking.API.csproj" goto :faltanProyectos
if not exist "Availability\Availability.API\Availability.API.csproj" goto :faltanProyectos
if not exist "Availability\Availability.Web\Availability.Web.csproj" goto :faltanProyectos

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
    echo ERROR: Docker no esta listo. Espera a que indique Engine running.
    goto :error
)

:dockerListo
echo Iniciando PostgreSQL...
docker compose -f "docker-compose.yml" up -d postgres
if errorlevel 1 goto :error
powershell -NoProfile -Command "$limite=(Get-Date).AddSeconds(60); do { docker exec postgres-booking pg_isready -U postgres -d booking_db *> $null; if ($LASTEXITCODE -eq 0) { exit 0 }; Start-Sleep -Seconds 2 } while ((Get-Date) -lt $limite); exit 1"
if errorlevel 1 (
    echo ERROR: PostgreSQL no respondio a tiempo. Revisa Docker Desktop.
    goto :error
)

echo Iniciando Keycloak...
docker compose -f "Authorization\Keycloak\docker-compose.yml" up -d
if errorlevel 1 goto :error
echo Esperando el realm de Keycloak en el puerto 8081...
powershell -NoProfile -Command "$limite=(Get-Date).AddSeconds(90); do { try { $r=Invoke-WebRequest -UseBasicParsing -Uri 'http://127.0.0.1:8081/realms/realm-tp-prog4/.well-known/openid-configuration' -TimeoutSec 3; if ($r.StatusCode -eq 200) { exit 0 } } catch {}; Start-Sleep -Seconds 2 } while ((Get-Date) -lt $limite); exit 1"
if errorlevel 1 (
    echo ERROR: Keycloak no esta listo. Verifica sus logs y el puerto 8081.
    goto :error
)

rem Cada aplicacion tiene su propia consola. Un puerto ocupado se omite
rem para evitar el bloqueo del ejecutable por una segunda compilacion.
call :iniciar "Booking.API" "Booking\src\Booking.API" 7296
if errorlevel 1 goto :error
call :iniciar "Availability.API" "Availability\Availability.API" 7004
if errorlevel 1 goto :error
call :iniciar "Availability.Web" "Availability\Availability.Web" 7165
if errorlevel 1 goto :error

echo.
echo Inicio completado. Revisa las consolas por posibles errores del worker.
echo Web: https://localhost:7165
echo Booking: https://localhost:7296/swagger
echo Availability: https://localhost:7004/swagger
echo.
echo Para detener las aplicaciones, presiona Ctrl+C en cada consola.
echo Los contenedores se pueden detener desde Docker Desktop.
echo Este BAT no inicia Payment ni Auth.TestApi.
start "" "https://localhost:7165"
popd
pause
exit /b 0

:iniciar
echo Comprobando %~1...
powershell -NoProfile -Command "$c=New-Object Net.Sockets.TcpClient; try { $t=$c.ConnectAsync('localhost',%~3); if ($t.Wait(1000) -and $c.Connected) { exit 0 }; exit 1 } catch { exit 1 } finally { $c.Dispose() }"
if not errorlevel 1 (
    echo Puerto %~3 ocupado: se conserva el proceso existente. Verifica que sea %~1.
    exit /b 0
)
start "%~1" cmd /k "dotnet run --project %~2 --launch-profile https"
echo Esperando %~1 en el puerto %~3...
powershell -NoProfile -Command "$limite=(Get-Date).AddSeconds(90); do { $c=New-Object Net.Sockets.TcpClient; try { $t=$c.ConnectAsync('localhost',%~3); if ($t.Wait(1000) -and $c.Connected) { exit 0 } } catch {} finally { $c.Dispose() }; Start-Sleep -Seconds 2 } while ((Get-Date) -lt $limite); exit 1"
if errorlevel 1 (
    echo ERROR: %~1 no inicio a tiempo. Revisa su consola. Las aplicaciones ya abiertas siguen ejecutandose.
    exit /b 1
)
exit /b 0

:faltanProyectos
echo ERROR: Faltan proyectos. Coloca el BAT en la raiz del repositorio actualizado.
goto :error

:error
echo.
echo No se pudo completar el inicio. Revisa el mensaje anterior.
echo No se eliminaron datos ni se detuvieron los procesos existentes.
popd
pause
exit /b 1
