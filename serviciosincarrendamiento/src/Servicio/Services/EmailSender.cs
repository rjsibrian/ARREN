using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Polly;
using Polly.Retry;
using ServicioSincArrendamiento.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Servicio.Services
{
    [ExcludeFromCodeCoverage]
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly EmailSettings _emailSettings;
        private readonly AsyncRetryPolicy _retryPolicy;

        public EmailSender(ILogger<EmailSender> logger, IOptions<EmailSettings> emailSettings)
        {
            _logger = logger;
            _emailSettings = emailSettings.Value;

            _retryPolicy = Policy
                .Handle<SocketException>()
                .Or<SmtpCommandException>()
                .Or<SmtpProtocolException>()
                .Or<IOException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, "Error de red al enviar correo. Reintentando en {timeSpan}. Intento {retryCount}/3.", timeSpan, retryCount);
                });
        }

        public async Task SendEmailAsync(string recipients, string subject, List<EmailAttachment> attachments, string body = "")
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.UserName, _emailSettings.Account));
            
            foreach (var recipient in recipients.Split(',').Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                message.To.Add(MailboxAddress.Parse(recipient));
            }

            if (!message.To.Any())
            {
                _logger.LogWarning("No se proporcionaron destinatarios válidos para el correo.");
                return;
            }

            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    builder.Attachments.Add(attachment.FileName, attachment.Data, ContentType.Parse(attachment.ContentType));
                }
            }

            message.Body = builder.ToMessageBody();

            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    using var client = new SmtpClient();
                    
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    await client.ConnectAsync(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_emailSettings.Account, _emailSettings.Password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                });

                _logger.LogInformation("Correo enviado a: {Recipients} con asunto: {Subject}", recipients, subject);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo con asunto: {Subject}. Todos los reintentos han fallado.", subject);
                throw; // Re-lanzamos la excepción para que el llamador pueda manejarla
            }
        }
    }
} 