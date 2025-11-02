using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Servicio.Services;
using ServicioSincArrendamiento.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace ServicioSincArrendamiento.Services
{
    public class SyncService : ISyncService
    {
        private readonly ILogger<SyncService> _logger;
        private readonly IDataAccessService _dataAccess;
        private readonly INotificationService _notification;
        private readonly IReportingService _reportingService;
        private readonly SyncSettings _syncSettings;
        private readonly EmailSettings _emailSettings;
        private readonly AppSettings _appSettings;
        private const string SyncProcessCode = "ARRENDA_POS_SYNC";

        public SyncService(
            ILogger<SyncService> logger,
            IDataAccessService dataAccess,
            INotificationService notification,
            IReportingService reportingService,
            IOptions<SyncSettings> syncSettings,
            IOptions<EmailSettings> emailSettings,
            IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _dataAccess = dataAccess;
            _notification = notification;
            _reportingService = reportingService;
            _syncSettings = syncSettings.Value;
            _emailSettings = emailSettings.Value;
            _appSettings = appSettings.Value;
        }

        public async Task PerformSyncAsync()
        {
            _logger.LogInformation("Iniciando ciclo de sincronización diario...");

            try
            {
                _logger.LogInformation("Iniciando proceso de sincronización de arrendamientos.");

                    var businessResult = await _dataAccess.ExecuteBusinessProcessAsync(DateTime.UtcNow);
                    
                    var arrendamientos = await _dataAccess.GetArrendamientosAsync(DateTime.UtcNow);
                    _logger.LogInformation("Se encontraron {Count} arrendamientos para sincronizar.", arrendamientos.Count());

                    var syncCounter = 0;
                    foreach (var arrendamiento in arrendamientos)
                    {
                        try
                        {
                            await _dataAccess.ExecuteSyncProcessAsync(arrendamiento);
                            syncCounter++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Fallo al sincronizar el retailer: {Retailer}", arrendamiento.Retailer);
                        }
                    }
                    var syncResult = $"Sincronización completada. Comercios procesados: {syncCounter}/{arrendamientos.Count()}.";
                    
                    _logger.LogInformation("Procesos de negocio y sincronización completados.");
                    _logger.LogInformation("Resultado Business: {BusinessResult}", businessResult);
                    _logger.LogInformation("Resultado Sync: {SyncResult}", syncResult);

                    // Se ejecuta el proceso para deshabilitar arrendamientos antes de generar reportes.
                    await _dataAccess.ExecuteDisableProcessAsync();


                // --- Lógica de Control de Reportes Mensuales ---
                bool isReportDay;
                try
                {
                    var today = DateTime.UtcNow;
                    var lastDayOfMonth = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
                    // Restamos los días para encontrar el día objetivo. Ej: 31 - 2 = día 29.
                    var targetReportDate = lastDayOfMonth.AddDays(-_syncSettings.AdvanceDays); 
                    isReportDay = today.Date == targetReportDate.Date;
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    _logger.LogError(ex, "Error al calcular la fecha de reporte. El valor de 'AdvanceDays' ({AdvanceDays}) podría ser demasiado grande o inválido para el mes actual.", _syncSettings.AdvanceDays);
                    isReportDay = false;
                }

                bool shouldSendReport = _syncSettings.SkipReportDateValidation || isReportDay;

                if (shouldSendReport)
                {
                    if (_syncSettings.SkipReportDateValidation)
                    {
                        _logger.LogWarning("Se está forzando la generación de reportes debido a que 'SkipReportDateValidation' es true.");
                    }
                    else
                    {
                        _logger.LogInformation("Hoy es el día designado para el reporte mensual. Procediendo...");
                    }

                    var morosidadData = await _dataAccess.GetMorosidadReportDataAsync();
                    var inactivosData = await _dataAccess.GetInactivosReportDataAsync();
                    
                    // Obtener parámetros y extraer el logo
                    byte[]? logo = null;
                    try
                    {
                        var systemParameters = await _dataAccess.GetSystemParametersAsync(_appSettings.IdSistema, _appSettings.Phrase);
                        var logoParameter = systemParameters.FirstOrDefault(p => p.Codigo.Equals("Logo", StringComparison.OrdinalIgnoreCase));
                        
                        if (logoParameter?.Byte != null)
                        {
                            if (logoParameter.Byte is string byteString && !string.IsNullOrEmpty(byteString))
                            {
                                if (byteString.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    logo = ConvertHexStringToBytes(byteString);
                                }
                                else
                                {
                                    try { logo = Convert.FromBase64String(byteString); }
                                    catch { logo = new byte[0]; }
                                }
                            }
                            else if (logoParameter.Byte is byte[] byteArray)
                            {
                                logo = byteArray;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("No se encontró el parámetro 'Logo' o su valor en la columna 'Byte' es nulo.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudo obtener o procesar el logo desde los parámetros del sistema.");
                    }

                    // Lógica para decidir si se envía correo basado en ReportMode y los datos
                    bool hasMorosidad = morosidadData.Any();
                    bool sendMorosidadReport = _syncSettings.ReportMode == ReportingMode.Force || (_syncSettings.ReportMode == ReportingMode.Flexible && hasMorosidad);
                    bool sendNoMorosidadNotification = _syncSettings.ReportMode == ReportingMode.Flexible && !hasMorosidad;

                    if (_syncSettings.ReportMode != ReportingMode.None)
                    {
                        var attachments = new List<EmailAttachment>();
                        if (sendMorosidadReport)
                        {
                            var pdfMorosidad = await _reportingService.GenerateMorosidadPdfAsync(morosidadData, logo);
                            var excelMorosidad = await _reportingService.GenerateMorosidadExcelAsync(morosidadData);
                            attachments.Add(new EmailAttachment { FileName = "ReporteMorosidad.pdf", Data = pdfMorosidad, ContentType = "application/pdf" });
                            attachments.Add(new EmailAttachment { FileName = "ReporteMorosidad.xlsx", Data = excelMorosidad, ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" });
                        }

                        var excelInactivos = await _reportingService.GenerateInactivosExcelAsync(inactivosData);
                        attachments.Add(new EmailAttachment { FileName = "ReporteInactivos.xlsx", Data = excelInactivos, ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" });

                        var recipients = await _dataAccess.GetEmailRecipientsAsync(_appSettings.IdSistema, 1); // IdTipo = 1 para notificaciones normales
                        await _notification.SendNotificationAsync(recipients, attachments, sendNoMorosidadNotification);
                    }
                    else
                    {
                        _logger.LogInformation("El modo de reporte está en 'None'. No se enviarán correos.");
                    }

                    _logger.LogInformation("Proceso de reportes finalizado.");
                }
                else
                {
                    _logger.LogInformation("Hoy no es el día designado para el envío de reportes mensuales.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "El proceso de sincronización ha fallado con una excepción no controlada.");
                try
                {
                    var errorRecipients = await _dataAccess.GetEmailRecipientsAsync(_appSettings.IdSistema, 2); // IdTipo = 2 para notificaciones de error
                    
                    var exceptionInfo = ExceptionHelper.CreateExceptionInfo(
                        ex,
                        "ServicioSincArrendamiento",
                        "PerformSyncAsync",
                        informacionAdicional: $"Proceso: Sincronización de Arrendamientos\nConfiguración: {_syncSettings.ReportMode}\nIdSistema: {_appSettings.IdSistema}"
                    );
                    
                    await _notification.SendNotificationAsync(errorRecipients, new List<EmailAttachment>(), false, ex.Message, exceptionInfo);
                }
                catch (Exception notificationEx)
                {
                    _logger.LogError(notificationEx, "Fallo crítico: No se pudo enviar la notificación de error.");
                }
            }
        }

        private byte[] ConvertHexStringToBytes(string hex)
        {
            if (hex.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                hex = hex.Substring(2);

            if (hex.Length % 2 != 0)
                hex = "0" + hex;

            try
            {
                return Enumerable.Range(0, hex.Length)
                                .Where(x => x % 2 == 0)
                                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                                .ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al convertir la cadena hexadecimal del logo: {Hex}", hex);
                return new byte[0];
            }
        }
    }
} 
