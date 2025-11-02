[**< Volver al README Principal**](../README.md)

---

### Documentación del Proyecto
1.  [Arquitectura del Sistema](./1_arquitectura.md)
2.  [Flujo de Ejecución](./2_flujo_de_ejecucion.md)
3.  [Configuración y Variables de Entorno](./3_configuracion.md)
4.  **Dependencias del Proyecto (Usted está aquí)**

---

# 4. Dependencias del Proyecto

Este documento describe todas las dependencias NuGet utilizadas en el proyecto `ServicioSincArrendamiento`. Estas librerías proporcionan las funcionalidades esenciales del servicio.

## Archivo de Proyecto Actual

El archivo `src/Servicio/Servicio.csproj` contiene todas las dependencias:

```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>dotnet-Servicio-9ded9900-ac1a-46e8-bfe1-871403a7f0b9</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="itext7.bouncy-castle-adapter" Version="9.2.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.1" />
    <PackageReference Include="Polly" Version="8.6.1" />
    <PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
    <PackageReference Include="MailKit" Version="4.7.0" />
    <PackageReference Include="ClosedXML" Version="0.102.2" />
    <PackageReference Include="iText" Version="9.2.0" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.1" />
  </ItemGroup>
</Project>
```

## Descripción de Cada Dependencia

### 🏗️ **Infraestructura del Servicio**

#### `Microsoft.Extensions.Hosting` (v8.0.1)
- **Para qué sirve**: Framework base para crear servicios de Windows (Worker Services)
- **Qué hace**: Maneja el ciclo de vida del servicio, inyección de dependencias, configuración y logging
- **Por qué es importante**: Es la base que permite que el servicio funcione como servicio de Windows

#### `Microsoft.Data.SqlClient` (v5.2.1)
- **Para qué sirve**: Conectar y comunicarse con SQL Server
- **Qué hace**: Ejecuta consultas, stored procedures y maneja conexiones a la base de datos
- **Por qué es importante**: Sin esta librería no podríamos acceder a los datos

### 🔄 **Resiliencia y Logging**

#### `Polly` (v8.6.1)
- **Para qué sirve**: Manejo de errores y reintentos automáticos
- **Qué hace**: Si falla una conexión a la BD, reintenta 3 veces con pausa de 5 segundos
- **Por qué es importante**: Hace el servicio más robusto ante problemas temporales de red

#### `Serilog.AspNetCore` (v9.0.0)
- **Para qué sirve**: Sistema de logging avanzado
- **Qué hace**: Guarda logs en archivos con rotación diaria, formatos estructurados
- **Por qué es importante**: Permite rastrear errores y monitorear el servicio

### 📧 **Envío de Correos**

#### `MailKit` (v4.7.0)
- **Para qué sirve**: Enviar correos electrónicos
- **Qué hace**: Se conecta a servidores SMTP (como Gmail) y envía los reportes por email
- **Por qué es importante**: Sin esto no se pueden enviar las notificaciones automáticas

### 📄 **Generación de Reportes**

#### `ClosedXML` (v0.102.2)
- **Para qué sirve**: Crear archivos Excel (.xlsx)
- **Qué hace**: Genera los reportes de morosidad e inactivos en formato Excel
- **Por qué es importante**: Los usuarios necesitan los reportes en Excel para analizarlos

#### `iText` (v9.2.0)
- **Para qué sirve**: Crear archivos PDF
- **Qué hace**: Genera el reporte de morosidad en formato PDF con tablas y colores
- **Por qué es importante**: Proporciona reportes profesionales en PDF

#### `itext7.bouncy-castle-adapter` (v9.2.0)
- **Para qué sirve**: Soporte criptográfico para iText
- **Qué hace**: Permite funciones avanzadas de seguridad en PDFs (requerido por iText)
- **Por qué es importante**: Dependencia técnica necesaria para que iText funcione correctamente

## Resumen por Funcionalidad

| Funcionalidad | Librería Principal | Propósito |
|---------------|-------------------|-----------|
| **Servicio Base** | Microsoft.Extensions.Hosting | Infraestructura del Worker Service |
| **Base de Datos** | Microsoft.Data.SqlClient | Conexión a SQL Server |
| **Resiliencia** | Polly | Reintentos automáticos |
| **Logging** | Serilog.AspNetCore | Registro de eventos |
| **Email** | MailKit | Envío de notificaciones |
| **Excel** | ClosedXML | Reportes en Excel |
| **PDF** | iText + bouncy-castle | Reportes en PDF |

## Instalación

Para restaurar todas las dependencias:

```bash
dotnet restore
```

Para actualizar una dependencia específica:

```bash
dotnet add package NombreDelPaquete --version X.X.X
```

---
<br>

| [**< Anterior**](./3_configuracion.md) <br> *3. Configuración* | [**Volver al Inicio**](../README.md) | **Siguiente >** <br> [*5. Flujo de Datos*](./5_flujo_de_datos_y_procesos.md) |
|:---|:---:|---:| 