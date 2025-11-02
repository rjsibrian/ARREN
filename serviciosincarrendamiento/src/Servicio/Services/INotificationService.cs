using ServicioSincArrendamiento.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Servicio.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(
            IEnumerable<string> recipients,
            IEnumerable<EmailAttachment> attachments,
            bool noMorosidadNotification,
            string? errorMessage = null,
            ExceptionInfo? exceptionInfo = null);
    }
} 