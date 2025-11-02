using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Servicio.Services;
using ServicioSincArrendamiento.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnitTest
{
    public class NotificationServiceTests
    {
        private Mock<IOptions<EmailSettings>> CreateMockEmailSettings()
        {
            var mockEmailSettings = new Mock<IOptions<EmailSettings>>();
            var emailSettings = new EmailSettings
            {
                Subject = "Reporte de Arrendamiento"
            };
            mockEmailSettings.Setup(s => s.Value).Returns(emailSettings);
            return mockEmailSettings;
        }

        [Fact]
        public async Task SendNotificationAsync_ConError_EnviaNotificacionDeError()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<NotificationService>>();
            var mockEmailSender = new Mock<IEmailSender>();
            var mockEmailSettings = CreateMockEmailSettings();
            var service = new NotificationService(mockLogger.Object, mockEmailSettings.Object, mockEmailSender.Object);
            
            var recipients = new List<string> { "admin@test.com" };
            var errorMessage = "Fallo de conexión a la base de datos.";

            // Act
            await service.SendNotificationAsync(recipients, null, false, errorMessage, null);

            // Assert
            mockEmailSender.Verify(s => s.SendEmailAsync(
                "admin@test.com",
                It.Is<string>(subject => subject.Contains("[ERROR]") && subject.Contains("Reporte de Arrendamiento")),
                It.IsAny<List<EmailAttachment>>(),
                It.Is<string>(body => body.Contains(errorMessage) && body.Contains("El servicio de sincronización ha fallado"))
            ), Times.Once);
        }

        [Fact]
        public async Task SendNotificationAsync_SinMorosidad_EnviaNotificacionInformativa()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<NotificationService>>();
            var mockEmailSender = new Mock<IEmailSender>();
            var mockEmailSettings = CreateMockEmailSettings();
            var service = new NotificationService(mockLogger.Object, mockEmailSettings.Object, mockEmailSender.Object);

            var recipients = new List<string> { "user@test.com" };

            // Act
            await service.SendNotificationAsync(recipients, new List<EmailAttachment>(), true, null, null);

            // Assert
            mockEmailSender.Verify(s => s.SendEmailAsync(
                "user@test.com",
                It.Is<string>(subject => subject.Contains("[INFO]") && subject.Contains("Sin Morosidad")),
                It.IsAny<List<EmailAttachment>>(),
                It.Is<string>(body => body.Contains("No se encontraron comercios con morosidad"))
            ), Times.Once);
        }

        [Fact]
        public async Task SendNotificationAsync_CasoNormal_EnviaNotificacionEstandar()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<NotificationService>>();
            var mockEmailSender = new Mock<IEmailSender>();
            var mockEmailSettings = CreateMockEmailSettings();
            var service = new NotificationService(mockLogger.Object, mockEmailSettings.Object, mockEmailSender.Object);

            var recipients = new List<string> { "user@test.com" };
            var attachments = new List<EmailAttachment> 
            { 
                new EmailAttachment { FileName = "reporte.pdf", Data = new byte[10] } 
            };

            // Act
            await service.SendNotificationAsync(recipients, attachments, false, null, null);

            // Assert
            mockEmailSender.Verify(s => s.SendEmailAsync(
                "user@test.com",
                "Reporte de Arrendamiento",
                attachments,
                It.Is<string>(body => body.Contains("Se adjuntan los reportes de morosidad e inactivos"))
            ), Times.Once);
        }

        public static IEnumerable<object[]> InvalidRecipientsData =>
            new List<object[]>
            {
                new object[] { null },
                new object[] { new string[0] },
                new object[] { new string[] { " ", null, "\t" } }
            };

        [Theory]
        [MemberData(nameof(InvalidRecipientsData))]
        public async Task SendNotificationAsync_SinDestinatariosValidos_NoDebeEnviarCorreo(IEnumerable<string> recipients)
        {
            // Arrange
            var mockLogger = new Mock<ILogger<NotificationService>>();
            var mockEmailSender = new Mock<IEmailSender>();
            var mockEmailSettings = CreateMockEmailSettings();
            var service = new NotificationService(mockLogger.Object, mockEmailSettings.Object, mockEmailSender.Object);

            // Act
            await service.SendNotificationAsync(recipients, null, false, null, null);

            // Assert
            mockEmailSender.Verify(s => s.SendEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<EmailAttachment>>(), It.IsAny<string>()), 
                Times.Never);
        }

        [Fact]
        public async Task SendNotificationAsync_ConExceptionInfo_GeneraCorreoDetallado()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<NotificationService>>();
            var mockEmailSender = new Mock<IEmailSender>();
            var mockEmailSettings = CreateMockEmailSettings();
            var service = new NotificationService(mockLogger.Object, mockEmailSettings.Object, mockEmailSender.Object);
            
            var recipients = new List<string> { "admin@test.com" };
            var errorMessage = "Error de conexión a la base de datos.";
            var exceptionInfo = new ExceptionInfo
            {
                Sistema = "Liquidacion",
                Usuario = "Sistema",
                Funcion = "InsertIva",
                Excepcion = "Timeout expired",
                StackTrace = "at System.Data.SqlClient.SqlConnection.Open()",
                FechaOcurrencia = DateTime.Now,
                InformacionAdicional = "Parámetros: @IdSistema=1, @IdTipo=2"
            };

            // Act
            await service.SendNotificationAsync(recipients, null, false, errorMessage, exceptionInfo);

            // Assert
            mockEmailSender.Verify(s => s.SendEmailAsync(
                "admin@test.com",
                It.Is<string>(subject => subject.Contains("[ERROR CRÍTICO]") && subject.Contains("Reporte de Arrendamiento")),
                It.IsAny<List<EmailAttachment>>(),
                It.Is<string>(body => 
                    body.Contains("🚨 ERROR CRÍTICO EN EL SISTEMA") &&
                    body.Contains("Sistema: Liquidacion") &&
                    body.Contains("Función: InsertIva") &&
                    body.Contains("Timeout expired") &&
                    body.Contains("📋 Detalles:")
            ), Times.Once);
        }
    }
} 