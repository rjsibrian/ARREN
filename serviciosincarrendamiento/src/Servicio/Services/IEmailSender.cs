using System.Collections.Generic;
using System.Threading.Tasks;
using ServicioSincArrendamiento.Models;

namespace Servicio.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string recipients, string subject, List<EmailAttachment> attachments, string body = "");
    }
} 