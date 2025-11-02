# Documentación Detallada del Flujo de Datos del Servicio de Sincronización

## 1. Diccionario de Entidades de Datos (Tablas y Vistas Clave)

Esta sección describe las tablas y vistas centrales del proceso y los campos específicos que se utilizan.

| Tabla / Vista | Campo Utilizado | Descripción del Uso |
| :--- | :--- | :--- |
| **`Comercios.DbDatos_Terminals`** | `Monto_Arrendamiento` | El costo de arrendamiento de un POS individual. Usado para sumar la deuda total. |
| | `Status` | Filtra para incluir solo terminales activas (`Status = '0'`). |
| | `FechaInicio`, `Fecha_Inicio` | Se usa para incluir solo terminales cuyo contrato ya comenzó. |
| | `IdBussiness_Fk` | Clave foránea para enlazar la terminal a su respectivo comercio. |
| **`dbo.DBDATOS_ARRENDAMIENTO_COMERCIOS_TEMP`**| `RETAILER` | Identificador único del comercio. Es la clave principal lógica. |
| | `MTO_ARRENDAMIENTO` | **Campo Crítico.** Almacena la deuda mensual total del comercio. Es actualizado por el SP de sincronización. |
| | `NO_POS` | Almacena el número total de terminales activas. También es actualizado por el SP de sincronización. |
| | `ESTADO` | Indica si el cobro de arrendamiento está activo (`> 0`) o inactivo (`0`). |
| | `FECHA_MODIFICACION` | **Campo Crítico.** Se actualiza cada vez que el SP de sincronización "toca" un registro. Se usa para detectar registros obsoletos. |
| | `Alertas` | Contador de ciclos en los que el comercio ha sido moroso. |
| | `Fecha_Desabilitado`, `Descripcion_Desa`| Se actualizan cuando un comercio es desactivado automáticamente. |
| **`dbo.sincronizacion_control`** | `fecha_ultima_sinc` | Guarda la fecha de la última ejecución exitosa para controlar la frecuencia. |
| **`dbo.Vw_Arrendamiento_Pos_comercios`** | `retailer` | Usado para enlazar con la tabla `_TEMP`. |
| | `debe` | Campo calculado que representa el número de cuotas pendientes. Se usa para determinar si un comercio es moroso. |

## 2. Flujo de Datos Cronológico (Nivel de Campo)

Este es el viaje que realizan los datos cada vez que el servicio se ejecuta.

---

### **FASE 0: Preparación de Datos de Negocio**

**¿Por qué esta fase?** Antes de sincronizar, se debe ejecutar la lógica de negocio que prepara los datos.

- **SP Invocado:** `Sp_DbDatos_Arrendamiento_Pos_Business`
- **Método en Código:** `ExecuteBusinessProcessAsync(DateTime.UtcNow)`
- **Propósito:** Preparar y validar los datos para el proceso de débitos del día

---

### **FASE 1: Obtener la "Verdad Absoluta" de la Deuda**

**¿Por qué esta fase?** El objetivo es calcular, desde cero, cuánto debe pagar cada comercio por arrendamiento este mes.

- **SP Invocado:** `Comercios.Sp_Arrendamiento_Lista_gete`
- **Método en Código:** `GetArrendamientosAsync(DateTime.UtcNow)`
- **Flujo de Datos:**
    1.  **SELECCIONA** las terminales de `Comercios.DbDatos_Terminals`
    2.  **FILTRA** estas terminales usando los campos:
        - `WHERE Monto_Arrendamiento > 0` (Solo las que tienen un costo)
        - `AND Status = '0'` (Solo las que están activas)
        - `AND (FechaInicio < @Fecha OR Fecha_Inicio < @Fecha)` (Solo las cuyos contratos ya empezaron)
    3.  **UNE** los resultados con `Comercios.DbDatos_Bussiness_Data` (para obtener el `Retailer` y si `Consolida`) y con `Comercios.DbDatos_Afiliation_Data` (para obtener el `Retailer Padre`).
    4.  **AGRUPA** por `b.Retailer`, `a.Retailer` y `b.Consolidar`.
    5.  **CALCULA** dos nuevos campos agregados para cada grupo:
        - `SUM(Monto_Arrendamiento)` que se convierte en el campo de salida **`Monto`**.
        - `COUNT(t.Terminal)` que se convierte en el campo de salida **`Cantidad`**.
- **SALIDA:** Una lista maestra en memoria con las columnas: `Retailer`, `Padre`, `Consolidar`, `Monto` (total calculado), `Cantidad` (total calculado).

---

### **FASE 2: Sincronizar la "Verdad Absoluta" con la Tabla de Control (BUCLE CRÍTICO)**

**¿Por qué esta fase?** Tomar la lista maestra del paso anterior y reflejarla en la tabla de trabajo principal.

- **SP Invocado:** `Sp_DbDatos_Arrendamiento_Pos_Synchronize` 
- **Método en Código:** `ExecuteSyncProcessAsync(arrendamiento)` - **SE EJECUTA EN BUCLE**
- **CRÍTICO:** Este SP se ejecuta **una vez por cada comercio** de la lista maestra:

```csharp
foreach (var arrendamiento in arrendamientos)
{
    await _dataAccess.ExecuteSyncProcessAsync(arrendamiento);
}
```

- **Flujo de Datos:**
    1.  **RECIBE** como parámetros los campos `Retailer`, `Padre`, `Consolidar`, `Monto` y `Cantidad` del comercio actual en el bucle.
    2.  **VERIFICA** si el `@RETAILER` existe en la tabla `dbo.DBDATOS_ARRENDAMIENTO_COMERCIOS_TEMP`.
    3.  **SI NO EXISTE:**
        - **INSERTA** una nueva fila en `..._TEMP` poblando las columnas `RETAILER`, `RTLPADRE`, `CONSOLIDAR`, `MTO_ARRENDAMIENTO`, `NO_POS` con los valores de los parámetros.
    4.  **SI YA EXISTE:**
        - **ACTUALIZA** la fila existente en `..._TEMP`.
        - **Establece** `MTO_ARRENDAMIENTO = @ARRENDAMIENTO`.
        - **Establece** `NO_POS = @NOPOS`.
        - **Establece** `FECHA_MODIFICACION = GETDATE()`. **<-- ¡Este es el paso clave para el siguiente SP!**

---

### **FASE 3: Limpieza y Mantenimiento Automático**

**¿Por qué esta fase?** Ahora que la tabla de control está actualizada, se ejecutan reglas de negocio para mantener su integridad.

#### **3.1 Desactivar Registros Obsoletos**

- **SP Invocado:** `Sp_DbDatos_Arrendamiento_Pos_Disable`
- **Método en Código:** `ExecuteDisableProcessAsync()`
- **Flujo de Datos:**
    1.  **SELECCIONA** para actualizar la tabla `dbo.DBDATOS_ARRENDAMIENTO_COMERCIOS_TEMP`.
    2.  **FILTRA** las filas usando los campos de fecha:
        - `WHERE (convert(date, Fecha_Modificacion ) < CONVERT(date, getdate())) OR (Fecha_Modificacion IS NULL AND convert(date, Fecha_Creacion ) < CONVERT(date, getdate()))`
        - **Traducción:** Encuentra todos los comercios que **NO** fueron actualizados en el paso anterior (su `Fecha_Modificacion` es de ayer o más antigua). Esto significa que ya no están en la lista de arrendamientos activos.
    3.  **Para cada fila encontrada, ACTUALIZA** los siguientes campos:
        - `Estado = 0` (Desactivado).
        - `Fecha_Desabilitado = GETDATE()`.
        - `Descripcion_Desa = 'DEBITO SUSPENDIDO POR ADMINISTRATIVOS'`.

---

## 3. Flujo de Reportes Mensuales (Opcional)

### **¿Cuándo se ejecuta?** Solo cuando es momento de generar reportes mensuales.

#### **3.1 Obtener Datos de Morosidad**
- **SP Invocado:** `Sp_DbDatos_Arrendamiento_Pos_Alert_Get`
- **Método en Código:** `GetMorosidadReportDataAsync()`
- **Propósito:** Obtener comercios con saldo pendiente para el reporte

#### **3.2 Obtener Datos de Inactivos**
- **SP Invocado:** `Sp_DbDatos_Arrendamiento_Pos_Disconnect_Get`
- **Método en Código:** `GetInactivosReportDataAsync()`
- **Propósito:** Obtener comercios desactivados para el reporte

#### **3.3 Obtener Destinatarios de Email**
- **SP Invocado:** `Sp_DbDatos_Arrendamiento_Pos_Alert_Destination_Get`
- **Método en Código:** `GetEmailRecipientsAsync(idSistema)`
- **Propósito:** Obtener lista de correos para enviar los reportes

---

## 4. Resumen del Flujo Completo

```
DIARIO (SIEMPRE):
├── 0. ExecuteBusinessProcessAsync() → Sp_Business (Preparar datos)
├── 1. GetArrendamientosAsync() → Sp_Lista_gete (Obtener comercios)
├── 2. ExecuteSyncProcessAsync() → Sp_Synchronize (BUCLE - por cada comercio)
└── 3. ExecuteDisableProcessAsync() → Sp_Disable (Limpiar obsoletos)

MENSUAL (OPCIONAL):
├── 4. GetMorosidadReportDataAsync() → Sp_Alert_Get
├── 5. GetInactivosReportDataAsync() → Sp_Disconnect_Get
├── 6. GetEmailRecipientsAsync() → Sp_Alert_Destination_Get
└── 7. Generar y enviar reportes por email
```

## ⚠️ **Puntos Críticos del Flujo**

### **🔥 El Bucle de Sincronización es CRÍTICO**
Si hay 500 comercios activos, el `Sp_Synchronize` se ejecutará **500 veces** en una sola pasada. Cualquier error en este SP puede afectar a cientos de comercios.

### **📅 La Fecha de Modificación es CLAVE**
El campo `FECHA_MODIFICACION` es lo que permite al sistema saber qué comercios siguen activos y cuáles deben desactivarse. Sin esta fecha, el proceso de limpieza no funcionaría.

### **🔄 Sincronización vs Reportes**
- **Sincronización:** Se ejecuta TODOS los días sin excepción
- **Reportes:** Solo se ejecutan UNA vez al mes cuando corresponde

### **⚠️ Nota sobre Alert_Load**
Aunque existe `Sp_DbDatos_Arrendamiento_Pos_Alert_Load` en la interfaz, **actualmente NO se ejecuta** en el flujo del servicio. Está disponible para implementación futura. 