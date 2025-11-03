# 4. Dependencias del Proyecto

El archivo `MerchantProcess.csproj` contiene todas las dependencias necesarias para el proyecto **ServicioMerchant**.  
Estas librerías proporcionan las funcionalidades esenciales del servicio, desde conexión a bases de datos hasta envío de correos y generación de reportes Excel.

| Componente               | Librería / Namespace                                           | Función                                                |
|--------------------------|---------------------------------------------------------------|-------------------------------------------------------|
| Lenguaje                 | C#                                                            | Desarrollo del servicio                               |
| Framework                | .NET Framework 4.8                                            | Plataforma de ejecución                               |
| Correo                   | System.Net.Mail                                               | Envío de correos SMTP                                 |
| Base de datos            | System.Data.SqlClient                                         | Conexión y ejecución de SP en SQL Server             |
| Excel                    | ClosedXML, DocumentFormat.OpenXml, XLParser, ExcelNumberFormat | Generación y formateo de reportes Excel             |
| JSON                     | Newtonsoft.Json                                               | Serialización y logs                                  |
| FTP/SFTP                 | Renci.SshNet                                                  | Transferencia de archivos segura                      |
| Tareas asíncronas        | Microsoft.Bcl.AsyncInterfaces, System.Threading.Tasks.Extensions | Ejecución asíncrona                                   |
| Fuentes / gráficos       | SixLabors.Fonts                                               | Renderizado de fuentes en documentos o reportes      |
| Seguridad criptográfica  | System.Security.Cryptography.*                                 | Encriptación / desencriptación de datos sensibles    |
| Logging / manejo de errores | System.IO, System.IO.Packaging, System.Text.RegularExpressions | Escritura de logs, manejo de excepciones           |
| Servicio Windows         | System.ServiceProcess                                         | Ejecución como servicio de Windows                   |
| Otros                    | System.Xml.Linq, System.Configuration, System.Core, System.Numerics.Vectors | Utilidades varias (configuración, XML, cálculos numéricos) |



