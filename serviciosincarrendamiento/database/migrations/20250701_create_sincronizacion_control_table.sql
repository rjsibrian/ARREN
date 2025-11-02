-- =================================================================================================
-- Script:          001_create_sincronizacion_control_table.sql
-- Descripción:     Crea la tabla de control para almacenar el estado de los procesos de servicio.
--                  Esta tabla es necesaria para que los servicios stateless (como este Worker Service)
--                  puedan guardar un registro de su última ejecución exitosa.
-- Autor:           Gemini Assistant
-- Fecha Creación:  (Fecha Actual)
-- =================================================================================================

-- Asegurarse de que se está usando la base de datos correcta
-- USE dbdatos;
-- GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='sincronizacion_control' and xtype='U')
BEGIN
    CREATE TABLE dbo.sincronizacion_control (
        id INT IDENTITY(1,1) PRIMARY KEY,
        codigo_proceso VARCHAR(50) NOT NULL UNIQUE,
        fecha_ultima_sinc DATETIME NOT NULL,
        notas VARCHAR(255) NULL
    );

    PRINT 'Tabla "sincronizacion_control" creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla "sincronizacion_control" ya existe.';
END
GO

-- (Opcional) Insertar un registro inicial para nuestro proceso si no existe.
-- Esto asegura que la primera consulta del servicio no falle.
IF NOT EXISTS (SELECT 1 FROM dbo.sincronizacion_control WHERE codigo_proceso = 'ARRENDA_POS_SYNC')
BEGIN
    INSERT INTO dbo.sincronizacion_control (codigo_proceso, fecha_ultima_sinc, notas)
    VALUES ('ARRENDA_POS_SYNC', '1900-01-01 00:00:00', 'Registro inicial para el servicio de sincronización de arrendamientos POS.');
    
    PRINT 'Registro inicial para "ARRENDA_POS_SYNC" insertado.';
END
GO 