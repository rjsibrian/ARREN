# Plan de Migración: ArrendamientoPOS Sync a Worker Service

Este documento traza el plan y el progreso de la migración de la lógica de sincronización a un servicio de fondo en .NET.

---

### Fase 1: Configuración y Andamiaje del Servicio

- [x] Agregar el paquete NuGet `MailKit` para el envío de correos electrónicos.
- [x] Crear y configurar el archivo `appsettings.json` con las secciones `SyncSettings` y `EmailSettings`.
- [x] Crear las clases C# fuertemente tipadas (`SyncSettings.cs`, `EmailSettings.cs`) para la configuración.
- [x] Registrar las clases de configuración en `Program.cs` para la inyección de dependencias.

### Fase 2: Re-implementación de la Lógica de Negocio

- [x] Crear la interfaz `IDataAccessService.cs` y su implementación `DataAccessService.cs`.
- [x] Implementar los métodos de acceso a datos en `DataAccessService.cs` usando las herramientas `mcp`.
- [x] Crear la interfaz `INotificationService.cs` y su implementación `NotificationService.cs`.
- [x] Implementar el envío de correos con `MailKit` en `NotificationService.cs`.
- [x] Crear la interfaz `ISyncService.cs` y su implementación `SyncService.cs`.
- [x] Mover la lógica principal de `Sincronizar()` al método `ExecuteSyncAsync()` en `SyncService.cs`.
- [x] Registrar los servicios (`DataAccessService`, `NotificationService`, `SyncService`) en `Program.cs`.

### Fase 3: Construcción del Worker

- [x] Inyectar los servicios (`ILogger`, `ISyncService`) en el constructor de `Worker.cs`.
- [x] Implementar un `PeriodicTimer` en `ExecuteAsync` para agendar la ejecución del worker.
- [x] Dentro del ciclo del timer, invocar el método `ExecuteSyncAsync()` del `SyncService`.
- [x] Añadir logging robusto para registrar cada acción, decisión y error.

### Fase 4: Documentación

- [x] Crear un archivo `README.md` para el proyecto del servicio.
- [x] Documentar el propósito del servicio, las variables de configuración en `appsettings.json` y cómo ejecutarlo.

### Fase 5: Refinamiento y Validación
- [x] Añadir la cadena de conexión a `appsettings.json`.
- [x] Analizar `SqlProcess.vb` del proyecto original para identificar los procedimientos almacenados.
- [x] Refactorizar `DataAccessService.cs` para usar los procedimientos almacenados originales.
- [x] Validar que el flujo de datos y la lógica de negocio son idénticos al sistema original.

### Fase 6: Corrección y Verificación Final
- [x] Configurar la segunda conexión a la base de datos `Control` en `appsettings.json`.
- [x] Conectar a `dbdatos` para listar y verificar los parámetros exactos de los Stored Procedures.
- [x] Refactorizar `DataAccessService.cs` para usar la firma exacta de los SP y la conexión correcta para cada operación.
- [x] Realizar una validación final comparando el código VB y el código C# lado a lado.

### 3. Generación de Reportes (Adaptación de `MenuPrincipal.vb`)
- [x] **Modelo de Datos:**
    - [x] Crear la clase `MorosidadInfo.cs` en `Models` para tipar los datos del reporte de morosidad.
    - [x] Crear la clase `InactivoInfo.cs` en `Models` para tipar los datos del reporte de comercios inactivos.
- [x] **Acceso a Datos (`DataAccessService`):**
    - [x] Crear un nuevo SP o adaptar la consulta de `getAlert` para obtener los datos de morosidad.
    - [x] Implementar `GetMorosidadReportDataAsync()` que devuelva `IEnumerable<MorosidadInfo>`.
    - [x] Crear un nuevo SP o adaptar la consulta de `getDisconnect` para obtener los datos de inactivos.
    - [x] Implementar `GetInactivosReportDataAsync()` que devuelva `IEnumerable<InactivoInfo>`.
- [x] **Servicio de Reportes (`ReportingService`):**
    - [x] Crear la interfaz `IReportingService.cs`.
    - [x] Crear la clase `ReportingService.cs`.
    - [x] Implementar `GenerateMorosidadPdfAsync` usando una librería de PDF.
    - [x] Implementar `GenerateMorosidadExcelAsync` usando una librería de Excel.
    - [x] Implementar `GenerateInactivosExcelAsync` usando una librería de Excel.
- [x] **Servicio de Notificación (`NotificationService`):**
    - [x] Modificar la interfaz `INotificationService` y su implementación para aceptar una lista de adjuntos (nombre de archivo + array de bytes).
- [x] **Orquestación (`SyncService`):**
    - [x] Inyectar `IReportingService`.
    - [x] Modificar `PerformSyncAsync` para que, tras el éxito, llame a los servicios para obtener datos, generar los reportes y pasarlos como adjuntos al servicio de notificación.

### 4. Mejoras Adicionales de Robustez y Seguridad
- [ ] **Seguridad:** Implementar manejo de secretos (User Secrets / Azure Key Vault).
- [ ] **Seguridad:** Reemplazar la construcción de consultas SQL con Dapper para prevenir inyección SQL.
- [ ] **Robustez:** Implementar guarda de concurrencia en el `Worker` para evitar ejecuciones superpuestas.
- [ ] **Monitoreo:** Implementar Health Checks. 