#!/bin/bash
# Levanta todo el stack local. Antes de arrancar revisa lo que suele romperse
# (Docker, .env de Keycloak, User Secrets, puertos, migraciones) y avisa qué falta.
# En Windows correrlo con Git Bash:  ./start-all.sh

set -uo pipefail
cd "$(dirname "$0")"

KEYCLOAK_DIR="Authorization/Keycloak"
KEYCLOAK_ENV="$KEYCLOAK_DIR/.env"
KEYCLOAK_REALM_URL="http://127.0.0.1:8081/realms/realm-tp-prog4"

BOOKING="Booking/src/Booking.API"
AVAILABILITY="Availability/Availability.API"
AVAILABILITY_WEB="Availability/Availability.Web"
PAYMENT="Payment/Payment.API"

ERRORS=0
ok()   { echo "  ✅ $1"; }
warn() { echo "  ⚠️  $1"; }
fail() { echo "  ❌ $1"; ERRORS=$((ERRORS + 1)); }

# true si hay algo escuchando en ese puerto local
port_in_use() { (echo > "/dev/tcp/127.0.0.1/$1") >/dev/null 2>&1; }

# true si la clave tiene un valor real en User Secrets o en appsettings.json.
# En appsettings busca "Seccion": { ... "Clave": "valor" } y descarta vacíos y
# placeholders como REPLACE_WITH_... (Booking trae uno para Service Bus).
has_config() {
    local project="$1" key="$2" section="${2%%:*}" leaf="${2##*:}"
    dotnet user-secrets list --project "$project" 2>/dev/null | grep -qE "^$key = .+" && return 0
    tr -d '\r\n' < "$project/appsettings.json" 2>/dev/null \
        | grep -oE "\"$section\"[[:space:]]*:[[:space:]]*\{[^}]*\}" \
        | grep -oE "\"$leaf\"[[:space:]]*:[[:space:]]*\"[^\"]+\"" \
        | grep -qv "REPLACE_WITH"
}

wait_until() {
    local description="$1" seconds="$2"; shift 2
    for ((i = 0; i < seconds; i += 2)); do
        "$@" && { ok "$description"; return 0; }
        sleep 2
    done
    fail "$description (no respondió en ${seconds}s)"
    return 1
}

stop_if_errors() {
    if [ "$ERRORS" -gt 0 ]; then
        echo
        echo "🛑 Hay $ERRORS problema(s). Corregilos y volvé a correr el script."
        exit 1
    fi
}

echo "🚀 Iniciando el stack de microservicios locales..."

# ---------------------------------------------------------------------------
echo
echo "1) Herramientas"
command -v dotnet >/dev/null && ok "dotnet" || fail "Falta dotnet SDK"
command -v curl   >/dev/null && ok "curl"   || fail "Falta curl"
if dotnet ef --version >/dev/null 2>&1; then ok "dotnet-ef"
else fail "Falta dotnet-ef → dotnet tool install --global dotnet-ef"; fi
if ! command -v docker >/dev/null; then fail "Falta Docker"
elif ! docker info >/dev/null 2>&1; then fail "Docker no está corriendo → abrí Docker Desktop"
else ok "Docker corriendo"; fi

# ---------------------------------------------------------------------------
echo
echo "2) .env de Keycloak"
if [ ! -f "$KEYCLOAK_ENV" ]; then
    cp "$KEYCLOAK_DIR/.env.example" "$KEYCLOAK_ENV"
    fail "No existía $KEYCLOAK_ENV. Lo copié del ejemplo: completalo con valores reales"
else
    for var in KEYCLOAK_ADMIN_PASSWORD BOOKING_CLIENT_SECRET AVAILABILITY_CLIENT_SECRET \
               MVC_CLIENT_SECRET GOOGLE_CLIENT_ID GOOGLE_CLIENT_SECRET; do
        value=$(grep -E "^$var=" "$KEYCLOAK_ENV" | tail -1 | cut -d= -f2- | tr -d '\r')
        if [ -z "$value" ] || [ "$value" = "cambiar_contrasena" ]; then
            fail "$var vacío en $KEYCLOAK_ENV"
        else
            ok "$var"
        fi
    done
fi

# ---------------------------------------------------------------------------
echo
echo "3) User Secrets / configuración"
check_config() {
    if has_config "$1" "$2"; then ok "$(basename "$1") → $2"
    else fail "$(basename "$1") → falta $2  (dotnet user-secrets set \"$2\" \"...\" --project $1)"; fi
}
check_config "$BOOKING"      "AzureServiceBus:ConnectionString"
check_config "$BOOKING"      "Keycloak:ClientSecret"
check_config "$AVAILABILITY" "AzureServiceBus:ConnectionString"
check_config "$AVAILABILITY" "AzureServiceBus:TopicName"
if has_config "$AVAILABILITY_WEB" "Keycloak:ClientSecret"; then ok "Availability.Web → Keycloak:ClientSecret"
else warn "Availability.Web → falta Keycloak:ClientSecret (el login del MVC no va a andar)"; fi

# ---------------------------------------------------------------------------
echo
echo "4) Puertos de los servicios .NET"
for port in 5194 5057 7004 5062 7016 5009 7165 5089; do
    port_in_use "$port" && fail "Puerto $port ocupado (¿quedó un servicio corriendo de antes?)"
done
[ "$ERRORS" -eq 0 ] && ok "Puertos libres"

stop_if_errors

# ---------------------------------------------------------------------------
echo
echo "5) Postgres de Booking (puerto 5432)"
if docker ps --format '{{.Names}}' | grep -q '^postgres-booking$'; then
    ok "Contenedor postgres-booking ya corriendo"
elif port_in_use 5432; then
    # Otro Postgres (ej. instalado en Windows) ya usa el 5432. Si levantamos el
    # contenedor también, los dos escuchan en el mismo puerto y Booking se conecta
    # a cualquiera de los dos → error 28P01 de contraseña.
    warn "Ya hay otro Postgres en el 5432: uso ese y NO levanto el contenedor."
    warn "Tiene que aceptar las credenciales de ConnectionStrings:DefaultConnection (appsettings o User Secrets)."
else
    docker compose up -d postgres || fail "No se pudo levantar postgres-booking"
    wait_until "Postgres respondiendo" 30 port_in_use 5432
fi
stop_if_errors

# ---------------------------------------------------------------------------
echo
echo "6) Keycloak"
docker compose -f "$KEYCLOAK_DIR/docker-compose.yml" up -d || fail "No se pudo levantar Keycloak"
stop_if_errors
wait_until "Keycloak listo" 120 curl -sf -o /dev/null "$KEYCLOAK_REALM_URL"
stop_if_errors

# ---------------------------------------------------------------------------
echo
echo "7) Migraciones"
migrate() {
    local name="$1" project="$2" startup="$3" output
    if output=$(dotnet ef database update --project "$project" --startup-project "$startup" 2>&1); then
        ok "$name"
    else
        fail "$name"
        echo "$output" | grep -iE "error|exception" | head -5 | sed 's/^/      /'
        if echo "$output" | grep -q "28P01"; then
            echo "      → Postgres rechazó la contraseña. Revisá que no haya dos Postgres en el 5432"
            echo "        y que la contraseña coincida con ConnectionStrings:DefaultConnection."
        fi
    fi
}
migrate "Booking (Postgres)"    "Booking/src/Booking.Infrastructure"      "$BOOKING"
migrate "Availability (SQLite)" "Availability/Availability.Infraestructure" "$AVAILABILITY"
migrate "Payment (SQLite)"      "Payment/Payment.DAL"                     "$PAYMENT"
stop_if_errors

# ---------------------------------------------------------------------------
echo
echo "8) Microservicios .NET (una consola por servicio)"

REPO_ROOT_WIN="$(pwd -W)"
RUN_TMP_DIR="$(mktemp -d)"

TITLES=(Booking Availability Payment AuthTestApi AvailabilityWeb)

# Genera un .bat temporal que fija el título con el comando "title" (adentro del
# propio .bat, no como argumento de "start") y corre el servicio. Pasarle el
# título directo a "start" como argumento falla: Git Bash no preserva las
# comillas al traducir argv a la línea de comandos de Windows cuando el texto
# no tiene espacios, y sin comillas reales "start" confunde el título con el
# programa a ejecutar ("no se puede encontrar el archivo <Título>").
# "pause" al final deja la consola abierta si el servicio crashea al iniciar,
# para poder leer el error antes de que la ventana se cierre sola.
make_launcher() {
    local title="$1" extra_env="$2" project="$3" profile="$4"
    local bat="$RUN_TMP_DIR/$title.bat"
    {
        echo "@echo off"
        echo "title $title"
        echo "cd /d \"$REPO_ROOT_WIN\""
        [ -n "$extra_env" ] && echo "$extra_env"
        echo "dotnet run --project \"$project\" --launch-profile $profile"
        echo "pause"
    } > "$bat"
    cmd.exe //c start "" "$bat"
}

# Al cerrar esta terminal (ENTER, Ctrl+C o salida normal) cierra las 5 consolas por título.
cleanup_consoles() {
    for t in "${TITLES[@]}"; do
        taskkill //F //FI "WINDOWTITLE eq $t*" >/dev/null 2>&1
    done
    rm -rf "$RUN_TMP_DIR"
}
trap cleanup_consoles EXIT

make_launcher "Booking" "" "$BOOKING" "http"
# https porque Availability.Web llama a Availability.API en https://localhost:7004
make_launcher "Availability" "" "$AVAILABILITY" "https"
# https porque Booking llama a Payment por gRPC en https://localhost:7016 (HTTP/2)
make_launcher "Payment" "" "$PAYMENT" "https"
# Estas variables van solo a Auth.TestApi. Con "export" les pisaban la audiencia
# a Booking y Availability, y rechazaban sus tokens con 401.
make_launcher "AuthTestApi" "set Keycloak__Authority=$KEYCLOAK_REALM_URL
set Keycloak__Audience=payment-api" "Authorization/Auth.TestApi/Auth.TestApi.csproj" "http"
make_launcher "AvailabilityWeb" "" "$AVAILABILITY_WEB/Availability.Web.csproj" "https"

echo
echo "  Booking        http://localhost:5194/swagger"
echo "  Availability   https://localhost:7004/swagger"
echo "  Payment        https://localhost:7016"
echo "  MVC            https://localhost:7165"
echo "  Keycloak       http://127.0.0.1:8081"
echo
echo "  Cada microservicio abrió su propia consola (Booking, Availability, Payment, AuthTestApi, AvailabilityWeb)."
echo "  Presioná ENTER en esta terminal para cerrarlas todas."
read -r
