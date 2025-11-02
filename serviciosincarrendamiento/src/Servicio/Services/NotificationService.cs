using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using ServicioSincArrendamiento.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Servicio.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly EmailSettings _emailSettings;
        private readonly IEmailSender _emailSender;

        public NotificationService(ILogger<NotificationService> logger, IOptions<EmailSettings> emailSettings, IEmailSender emailSender)
        {
            _logger = logger;
            _emailSettings = emailSettings.Value;
            _emailSender = emailSender;
        }

        public async Task SendNotificationAsync(
            IEnumerable<string> recipients,
            IEnumerable<EmailAttachment> attachments,
            bool noMorosidadNotification,
            string? errorMessage = null,
            ExceptionInfo? exceptionInfo = null)
        {
            if (recipients == null || !recipients.Any())
            {
                _logger.LogWarning("No se encontraron destinatarios para la notificación.");
                return;
            }

            var validRecipients = recipients.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
            if (!validRecipients.Any())
            {
                _logger.LogWarning("No quedaron destinatarios válidos tras el filtrado.");
                return;
            }

            string subject;
            string body;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                subject = $"[ERROR] {_emailSettings.Subject}";
                
                if (exceptionInfo != null)
                {
                    body = GenerateDetailedErrorBody(exceptionInfo, errorMessage);
                }
                else
                {
                    body = $"<p>El servicio de sincronización ha fallado.</p><p><strong>Mensaje:</strong> {errorMessage}</p>";
                }
            }
            else if (noMorosidadNotification)
            {
                subject = $"[INFO] {_emailSettings.Subject} - Sin Morosidad";
                body = "<p>El proceso de sincronización de arrendamientos ha finalizado con éxito.</p><p>No se encontraron comercios con morosidad en el período actual.</p><p>Se adjunta el reporte de comercios inactivos.</p>";
            }
            else
            {
                subject = _emailSettings.Subject;
                body = "<p>El proceso de sincronización de arrendamientos ha finalizado con éxito.</p><p>Se adjuntan los reportes de morosidad e inactivos.</p>";
            }
            
            body += "<br/><p><strong>Nota:</strong> Este es un correo generado automáticamente. Por favor, no responder.</p>";

            try
            {
                await _emailSender.SendEmailAsync(
                    string.Join(",", validRecipients), 
                    subject, 
                    attachments?.ToList() ?? new List<EmailAttachment>(), 
                    body);

                _logger.LogInformation("Notificación enviada a {RecipientCount} destinatarios con asunto: {Subject}", validRecipients.Count, subject);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al solicitar el envío del correo con asunto: {Subject}", subject);
            }
        }

        private string GenerateDetailedErrorBody(ExceptionInfo exceptionInfo, string errorMessage)
        {
            var body = "<h2>ERROR EN SISTEMA</h2>";
            body += "<p>Se ha detectado un error que requiere atención.</p>";
            body += "<br/>";
            body += "<h3>📋 Detalles:</h3>";
            body += "<table style='border-collapse: collapse; width: 100%; margin: 10px 0;'>";
            body += "<tr style='background-color: #f8f9fa;'>";
            body += "<td style='border: 1px solid #ddd; padding: 8px; font-weight: bold; width: 150px;'>Sistema:</td>";
            body += $"<td style='border: 1px solid #ddd; padding: 8px;'>{exceptionInfo.Sistema ?? "No especificado"}</td>";
            body += "</tr>";
            body += "<tr>";
            body += "<td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Usuario:</td>";
            body += $"<td style='border: 1px solid #ddd; padding: 8px;'>{exceptionInfo.Usuario ?? "Sistema"}</td>";
            body += "</tr>";
            body += "<tr style='background-color: #f8f9fa;'>";
            body += "<td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Función:</td>";
            body += $"<td style='border: 1px solid #ddd; padding: 8px;'>{exceptionInfo.Funcion ?? "No especificada"}</td>";
            body += "</tr>";
            body += "<tr>";
            body += "<td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Fecha:</td>";
            body += $"<td style='border: 1px solid #ddd; padding: 8px;'>{exceptionInfo.FechaOcurrencia:dd/MM/yyyy HH:mm:ss}</td>";
            body += "</tr>";
            body += "</table>";
            body += "<br/>";
            body += "<h3>❌ Excepción:</h3>";
            body += $"<div style='background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 10px; border-radius: 4px;'>";
            body += $"<pre style='margin: 0; white-space: pre-wrap;'>{exceptionInfo.Excepcion ?? errorMessage}</pre>";
            body += "</div>";
            
            if (!string.IsNullOrEmpty(exceptionInfo.StackTrace))
            {
                body += "<br/>";
                body += "<h3>🔍 Stack Trace:</h3>";
                body += $"<div style='background-color: #e2e3e5; border: 1px solid #d6d8db; padding: 10px; border-radius: 4px; font-family: monospace; font-size: 12px; max-height: 300px; overflow-y: auto;'>";
                body += $"<pre style='margin: 0; white-space: pre-wrap;'>{exceptionInfo.StackTrace}</pre>";
                body += "</div>";
            }
            
            if (!string.IsNullOrEmpty(exceptionInfo.InformacionAdicional))
            {
                body += "<br/>";
                body += "<h3>ℹ️ Información Adicional:</h3>";
                body += $"<div style='background-color: #d1ecf1; border: 1px solid #bee5eb; padding: 10px; border-radius: 4px;'>";
                body += $"<pre style='margin: 0; white-space: pre-wrap;'>{exceptionInfo.InformacionAdicional}</pre>";
                body += "</div>";
            }
            
            
            return body;
        }
    }
} 