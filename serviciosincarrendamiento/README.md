# Servicio de Sincronización de Arrendamientos POS

Servicio de fondo (.NET 8 Worker Service para Windows) que sincroniza datos de arrendamientos POS diariamente, genera reportes mensuales (Morosidad e Inactivos) y envía notificaciones por correo.

## Características

- Ejecución diaria programada por hora configurable (`SyncSettings.ExecutionTime`).
- Generación de reportes mensuales en PDF y Excel.
- Envío de correos con adjuntos (MailKit).
- Acceso a SQL Server con reintentos (Polly) y timeout configurable (`DatabaseSettings.CommandTimeoutSeconds`).
- Logging estructurado con Serilog (consola + archivo con rotación diaria).
- Scripts de despliegue e instalación del servicio (publish/install/uninstall).

## Tabla de Contenidos

1.  **[Arquitectura del Sistema](./docs/1_arquitectura.md)**
2.  **[Flujo de Ejecución](./docs/2_flujo_de_ejecucion.md)**
3.  **[Configuración y Variables de Entorno](./docs/3_configuracion.md)**
4.  **[Dependencias del Proyecto](./docs/4_dependencias.md)**

## Requisitos

- Windows Server con permisos para crear/ejecutar servicios.
- .NET 8 Runtime.
- Acceso a SQL Server (según `ConnectionStrings`).
- Acceso al servidor SMTP definido en `EmailSettings`.

## Configuración Esencial

Archivo `src/Servicio/appsettings.json`:

- `ConnectionStrings`: cadenas a `dbdatos` y `Control`.
- `SyncSettings`: `ExecutionTime` (HH:mm), `AdvanceDays`, `ReportMode`, `SkipReportDateValidation`.
- `EmailSettings`: host, puerto, cuenta, asunto, contraseña.
- `Serilog`: salida a consola y archivo. Ruta por defecto: `C:\Logs\SincronizarArrendamientos\log-.log`.
- `DatabaseSettings`: `CommandTimeoutSeconds` para comandos SQL.

> Nota: Existe `src/Servicio/appsettings.Prod.json` con valores de producción. Para carga automática por entorno, renómbralo a `appsettings.Production.json` o agrega su carga explícita en `Program.cs`.

## Despliegue

1) Publicar en Release:
```bash
dotnet publish src/Servicio/Servicio.csproj -c Release -o publish
```

2) Copiar `publish/` al servidor (o usar `publish.bat`).

3) Instalar el servicio en el servidor:
- Abrir consola como Administrador en la carpeta que contiene `install.bat` y `publish/`.
- Ejecutar `install.bat` (crea y arranca el servicio apuntando a `publish/Servicio.exe`).

4) Verificar logs: confirmar que existe `C:\Logs\SincronizarArrendamientos\` y que se están generando `log-YYYYMMDD.log`.

## Operación

- El `Worker` espera diariamente hasta `SyncSettings.ExecutionTime` y luego ejecuta `SyncService.PerformSyncAsync()`.
- `SyncService` aplica la lógica de negocio y, si corresponde, genera y envía reportes.
- `DataAccessService` usa reintentos y aplica el timeout configurado a cada comando SQL.

## Solución de Problemas

- No se generan logs: verificar permisos y existencia de `C:\Logs\SincronizarArrendamientos\`.
- No salen correos: validar `EmailSettings` y conectividad SMTP.
- Errores de BD: revisar `ConnectionStrings` y el firewall hacia SQL Server.
