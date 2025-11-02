# Pruebas Unitarias - ServicioSincArrendamiento

Este proyecto contiene las pruebas unitarias para el `ServicioSincArrendamiento`. El objetivo de estas pruebas es asegurar la calidad, fiabilidad y corrección de la lógica de negocio del servicio antes de su despliegue.

## Tecnologías Utilizadas

- **Framework**: .NET 8.0 (compatible con el proyecto principal)
- **Pruebas**: [xUnit](https://xunit.net/) - Framework de pruebas unitarias moderno y extensible para .NET
- **Mocking**: [Moq](https://github.com/moq/moq) - Librería popular para crear objetos de simulación (mocks)
- **Cobertura**: coverlet.collector para análisis de cobertura de código

## Estructura de las Pruebas

Las pruebas están organizadas en archivos que corresponden a las clases de servicio del proyecto:

### `SyncServiceTests.cs` - **6 pruebas**

Este es el archivo de pruebas más importante, ya que cubre el núcleo de la lógica de negocio del servicio:

- **`PerformSyncAsync_DebeEjecutarse_CuandoEsDiaDeReporte`**: Valida el flujo completo cuando es día de reporte (sincronización + reportería + notificación)
- **`PerformSyncAsync_EjecutaProcesoDiario_AunqueNoSeaDiaDeReporte`**: Asegura que los procesos diarios siempre se ejecutan, incluso si no es día de reporte
- **`PerformSyncAsync_ReportModeForce_SiempreEnviaReportes`**: Testa ReportingMode.Force (envía reportes siempre, aunque estén vacíos)
- **`PerformSyncAsync_DebeEnviarNotificacionDeError_CuandoHayExcepcion`**: Manejo de excepciones y notificación de errores críticos
- **`PerformSyncAsync_ReportModeFlexible_NoEnviaCorreo_SinDatos`**: Testa ReportingMode.Flexible (solo envía si hay datos)
- **`PerformSyncAsync_ReportModeNone_NuncaEnviaReportes`**: Testa ReportingMode.None (nunca envía reportes)

**Cobertura**: Procesos de negocio, sincronización, reportería (PDF y Excel), notificaciones, manejo de errores, y todos los modos de reporte.

### `NotificationServiceTests.cs` - **3 pruebas**

Pruebas enfocadas en el servicio de notificaciones por email:

- **`SendNotificationAsync_NoEnviaCorreo_SiNoHayDestinatarios`**: Valida el comportamiento cuando no hay destinatarios
- **`SendNotificationAsync_ConstruyeAsuntoCorrecto_ParaExito`**: Verifica la construcción correcta del email de éxito
- **`SendNotificationAsync_ManejaCorrectamente_CuandoHayAttachments`**: Testa el manejo de archivos adjuntos

**Cobertura**: Configuración de email, manejo de destinatarios, attachments, y logging.

### `ReportingServiceTests.cs` - **7 pruebas**

Pruebas completas del servicio de generación de reportes:

**PDF de Morosidad:**
- **`GenerateMorosidadPdfAsync_DebeGenerarPdf_ConDatosValidos`**: Generación PDF con datos
- **`GenerateMorosidadPdfAsync_DebeGenerarPdfVacio_CuandoNoHayDatos`**: PDF sin datos
- **`GenerateMorosidadPdfAsync_DebeIncluirLogo_CuandoSeProporcionaLogo`**: PDF con logo corporativo

**Excel de Morosidad:**
- **`GenerateMorosidadExcelAsync_DebeGenerarExcel_ConDatosValidos`**: Excel con datos de morosidad
- **`GenerateMorosidadExcelAsync_DebeGenerarExcelVacio_CuandoNoHayDatos`**: Excel sin datos

**Excel de Inactivos:**
- **`GenerateInactivosExcelAsync_DebeGenerarExcel_ConDatosValidos`**: Excel con datos de inactivos
- **`GenerateInactivosExcelAsync_DebeGenerarExcelVacio_CuandoNoHayDatos`**: Excel sin datos

**Cobertura**: Todos los métodos de IReportingService, validación de formatos de archivo (magic numbers), manejo de casos vacíos.

### `WorkerTests.cs` - **2 pruebas**

Pruebas del componente programador del servicio:

- **`ExecuteAsync_DebeLlamarAlSyncService_DespuesDelDelayCalculado`**: Verifica que el Worker llama al SyncService en el momento correcto
- **`ExecuteAsync_DebeLoguearErrorCriticoYParar_ConFormatoDeHoraInvalido`**: Manejo de errores de configuración inválida

**Cobertura**: Scheduling, manejo de configuración, logging de errores críticos.

## Estado de Cobertura

**Cobertura Total Estimada: ~85-90%**

- ✅ **SyncService**: Cobertura alta (todos los métodos principales, modos de reporte, manejo de errores)
- ✅ **NotificationService**: Cobertura básica (escenarios principales)
- ✅ **ReportingService**: Cobertura completa (100% de métodos públicos)
- ✅ **Worker**: Cobertura básica (escenarios críticos)

## Cómo Ejecutar las Pruebas

### Ejecutar todas las pruebas
```bash
dotnet test
```

### Ejecutar con cobertura
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Ejecutar un archivo específico
```bash
dotnet test --filter "SyncServiceTests"
```

### Ejecutar una prueba específica
```bash
dotnet test --filter "PerformSyncAsync_DebeEjecutarse_CuandoEsDiaDeReporte"
```

## Dependencias de Prueba

- **Microsoft.NET.Test.Sdk** (17.12.0): SDK de pruebas de Microsoft
- **xunit** (2.9.2): Framework de pruebas
- **xunit.runner.visualstudio** (2.8.2): Runner para Visual Studio
- **Moq** (4.20.72): Framework de mocking
- **coverlet.collector** (6.0.2): Colector de cobertura de código

## Notas Importantes

1. **Compatibilidad**: Las pruebas usan .NET 8.0 para mantener compatibilidad con el proyecto principal
2. **Mocking Strategy**: Se usa Moq para aislar dependencias y testear lógica específica
3. **Validación de Archivos**: Se verifican magic numbers (firmas de archivo) para PDF y Excel
4. **Configuración Real**: Los tests usan estructuras de configuración idénticas al código de producción
5. **Error Handling**: Se incluyen pruebas específicas para manejo de excepciones y casos edge

Las pruebas están diseñadas para ejecutarse rápidamente y de forma independiente, sin requerir conexiones a base de datos o servicios externos. 