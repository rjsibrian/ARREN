[**< Volver al README Principal**](../README.md)

---

### Documentación del Proyecto
1.  [Arquitectura del Sistema](./1_arquitectura.md)
2.  [Flujo de Ejecución](./2_flujo_de_ejecucion.md)
3.  **Configuración y Variables de Entorno (Usted está aquí)**
4.  [Dependencias del Proyecto](./4_dependencias.md)

---

# 3. Configuración y Variables de Entorno

El servicio se configura completamente a través del archivo `appsettings.json` ubicado en `src/Servicio/`. Esto permite cambiar el comportamiento sin recompilar el código. Adicionalmente, existe `appsettings.Prod.json` con valores de producción: si se desea carga automática por entorno, renómbralo a `appsettings.Production.json` o agrégalo manualmente en `Program.cs`.

## Ejemplo Completo de `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DbDatosConnection": "Data Source=TU_SERVIDOR;Initial Catalog=dbdatos;User ID=tu_usuario;Password=tu_password;TrustServerCertificate=True;",
    "ControlDbConnection": "Data Source=TU_SERVIDOR;Initial Catalog=Control;User ID=tu_usuario;Password=tu_password;TrustServerCertificate=True;"
  },
  "DatabaseSettings": {
    "CommandTimeoutSeconds": 180
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "\\\\192.168.150.7\\www\\LogSincronizarArrendamiento\\servicio-sinc-.log",
          "rollingInterval": "Day",
          "rollOnFileSizeLimit": true,
          "fileSizeLimitBytes": 10485760,
          "retainedFileCountLimit": 31,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": [ "FromLogContext" ]
  },
  "EmailSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "UserName": "Arrendamiento POS",
    "Account": "sistemassf@redserfinsa.com",
    "Password": "tu_password_de_aplicacion",
    "Subject": "Reportes Mensuales de Arrendamiento de Equipo POS"
  },
  "AppSettings": {
    "IdSistema": 1,
    "Phrase": ""
  },
  "SyncSettings": {
    "ExecutionTime": "15:36",
    "AdvanceDays": 2,
    "SkipReportDateValidation": true,
    "ReportMode": "Force"
  }
}
```

## 📋 **Configuraciones Explicadas**

### 🗄️ **ConnectionStrings** - Conexiones a Base de Datos

```json
"ConnectionStrings": {
  "DbDatosConnection": "Data Source=...",
  "ControlDbConnection": "Data Source=..."
}
```

- **`DbDatosConnection`**: Base de datos principal con información de comercios, terminales y arrendamientos
- **`ControlDbConnection`**: Base de datos de control que guarda la fecha del último reporte enviado

### 📧 **EmailSettings** - Configuración de Correo

```json
"EmailSettings": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "UserName": "Arrendamiento POS",
  "Account": "sistemassf@redserfinsa.com", 
  "Password": "nikpsffhzqsivqwc",
  "Subject": "Reportes Mensuales de Arrendamiento de Equipo POS"
}
```

| Campo | Descripción | Ejemplo |
|-------|-------------|---------|
| **Host** | Servidor SMTP | `smtp.gmail.com` |
| **Port** | Puerto del servidor | `587` (Gmail con TLS) |
| **UserName** | Nombre que aparece como remitente | `"Arrendamiento POS"` |
| **Account** | Dirección de email para enviar | `sistemassf@redserfinsa.com` |
| **Password** | Contraseña de aplicación | `nikpsffhzqsivqwc` |
| **Subject** | Asunto base del correo | `"Reportes Mensuales..."` |

> ⚠️ **Importante**: Para Gmail, usa una "Contraseña de Aplicación", no tu contraseña normal.

### ⚙️ **AppSettings** - Configuración del Sistema

```json
"AppSettings": {
  "IdSistema": 1,
  "Phrase": ""
}
```

- **`IdSistema`**: ID del sistema en la base de datos (usado para obtener destinatarios de email)
- **`Phrase`**: Frase de encriptación para parámetros del sistema (puede estar vacía)

### ⏰ **SyncSettings** - Configuración de Sincronización

```json
"SyncSettings": {
  "ExecutionTime": "23:00",
  "AdvanceDays": 20,
  "SkipReportDateValidation": false,
  "ReportMode": "Flexible"
}
```

| Campo | Descripción | Valores Posibles |
|-------|-------------|------------------|
| **ExecutionTime** | Hora diaria de ejecución | `"23:00"` (formato HH:mm) |
| **AdvanceDays** | Días antes del fin de mes para generar reportes | `20` (número) |
| **SkipReportDateValidation** | Forzar reportes siempre | `false` (prod), `true` (pruebas) |
| **ReportMode** | Modo de envío de reportes | `"Flexible"`, `"Force"`, `"None"` |

#### 🎯 **Modos de Reporte (ReportMode)**

- **`"Flexible"`** (Recomendado): Envía reportes solo si hay datos relevantes
- **`"Force"`**: Siempre envía reportes, aunque estén vacíos  
- **`"None"`**: No envía reportes (solo sincroniza datos)

#### 📅 **AdvanceDays Explicado**

Si `AdvanceDays = 20`, entonces:
- El día 11 del mes (30-20+1) ya considera que es "fin de mes"
- Esto permite generar reportes antes del cierre contable

### 📝 **Serilog** - Configuración de Logging

La sección Serilog configura dónde y cómo se guardan los logs:

- **Consola**: Muestra logs en pantalla durante desarrollo
- **Archivos**: Guarda logs en archivos con rotación diaria
- **Filtros**: Solo guarda logs importantes (Information y superior)

## 🔒 **Gestión Segura de Configuración**

### ❌ **NO HACER** (Inseguro)
```json
{
  "ConnectionStrings": {
    "DbDatosConnection": "Server=prod;User=sa;Password=123456;"
  }
}
```

### ✅ **HACER** (Seguro)

#### Para Desarrollo Local:
```bash
dotnet user-secrets set "ConnectionStrings:DbDatosConnection" "Server=..."
dotnet user-secrets set "EmailSettings:Password" "mi_password"
```

#### Para Producción:
- Usar variables de entorno
- Azure Key Vault
- Archivo de configuración separado (no en git)

## 🚀 **Configuración Rápida**

### 1. Copiar y Personalizar
1. Copia el ejemplo completo de arriba
2. Cambia `TU_SERVIDOR` por tu servidor real
3. Cambia las credenciales de email

### 2. Configuración Mínima Requerida
```json
{
  "ConnectionStrings": {
    "DbDatosConnection": "TU_CADENA_DE_CONEXION",
    "ControlDbConnection": "TU_CADENA_DE_CONEXION_CONTROL"
  },
  "EmailSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Account": "tu_email@gmail.com",
    "Password": "tu_password_de_aplicacion",
    "Subject": "Reportes de Arrendamiento"
  },
  "SyncSettings": {
    "ExecutionTime": "23:00"
  }
}
```

### 3. Verificar Configuración
```bash
dotnet run --project src/Servicio
```

Si hay errores de configuración, aparecerán inmediatamente en la consola.

---
<br>

| [**< Anterior**](./2_flujo_de_ejecucion.md) <br> *2. Flujo de Ejecución* | [**Volver al Inicio**](../README.md) | **Siguiente >** <br> [*4. Dependencias*](./4_dependencias.md) |
|:---|:---:|---:|