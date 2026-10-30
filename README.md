# 🚀 Guía de Inicio Local e Integración del Sistema

> **Visión General:** Esta guía explica cómo configurar, iniciar y ejecutar todos los microservicios (`Booking.API`, `Availability.API`, `Payment.API`, `Auth.TestApi` y `Availability.Web`) junto con la infraestructura de **Keycloak** utilizando un único comando de inicio.

---

## 📋 Requisitos Previos y Configuración Inicial

Antes de ejecutar el entorno por primera vez, debes configurar los secretos locales para que las credenciales no se rastreen en Git.

### 1. Configurar Secretos Locales (`dotnet user-secrets`)

Ejecuta los siguientes comandos desde la raíz del repositorio para guardar las cadenas de conexión y secretos de Keycloak en tu perfil local de .NET:

#### 🔹 Booking.API
```bash
dotnet user-secrets set "AzureServiceBus:ConnectionString" "TU_CADENA_DE_CONEXION_AZURE" --project Booking/src/Booking.API
```

#### 🔹 Availability.Web
```bash
dotnet user-secrets set "Keycloak:ClientSecret" "TU_CLIENT_SECRET" --project Availability/Availability.Web/Availability.Web.csproj
```

#### 🔹 Availability.API
```bash
dotnet user-secrets set "AzureServiceBus:ConnectionString" "<connection-string>" --project Availability/Availability.API/Availability.API.csproj
dotnet user-secrets set "AzureServiceBus:TopicName" "<topic-name>" --project Availability/Availability.API/Availability.API.csproj
```

---

### 2. Verificar el archivo `.env`

> 💡 **Nota sobre Keycloak:** Keycloak requiere un archivo de variables de entorno local. El script de inicio copiará automáticamente `Authorization/Keycloak/.env.example` a `.env` en caso de que no exista.

---

## 🛠️ El Script `start-all.sh`

El script `start-all.sh` se encuentra incluido en la raíz del repositorio. Se encarga de:
- Copiar el archivo `.env`
- Levantar los contenedores de Docker (Keycloak y Base de Datos)
- Exportar las variables requeridas por `Auth.TestApi`
- Iniciar todos los microservicios en paralelo

### 📌 Cómo ejecutarlo:

1. **Dar permisos de ejecución** al script *(solo la primera vez)*:
   ```bash
   chmod +x start-all.sh
   ```

2. **Iniciar todo el sistema** con un solo comando:
   ```bash
   ./start-all.sh
   ```

3. **Detener todo el sistema:**
   Presiona `Ctrl + C` en la terminal.

---

## 🌐 Referencia de Servicios y Puertos

| Servicio | Protocolo / URL | Notas |
| :--- | :--- | :--- |
| **Availability Web UI** | `https://localhost:7165` | ⚠️ **Debe usarse el puerto HTTPS 7165**. El puerto HTTP (`5089`) fallará en las redirecciones de OIDC/Google OAuth (`/signin-oidc`). |
| **Booking API** | `http://localhost:5194/swagger` | Integrado con Azure Service Bus y Keycloak. |
| **Availability API** | `http://localhost:5000`<br>`https://localhost:5001` | Gestiona la disponibilidad y asignación de cupos. |
| **Payment API** | `http://localhost:5000` *(o puerto configurado)* | Procesa pagos y confirmaciones. |
| **Auth Test API** | `http://localhost:5000` *(perfil http)* | Test harness de Keycloak (`Audience=payment-api`). |
| **Keycloak Admin UI** | [http://127.0.0.1:8081](http://127.0.0.1:8081) | Consola de administración, OpenID Connect y realms. |

---

## ⚠️ Notas Operativas Importantes

> 🚨 **1. Advertencia del puerto de Availability Web:**
> Accede siempre a `https://localhost:7165`. Abrir la interfaz web desde el puerto HTTP (`5089`) causará errores de coincidencia de estado en OIDC y fallas de `unauthorized_client` durante el inicio de sesión con Google o Keycloak.

> 🔑 **2. Sesiones bloqueadas / Errores de Login:**
> Si experimentas una excepción de OpenID durante el inicio de sesión, borra las cookies para `localhost:7165` en las herramientas de desarrollador (`F12` $\rightarrow$ **Aplicación** $\rightarrow$ **Cookies**) y prueba desde una ventana de incógnito.
