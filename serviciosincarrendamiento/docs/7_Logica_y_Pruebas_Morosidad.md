# Lógica de Morosidad y Estrategias de Prueba

Este documento explica cómo funciona la detección de morosidad en el sistema y cómo probarlo efectivamente.

## 🎯 **¿Qué es un Comercio Moroso?**

Un comercio se considera **moroso** cuando:
- Tiene un **saldo pendiente > 0** en su cuenta de arrendamiento
- Aparece en la vista `[Comercios].[vw_arrendamiento_comercios]` con `saldo > 0`

## 🔄 **Flujo Completo de Morosidad**

### **1️⃣ Detección (Base de Datos)**
```sql
-- El SP que detecta morosos:
EXEC Sp_DbDatos_Arrendamiento_Pos_Alert_Get 
    @Tipo = 2, 
    @Desde = '2025-01-01', 
    @Hasta = '2025-01-31'
```

**¿Qué hace?**
- Consulta la vista `[Comercios].[vw_arrendamiento_comercios]`
- Filtra por `saldo > 0` (comercios con deuda)
- Devuelve información detallada: código, nombre, saldo, meses pendientes, etc.

### **2️⃣ Procesamiento (Servicio .NET)**
```csharp
// En SyncService.cs - el método REAL que se usa:
var morosidadData = await _dataAccess.GetMorosidadReportDataAsync();

if (morosidadData.Any()) 
{
    // Generar reportes
    var pdfMorosidad = await _reportingService.GenerateMorosidadPdfAsync(morosidadData, logo);
    var excelMorosidad = await _reportingService.GenerateMorosidadExcelAsync(morosidadData);
}
```

### **3️⃣ Generación de Reportes**
El servicio genera **2 archivos**:
- **`ReporteMorosidad.pdf`** - Reporte visual con colores por nivel de deuda
- **`ReporteMorosidad.xlsx`** - Datos para análisis en Excel

### **4️⃣ Categorización por Nivel de Deuda**

El reporte PDF agrupa los comercios morosos por gravedad:

| Nivel | Meses Pendientes | Color de Cabecera | Descripción |
|-------|------------------|-------------------|-------------|
| **30 días** | 1 mes | 🟢 Verde | Comercios con 30 días de Incobrabilidad |
| **60 días** | 2 meses | 🟠 Naranja | Comercios con 60 días de Incobrabilidad |
| **90+ días** | 3+ meses | 🔴 Rojo | Comercios con 90 días o más de Incobrabilidad |

## 🧪 **Cómo Probar la Funcionalidad**

### **✅ Opción A: Prueba de Integración (Recomendada)**

#### **Paso 1: Preparar el Entorno**
```bash
# Asegúrate de usar una base de datos de PRUEBAS
# Nunca pruebes en producción
```

#### **Paso 2: Identificar un Comercio para Pruebas**
```sql
-- Buscar comercios SIN deuda para convertir en morosos
SELECT TOP 3 
    codigo_comercio, 
    nombre_comercio, 
    saldo
FROM [dbdatos].[Comercios].[vw_arrendamiento_comercios]
WHERE saldo <= 0
ORDER BY codigo_comercio;
```

#### **Paso 3: Simular Morosidad**
```sql
-- Crear una deuda artificial para pruebas
-- REEMPLAZA 'TU_COMERCIO_PRUEBA' con un código real
UPDATE [dbdatos].[Comercios].[arrendamiento_pos]
SET saldo = 250.75
WHERE codigo_comercio = 'TU_COMERCIO_PRUEBA';
```

#### **Paso 4: Forzar Ejecución de Reportes**
```json
// En appsettings.json - TEMPORALMENTE para la prueba
{
  "SyncSettings": {
    "SkipReportDateValidation": true,  // ✅ Fuerza reportes
    "ReportMode": "Force"             // ✅ Genera aunque esté vacío
  }
}
```

#### **Paso 5: Ejecutar el Servicio**
```bash
dotnet run --project src/Servicio
```

#### **Paso 6: Verificar Resultados**
1. **📧 Revisar email** - Debe llegar con `ReporteMorosidad.pdf` y `ReporteMorosidad.xlsx`
2. **📄 Abrir el PDF** - Verificar que el comercio aparece en la sección correcta según su deuda
3. **📊 Abrir el Excel** - Verificar los datos detallados

#### **Paso 7: Limpiar (Importante)**
```sql
-- Restaurar el saldo original
UPDATE [dbdatos].[Comercios].[arrendamiento_pos]
SET saldo = 0
WHERE codigo_comercio = 'TU_COMERCIO_PRUEBA';
```

```json
// Restaurar configuración en appsettings.json
{
  "SyncSettings": {
    "SkipReportDateValidation": false,  // ✅ Volver a normal
    "ReportMode": "Flexible"           // ✅ Volver a normal
  }
}
```

### **✅ Opción B: Prueba Unitaria (Para Desarrolladores)**

#### **Crear Datos de Prueba**
```csharp
[Test]
public async Task GenerarReporteMorosidad_ConDatosDePrueba()
{
    // Arrange - Crear datos falsos
    var datosMorosos = new List<MorosidadInfo>
    {
        new MorosidadInfo 
        { 
            No = 1,
            Banco = "Banco Prueba",
            Retailer = "TEST001", 
            Nombre = "Comercio Prueba 1", 
            Saldo = 150.75m, 
            Pte = 1,  // 1 mes pendiente = 30 días
            Inicio = DateTime.Now.AddMonths(-2),
            Mes = "01/2025",
            Pos = 2,
            Estado = "Activo",
            Abonos = 0,
            DebC = 0,
            DebA = 150.75m,
            Max = 300.00m
        },
        new MorosidadInfo 
        { 
            No = 2,
            Banco = "Banco Prueba",
            Retailer = "TEST002", 
            Nombre = "Comercio Prueba 2", 
            Saldo = 300.50m, 
            Pte = 3,  // 3 meses pendientes = 90+ días
            Inicio = DateTime.Now.AddMonths(-4),
            Mes = "01/2025",
            Pos = 1,
            Estado = "Activo",
            Abonos = 50.00m,
            DebC = 25.00m,
            DebA = 275.50m,
            Max = 400.00m
        }
    };

    // Act - Generar el reporte
    var reportingService = new ReportingService(Mock.Of<ILogger<ReportingService>>());
    var pdfBytes = await reportingService.GenerateMorosidadPdfAsync(datosMorosos, null);
    var excelBytes = await reportingService.GenerateMorosidadExcelAsync(datosMorosos);

    // Assert - Verificar que se generaron los archivos
    Assert.That(pdfBytes.Length, Is.GreaterThan(0));
    Assert.That(excelBytes.Length, Is.GreaterThan(0));
    
    // Verificar que el PDF es válido (magic number)
    var pdfSignature = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF-
    Assert.That(pdfBytes.Take(5), Is.EqualTo(pdfSignature));
}
```

#### **Probar sin Base de Datos**
```csharp
// Mock del DataAccessService
var mockDataAccess = new Mock<IDataAccessService>();
mockDataAccess.Setup(x => x.GetMorosidadReportDataAsync())
             .ReturnsAsync(datosMorosos);

// Usar el mock en lugar del servicio real
var syncService = new SyncService(
    logger, 
    mockDataAccess.Object,  // ✅ Usa datos falsos
    notification, 
    reporting, 
    syncSettings, 
    emailSettings, 
    appSettings
);
```

## 🔧 **Configuraciones para Pruebas**

### **Forzar Reportes Siempre**
```json
{
  "SyncSettings": {
    "SkipReportDateValidation": true,  // Ignora fechas
    "ReportMode": "Force"             // Genera siempre
  }
}
```

### **Solo Reportes si Hay Datos**
```json
{
  "SyncSettings": {
    "ReportMode": "Flexible"          // Comportamiento normal
  }
}
```

### **Desactivar Reportes Completamente**
```json
{
  "SyncSettings": {
    "ReportMode": "None"              // Solo sincronización
  }
}
```

## 📊 **Información Clave en los Reportes**

### **Campos Principales en el Reporte**
| Campo | Descripción | Fuente |
|-------|-------------|---------|
| **Retailer** | Código único del comercio | BD Principal |
| **Nombre** | Nombre del comercio | BD Principal |
| **Saldo** | Deuda total pendiente | Calculado |
| **Pte** | Meses pendientes de pago | Calculado |
| **Pos** | Cantidad de terminales | BD Principal |
| **Estado** | Estado actual del comercio | BD Principal |
| **Abonos** | Pagos realizados | BD Principal |
| **DebA** | Débitos por arrendamiento | BD Principal |

### **Lógica de Colores en PDF**
```csharp
// En ReportingService.cs
var groupedData = data.GroupBy(d => d.Pte switch {
    1 => 1,     // 🟢 Verde - 30 días
    2 => 2,     // 🟠 Naranja - 60 días  
    _ => 3      // 🔴 Rojo - 90+ días
});
```

## ⚠️ **Consideraciones Importantes**

### **Datos Sensibles**
- ⚠️ Los reportes contienen información financiera sensible
- ⚠️ Solo debe ejecutarse en entornos seguros
- ⚠️ Los emails se envían a destinatarios configurados en la BD

### **Rendimiento**
- 📊 El SP de morosidad puede ser lento con muchos datos
- 📊 Los reportes PDF pueden ser grandes con muchos comercios
- 📊 Los emails con adjuntos grandes pueden fallar

### **Frecuencia**
- 📅 Los reportes de morosidad se generan **una vez por mes**
- 📅 El sistema controla automáticamente que no se repitan
- 📅 Usar `SkipReportDateValidation: true` solo para pruebas

## 🚀 **Resumen de Prueba Rápida**

1. **🔧 Configurar** `SkipReportDateValidation: true` y `ReportMode: "Force"`
2. **💾 Simular** deuda en un comercio de prueba
3. **▶️ Ejecutar** el servicio
4. **📧 Verificar** que llega el email con reportes
5. **🔍 Revisar** que el comercio aparece en los archivos
6. **🧹 Limpiar** datos de prueba y configuración

---
<br>

| [**< Anterior**](./6_Analisis_Procedimientos_Sincronizacion.md) <br> *6. Análisis de Procedimientos* | [**Volver al Inicio**](../README.md) | |
|:---|:---:|---:| 