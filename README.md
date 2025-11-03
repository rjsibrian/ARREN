# ServicioMerchant — Servicio de Procesamiento de Comercios

Servicio Windows (.NET Framework 4.8) diseñado para procesar archivos Excel de merchants provenientes de un servidor FTP/SFTP, actualizar información en SQL Server, generar reportes en Excel, y subirlos nuevamente al servidor remoto.  
El servicio se ejecuta en intervalos de tiempo configurables definidos en base de datos y envía notificaciones por correo electrónico tras cada ciclo de procesamiento.


## Características principales

- Procesamiento automático de archivos de entrada desde un SFTP.
- Generación de reportes Excel y envío al FTP remoto o ruta local compartida.
- Actualización de datos en SQL Server mediante procedimientos almacenados.
- Ejecución periódica según configuración (Times en App.config).
- Notificaciones por correo usando SMTP (System.Net.Mail) con reintentos automáticos.
- Logging detallado en ruta configurada.
- Configuración dinámica de parámetros (rutas, credenciales, horas, etc.) desde App.config.

---

## Tabla de Contenidos

- [Arquitectura del Sistema](docs/Arquitectura.md)
- [Flujo de Ejecución](docs/Flujo.md)
- [Configuración y Variables de Entorno](docs/Configuracion.md)
- [Dependencias del Proyecto](docs/Dependencias.md)

---

## Requisitos

Servidor Windows con:  
- Permisos administrativos para instalar y ejecutar servicios.  
- **.NET Framework 4.8** instalado.  
- SQL Server accesible con las credenciales configuradas.  
- Acceso de red a las rutas compartidas (`\\192.168.150.7\…`).  
- Conectividad al servidor SFTP configurado.  
- Acceso al servidor SMTP (puerto 587 abierto).  

---

## Despliegue

1. **Compilar la solución desde Visual Studio**  
   - Seleccionar **modo Release**.  
   - Esto generará el ejecutable en:  
     ```
     C:\Users\{user}\Desktop\{carpeta}\serviciomerchant\MerchantProcess\MerchantProcess\bin\Release\MerchantProcess.exe
     ```

2. **Instalar el servicio en Windows**  
   - Abrir **Símbolo del sistema** (CMD) o **PowerShell** como Administrador.  
   - Dirigirse al directorio del framework:
     ```cmd
     cd C:\Windows\Microsoft.NET\Framework\v4.0.30319
     ```
   - Ejecutar el comando para instalar:
     ```cmd
     installutil.exe -i C:\Users\{user}\Desktop\{carpetaRepo}\serviciomerchant\MerchantProcess\MerchantProcess\bin\Release\MerchantProcess.exe
     ```

3. **Iniciar el servicio**  
   - Abrir el **Panel de Servicios de Windows** (`services.msc`).  
   - Buscar **Servicio Merchant**.  
   - Iniciarlo manualmente o configurar su inicio como **Automático**.

---

## Operación

- El servicio ejecuta sus tareas automáticamente en los horarios definidos (`Times` en App.config).  
- Los archivos procesados y generados se almacenan en las rutas configuradas (`PathRead`, `PathFiles`).  
- Los errores y eventos se registran en los archivos de log (`PathLog`).  
- En caso de fallo en el envío de correo, se realizan hasta **3 reintentos automáticos** antes de registrar el error en la base de datos.

---

## Solución de Problemas

| Problema              | Causa probable                               | Solución                                           |
|-----------------------|---------------------------------------------|--------------------------------------------------|
| No se generan logs     | Falta de permisos en `PathLog`             | Otorgar permisos de escritura                    |
| No se envían correos   | Credenciales incorrectas o puerto 587 bloqueado | Revisar `PhraseMail`, credenciales y firewall |
| No se procesan archivos | Ruta SFTP inválida o sin conexión          | Validar `HostFtp`, `FolderFtpRead` y credenciales |
| Error SQL              | Timeout o cadena de conexión inválida       | Revisar conexión y procedimientos almacenados    |
| Servicio no inicia     | Dependencias faltantes (.NET Framework 4.8 o permisos) | Verificar instalación del runtime y permisos de usuario |

---

